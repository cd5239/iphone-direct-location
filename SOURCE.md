# 下载包对应源码

本仓库的 `source/` 包含项目自有的 C# 界面、Python 连接入口、构建脚本和测试。`source/requirements-build.txt` 固定直接构建依赖；`source/DEPENDENCIES.md` 记录制作发布包时的依赖版本，`source/third-party-licenses/` 保存第三方许可文本。构建步骤见 [README](README.md#从源码构建)。

发布包中的连接引擎包含未修改的 `pymobiledevice3 11.17.0`，其许可为 **GPL-3.0-or-later**。该版本的源码包 `pymobiledevice3-11.17.0.tar.gz` 与 Windows 下载包一起放在 [v2.4.2 Release](https://github.com/cd5239/iphone-virtual-location/releases/tag/v2.4.2)；也可从 [PyPI 的 11.17.0 版本页](https://pypi.org/project/pymobiledevice3/11.17.0/) 获取。

源码包 SHA-256：`475cb7f900ab2f86642d4490599f0ce428d003fd64d69baa507509375b7a58b7`。项目自身与第三方组件的许可边界见 [COPYRIGHT](COPYRIGHT.md)。
