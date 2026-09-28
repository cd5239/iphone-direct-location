"""Private stdin/stdout worker; no HTTP listener, UI automation, or third-party app launch."""
import argparse
import asyncio
import contextlib
import json
import logging
from pathlib import Path
import sys

from coordinates import to_wgs

request_id=None
request_command=None
def emit(event, message='', **values):
    print(json.dumps(dict(event=event,message=message,request_id=request_id,command=request_command,**values),ensure_ascii=False),flush=True)

class UserError(Exception): pass

class Engine:
    def __init__(self):
        self.stack=None
        self.location=None
        self.tunnel=None
        self.serial=None
        self.active=False

    async def devices(self):
        from pymobiledevice3.usbmux import list_devices
        return [d for d in await list_devices() if d.connection_type=='USB']

    async def select(self):
        devices=await self.devices()
        if not devices: raise UserError('没有检测到 USB iPhone。请连接数据线、解锁手机，并确认已信任此电脑。')
        if len(devices)!=1: raise UserError('检测到多台 USB 设备。为避免改错手机，请只连接需要定位的那一台。')
        return devices[0].serial

    async def lockdown(self,serial):
        from pymobiledevice3.lockdown import create_using_usbmux
        return await create_using_usbmux(serial=serial,connection_type='USB',pair_timeout=10,label='iPhone Direct Location')

    async def probe(self):
        ds=await self.devices()
        lost=None
        if self.active and self.serial not in [d.serial for d in ds]:
            await self.close(clear=False)
            lost='usb_disconnected'
        elif self.active and (self.tunnel is None or self.tunnel.rsd is None):
            await self.close(clear=False)
            lost='session_lost'
        if len(ds)!=1:
            emit('device','未连接手机，请插入 USB 数据线。' if not ds else '检测到多台设备，请只连接目标手机。',connection='disconnected' if not ds else 'multiple',usable=False,active=False)
        else:
            try:
                async with await self.lockdown(ds[0].serial) as ld:
                    name=ld.all_values.get('DeviceName','iPhone')
                    emit('device',f'{name}  ·  iOS {ld.product_version}',connection='connected',usable=True,active=self.active)
            except Exception as ex:
                emit('device','USB 已连接，但尚未完成通信。'+friendly(ex),connection='connected',usable=False,active=self.active)
        if lost:
            emit('session_lost','手机 USB 已断开，定位会话已结束。' if lost=='usb_disconnected' else '手机仍通过 USB 连接，但定位会话已中断。请重新点击修改定位。',reason=lost)

    async def open_channel(self,serial):
        from pymobiledevice3.remote.userspace_tunnel import UserspaceRsdTunnel
        from pymobiledevice3.services.dvt.instruments.dvt_provider import DvtProvider
        from pymobiledevice3.services.dvt.instruments.location_simulation import LocationSimulation
        stack=contextlib.AsyncExitStack()
        try:
            tunnel=UserspaceRsdTunnel(serial=serial,autopair=False,remotepairing_fallback=False)
            rsd=await stack.enter_async_context(tunnel)
            dvt=await stack.enter_async_context(DvtProvider(rsd))
            location=await stack.enter_async_context(LocationSimulation(dvt))
        except BaseException:
            await stack.aclose()
            raise
        self.stack,self.tunnel,self.location,self.serial=stack,tunnel,location,serial

    async def ensure(self):
        serial=await self.select()
        if self.stack is not None and serial==self.serial and self.tunnel.rsd is not None:return
        await self.close(clear=False)
        emit('status','正在通过 USB 建立手机定位连接…')
        async with await self.lockdown(serial) as ld:
            if int(ld.product_version.split('.')[0])<17:raise UserError('此直连版本面向 iOS 17.4 及以上系统。')
            if not await ld.get_developer_mode_status():raise UserError('请在 iPhone「设置 → 隐私与安全性 → 开发者模式」中手动开启，按提示重启并确认后重试。')
        from pymobiledevice3.exceptions import InvalidServiceError
        try:
            await self.open_channel(serial)
        except InvalidServiceError:
            emit('status','手机需初始化开发者组件，首次可能需要联网下载。请保持连接…')
            from pymobiledevice3.services.mobile_image_mounter import MobileImageMounterService,auto_mount
            from pymobiledevice3.exceptions import AlreadyMountedError
            async with await self.lockdown(serial) as ld:
                async with MobileImageMounterService(ld) as mounter:
                    mounted=await mounter.is_image_mounted('Personalized')
                if not mounted:
                    with contextlib.suppress(AlreadyMountedError):await auto_mount(ld)
            await self.open_channel(serial)

    async def apply(self,request):
        emit('status','正在检查地点和手机连接…',stage='checking')
        lon,lat=to_wgs(request.get('longitude'),request.get('latitude'),request.get('source','WGS84'))
        await self.ensure()
        emit('status','正在向手机发送定位指令，等待手机确认…',stage='sending')
        await self.location.set(lat,lon)
        self.active=True
        emit('applied','手机定位服务已接受修改，正在保持连接。可关闭窗口留在托盘；请在手机地图中确认位置。',longitude=lon,latitude=lat)

    async def clear(self):
        await self.ensure()
        emit('status','正在停止手机的模拟定位，请保持 USB 连接…',stage='clearing')
        await self.location.clear()
        # This selector has no reply. Keep the userspace transport alive instead
        # of tearing it down immediately after enqueueing a fire-and-forget send.
        emit('status','停止指令已发出，正在保持连接并补发确认步骤…',stage='settling')
        await asyncio.sleep(3)
        await self.location.clear()
        await asyncio.sleep(2)
        self.active=False
        await self.close(clear=False)
        emit('restored','已两次发送停止模拟指令并保持连接等待，定位会话已结束。此接口不返回真实 GPS，尚不能确认实际位置。请打开手机地图，点击当前位置；若仍未恢复，重开地图后重试。')

    async def close(self,clear=True):
        stack,self.stack=self.stack,None
        location,self.location=self.location,None
        was_active,self.active=self.active,False
        self.tunnel=None
        self.serial=None
        try:
            if clear and was_active and location is not None:
                with contextlib.suppress(Exception):
                    await asyncio.wait_for(location.clear(),3)
                    await asyncio.sleep(2)
        finally:
            if stack is not None:
                with contextlib.suppress(Exception):await asyncio.wait_for(stack.aclose(),5)

