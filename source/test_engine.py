import asyncio
import contextlib
import io
import json
import unittest
from types import SimpleNamespace
from unittest.mock import patch
from coordinates import validate,to_wgs,wgs_to_gcj
from engine import Engine,UserError

class CoordinateTests(unittest.TestCase):
    def test_invalid_never_accepted(self):
        for lon,lat in [(181,0),(0,91),(float('nan'),0),(0,float('inf'))]:
            with self.assertRaises(ValueError):validate(lon,lat)
    def test_wgs_not_converted(self):
        self.assertEqual(to_wgs(-122.03,37.33,'WGS84'),(-122.03,37.33))
    def test_gcj_inverse_roundtrip(self):
        for original in [(113.27,23.13),(116.3,39.9),(121.5,31.2)]:
            converted=to_wgs(*wgs_to_gcj(*original),'GCJ02')
            for a,b in zip(original,converted):self.assertAlmostEqual(a,b,places=7)
    def test_invalid_source(self):
        with self.assertRaises(ValueError):to_wgs(103,30,'UNKNOWN')

class EngineTests(unittest.IsolatedAsyncioTestCase):
    async def test_restore_keeps_transport_alive_for_no_reply_command(self):
        calls=[];e=Engine();e.active=True
        class Location:
            async def clear(self):calls.append('clear')
        async def ensure():e.location=Location()
        async def settle(seconds):calls.append(('wait',seconds))
        async def close(clear=True):calls.append(('close',clear))
        e.ensure=ensure;e.close=close
        with patch('engine.asyncio.sleep',side_effect=settle),contextlib.redirect_stdout(io.StringIO()) as output:
            await e.clear()
        self.assertEqual(calls,['clear',('wait',3),'clear',('wait',2),('close',False)])
        self.assertFalse(e.active)
        self.assertEqual(json.loads(output.getvalue().splitlines()[-1])['event'],'restored')
    async def test_failed_restore_never_reports_completion(self):
        e=Engine()
        class Location:
            async def clear(self):raise ConnectionError()
        async def ensure():e.location=Location()
        e.ensure=ensure
        with contextlib.redirect_stdout(io.StringIO()) as output:
            with self.assertRaises(ConnectionError):await e.clear()
        self.assertNotIn('restored',output.getvalue())
    async def test_tunnel_loss_does_not_report_usb_unplug(self):
        e=Engine();e.active=True;e.serial='test';e.tunnel=SimpleNamespace(rsd=None)
        async def devices():return [SimpleNamespace(serial='test')]
        class Lockdown:
            all_values={'DeviceName':'test'};product_version='26.6.2'
            async def __aenter__(self):return self
            async def __aexit__(self,*a):pass
        async def lockdown(serial):return Lockdown()
        e.devices=devices;e.lockdown=lockdown
        with contextlib.redirect_stdout(io.StringIO()) as output:await e.probe()
        events=[json.loads(x) for x in output.getvalue().splitlines()]
        self.assertEqual(events[0]['connection'],'connected')
        self.assertEqual(events[1]['reason'],'session_lost')
        self.assertFalse(e.active)
    async def test_unplug_then_reconnect_emits_updated_state(self):
        e=Engine();e.active=True;e.serial='test'
        async def devices():return []
        e.devices=devices
        with contextlib.redirect_stdout(io.StringIO()) as output:await e.probe()
        events=[json.loads(x) for x in output.getvalue().splitlines()]
        self.assertEqual(events[0]['connection'],'disconnected')
        self.assertFalse(events[0]['usable'])
        self.assertEqual(events[1]['reason'],'usb_disconnected')
    async def test_multiple_devices_rejected(self):
        e=Engine()
        async def devices():return [SimpleNamespace(serial='a'),SimpleNamespace(serial='b')]
        e.devices=devices
        with self.assertRaises(UserError):await e.select()
    async def test_no_device_rejected(self):
        e=Engine()
        async def devices():return []
        e.devices=devices
        with self.assertRaises(UserError):await e.select()
    async def test_bad_coordinates_cannot_open_device(self):
        e=Engine()
        async def ensure():self.fail('Device must not be opened for invalid coordinates')
        e.ensure=ensure
        with self.assertRaises(ValueError):await e.apply(dict(longitude=200,latitude=10,source='WGS84'))
    async def test_latitude_longitude_order(self):
        calls=[];e=Engine()
        class Location:
            async def set(self,lat,lon):calls.append((lat,lon))
        async def ensure():e.location=Location()
        e.ensure=ensure
        with contextlib.redirect_stdout(io.StringIO()):await e.apply(dict(longitude=-122.03,latitude=37.33,source='WGS84'))
        self.assertEqual(calls,[(37.33,-122.03)])
        self.assertTrue(e.active)
    async def test_clear_failure_does_not_skip_resource_cleanup(self):
        calls=[];e=Engine();e.active=True
        class Location:
            async def clear(self):calls.append('clear');raise ConnectionError()
        class Stack:
            async def aclose(self):calls.append('close')
        e.location=Location();e.stack=Stack()
        await e.close()
        self.assertEqual(calls,['clear','close']);self.assertFalse(e.active);self.assertIsNone(e.stack)

if __name__=='__main__':unittest.main()
