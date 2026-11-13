# 法兰图纸生成工具安装程序

`Output/FlangeDrawingTool-Setup.exe` 是 64 位 Windows 的单文件安装程序。默认安装到当前用户的本地程序目录，不需要管理员权限。安装时可选择位置和是否创建桌面快捷方式；也会创建开始菜单快捷方式与“已安装的应用”卸载项。

启动快捷方式后，应用在本机启动服务并打开默认浏览器。系统托盘菜单可以重新打开工具或退出服务。PDF 保存对话框需要在 Chrome 或 Edge 中通过本机地址打开。应用包括 .NET 8 运行时；安装程序本身使用 Windows 的 .NET Framework 4.x。

更新或卸载时，如果此安装目录中的工具仍在运行，安装程序会先提示关闭它。若其他程序占用文件，请关闭占用程序后重试；安装程序会先检查文件占用，避免因此只更新部分文件。

在 Visual Studio 中重新生成 `FlangeWeb.sln` 时，`FlangeWeb.Setup` 项目会自动以 Release 配置发布当前应用并重新生成安装程序。也可以单独生成 `FlangeWeb.Setup` 项目。构建需要 .NET 8/9 SDK、Windows .NET Framework 编译器及 win-x64 .NET 8 运行时包；输出在 `Output` 目录。
