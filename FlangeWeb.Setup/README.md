# 法兰图纸生成工具安装程序

`Output/FlangeDrawingTool-Setup.exe` 是 64 位 Windows 的单文件安装程序。默认安装到当前用户的本地程序目录，不需要管理员权限。安装时可选择位置和是否创建桌面快捷方式；也会创建开始菜单快捷方式与“已安装的应用”卸载项。

启动快捷方式后，应用在本机启动服务并打开默认浏览器。系统托盘菜单可以重新打开工具或退出服务。PDF 保存对话框需要在 Chrome 或 Edge 中通过本机地址打开。应用包括 .NET 8 运行时；安装程序本身使用 Windows 的 .NET Framework 4.x。

要重新构建，在 Windows 上运行 `build.ps1`；需要 .NET 8/9 SDK、Windows .NET Framework 编译器及 win-x64 .NET 8 运行时包。安装包输出在 `Output` 目录。`-Offline` 使用当前用户 NuGet 缓存进行离线构建。