def friendly(error):
    kind=type(error).__name__
    if isinstance(error,UserError):return str(error)
    if isinstance(error,(ValueError,TypeError)):return '地点参数无效，请检查经纬度和坐标来源。'
    if isinstance(error,TimeoutError):return '连接超时。请解锁手机、重新插拔数据线后重试；若正在首次下载组件，请确认网络可用。'
    if any(s in kind for s in ['Pair','Password','Locked','Trust']):return '手机尚未信任此电脑或屏幕已锁定。请解锁并在手机上确认信任，再重试。'
    if any(s in kind for s in ['Connection','NoDevice','BrokenPipe','NotConnected']):return '手机连接已中断，请重新插拔数据线并解锁后重试。'
    if 'DeveloperMode' in kind:return '请在手机中手动开启开发者模式，重启并确认后重试。'
    return f'本次操作未完成（{kind}）。请重新连接手机后重试。'

async def serve():
    global request_id,request_command
    engine=Engine()
    emit('ready')
    try:
        while True:
            line=await asyncio.to_thread(sys.stdin.readline)
            if not line:break
            command={}
            try:
                parsed=json.loads(line.lstrip('\ufeff'))
                if not isinstance(parsed,dict):raise UserError('操作请求格式不正确。')
                command=parsed
                name=command.get('command')
                request_id=command.get('request_id')
                request_command=name
                if name=='shutdown':break
                if name=='probe':await asyncio.wait_for(engine.probe(),20)
                elif name=='apply':await asyncio.wait_for(engine.apply(command),240)
                elif name=='clear':await asyncio.wait_for(engine.clear(),90)
                else:raise UserError('未知操作。')
            except Exception as ex:
                # Drop failed sessions so a subsequent click creates fresh connections.
                if command.get('command')!='probe':await engine.close()
                emit('error',friendly(ex),error_type=type(ex).__name__,session_ended=command.get('command')!='probe')
    finally:
        await engine.close()

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--state-dir',type=Path,required=True)
    args=parser.parse_args()
    if sys.stdout:sys.stdout.reconfigure(encoding='utf-8')
    if sys.stdin:sys.stdin.reconfigure(encoding='utf-8')
    logging.basicConfig(level=logging.CRITICAL)
    import pymobiledevice3.common as common
    # Library 11.17.0 has no public cache-path environment variable. Override its one
    # cache root BEFORE importing device services; do not touch the user's home dir.
    common._HOMEFOLDER=args.state_dir.resolve()
    common._HOMEFOLDER.mkdir(parents=True,exist_ok=True)
    asyncio.run(serve())

if __name__=='__main__':main()
