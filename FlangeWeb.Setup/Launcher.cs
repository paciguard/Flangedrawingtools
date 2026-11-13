using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;

internal static class Launcher
{
    private static string StateFile
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlangeDrawingTool", "port.txt"); }
    }
    private static bool Healthy(int port)
    {
        try
        {
            var request = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:" + port + "/api/health");
            request.Proxy = null; request.Timeout = 500;
            using (var response = request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream())) return reader.ReadToEnd() == "FlangeWeb";
        }
        catch { return false; }
    }
    private static void OpenBrowser(int port)
    {
        Process.Start(new ProcessStartInfo("http://127.0.0.1:" + port + "/") { UseShellExecute = true });
    }
    [STAThread]
    private static int Main(string[] args)
    {
        bool checkOnly = args.Length == 1 && args[0] == "--check";
        Application.EnableVisualStyles();
        bool isNew;
        using (var mutex = new Mutex(true, "Local\\FlangeDrawingTool-Launcher", out isNew))
        {
            if (!isNew && !checkOnly)
            {
                try { int existing = int.Parse(File.ReadAllText(StateFile)); if (Healthy(existing)) { OpenBrowser(existing); return 0; } }
                catch { }
                MessageBox.Show("工具正在启动，请稍后再试。", "法兰图纸生成工具"); return 1;
            }
            Process server = null;
            try
            {
                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start(); int port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
                var executable = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FlangeWeb.exe");
                if (!File.Exists(executable)) throw new FileNotFoundException("找不到应用程序文件。", executable);
                server = Process.Start(new ProcessStartInfo(executable, "--urls http://127.0.0.1:" + port)
                {
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden
                });
                bool ready = false;
                for (int i = 0; i < 80; i++)
                {
                    if (server.HasExited) break;
                    if (Healthy(port)) { ready = true; break; }
                    Thread.Sleep(250);
                }
                if (!ready) throw new InvalidOperationException("本地服务未能启动，请检查安装目录和端口权限。");
                if (checkOnly) return 0;
                Directory.CreateDirectory(Path.GetDirectoryName(StateFile));
                File.WriteAllText(StateFile, port.ToString());
                var context = new ApplicationContext();
                using (var menu = new ContextMenuStrip())
                using (var tray = new NotifyIcon())
                using (var appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath))
                {
                    menu.Items.Add("打开法兰图纸工具", null, delegate { OpenBrowser(port); });
                    menu.Items.Add("退出", null, delegate { context.ExitThread(); });
                    tray.Icon = appIcon ?? SystemIcons.Application; tray.Text = "法兰图纸生成工具"; tray.ContextMenuStrip = menu; tray.Visible = true;
                    tray.DoubleClick += delegate { OpenBrowser(port); };
                    OpenBrowser(port); Application.Run(context); tray.Visible = false;
                }
                return 0;
            }
            catch (Exception ex)
            {
                if (!checkOnly) MessageBox.Show(ex.Message, "无法启动法兰图纸工具", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
            finally
            {
                if (server != null) { try { if (!server.HasExited) server.Kill(); } catch { } server.Dispose(); }
                if (!checkOnly) { try { if (File.Exists(StateFile)) File.Delete(StateFile); } catch { } }
                if (isNew) mutex.ReleaseMutex();
            }
        }
    }
}
