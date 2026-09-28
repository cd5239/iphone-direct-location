# iPhone 直连定位

一个 Windows 桌面工具，通过 USB 连接 iPhone 的开发者定位服务，手动设置或清除模拟位置。主界面可管理多个常用地点；“一键修改”入口使用当前选中的地点，并在通知区域保持运行。当前版本为 2.4。

## 下载与首次使用

从 [Releases](https://github.com/cd5239/iphone-direct-location/releases) 下载 `iphone-direct-location-v2.4-win64.zip`，完整解压后再运行。需要 Windows 10/11 64 位、.NET Framework 4.x、Apple 设备驱动及服务、USB 数据线，以及已开启开发者模式并信任此电脑的 iPhone。连接组件已包含 Python，无需用户另装。

1. 解锁手机，用 USB 连接电脑，并在手机上确认“信任此电脑”。
2. 打开 `iPhone直连定位.exe`，输入地点名称、经纬度及坐标来源，然后保存地点。
3. 点击“修改定位”；在手机地图中核对落点。之后可用 `一键修改.exe` 重新应用选中的地点。
4. 用主界面或托盘菜单发送“恢复真实定位”，再在手机地图核对实际位置。

首次运行没有预置地点。切换收藏仅改变下次要使用的地点，需点击“修改定位”才会给手机发送指令。不要只复制单个 exe；两个 exe、`engine` 和 `source` 必须保持同目录结构。

## 能力与限制

- 支持多个收藏地点，新增、修改、删除和持久化当前选择。
- 坐标来源可选 BD-09、GCJ-02、WGS84；向设备发送前换算为 WGS84。转换为近似计算，落点须在手机地图确认。
- 手机服务接受修改指令后，界面会显示结果；地图 App 可能需要刷新。
- “恢复真实定位”接口没有设备回执，程序发送停止模拟指令并等待后会提示**待手机确认**，不能据此断言 GPS 已恢复。
- 已在一台 iPhone 16 / iOS 26.6.2 上验证 USB 连接、发送定位和恢复指令；其他机型、新电脑环境及地图最终落点未完成实测。

本工具不要求打开其他手机助手界面，不调用地图地址搜索服务，也不上传地点数据。连接依赖 Apple 设备驱动和 [`pymobiledevice3`](https://github.com/doronz88/pymobiledevice3)。

## 数据与隐私

`locations.xml` 保存你输入的地点；旧 `location.xml` 仅用于首次迁移。`state/` 会在运行时生成，可能包含设备配对凭据。**不要分享 `locations.xml`、`location.xml`、备份文件或 `state/`。**本公开仓库和下载包均不包含这些文件。

详见 [换电脑使用](docs/换电脑使用.md) 和 [验证记录](docs/验证记录.md)。

## 从源码构建

界面使用 Windows .NET Framework C# 编译器构建；连接引擎使用 Python 3.12、`pymobiledevice3==11.17.0` 和 `pyinstaller==6.22.3`。在 Windows PowerShell 中：

```powershell
.\source\build-ui.ps1
py -3.12 -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r .\source\requirements-build.txt
.\source\build-engine.ps1 -Python .\.venv\Scripts\python.exe -BuildDirectory .\build-engine
New-Item -ItemType Directory -Force .\engine | Out-Null
Copy-Item .\build-engine\dist\LocationEngine\* .\engine -Recurse
```

完整依赖清单及第三方许可文本位于 `source/DEPENDENCIES.md` 和 `source/third-party-licenses/`。构建流程需要联网安装依赖；运行时首次准备开发者组件也可能需要联网。

## 授权

当前仓库暂未附加本项目的开源许可证。公开内容可供查看；复制、修改或再发布本项目源码前，请先联系仓库所有者取得许可。第三方组件分别遵循各自许可证。
