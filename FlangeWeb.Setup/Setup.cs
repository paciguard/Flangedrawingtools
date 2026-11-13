using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

internal static class Setup
{
    private const string Product = "法兰图纸生成工具";
    private const string ManifestName = ".flange-install-manifest";
    private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\FlangeDrawingTool";
    private static string DefaultDirectory { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "FlangeDrawingTool"); } }
    private static string ProgramDirectory { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "法兰图纸生成工具"); } }
    private static string ShortcutName { get { return Product + ".lnk"; } }
    private static string FullTarget(string root, string relative)
    {
        string basePath = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string target = Path.GetFullPath(Path.Combine(basePath, relative));
        if (!target.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("安装包中包含无效路径。");
        return target;
    }
    private static void Shortcut(string linkPath, string target)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType == null) throw new InvalidOperationException("无法创建快捷方式。");
        object shell = Activator.CreateInstance(shellType);
        object link = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { linkPath });
        Type t = link.GetType();
        t.InvokeMember("TargetPath", BindingFlags.SetProperty, null, link, new object[] { target });
        t.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, link, new object[] { Path.GetDirectoryName(target) });
        t.InvokeMember("Description", BindingFlags.SetProperty, null, link, new object[] { Product });
        t.InvokeMember("Save", BindingFlags.InvokeMethod, null, link, null);
    }
    private static void Install(string directory, bool shortcuts, bool desktop)
    {
        string root = Path.GetFullPath(directory);
        string manifest = Path.Combine(root, ManifestName);
        if (shortcuts)
        {
            using (RegistryKey existing = Registry.CurrentUser.OpenSubKey(RegistryPath))
            {
                string previous = existing == null ? null : existing.GetValue("InstallLocation") as string;
                if (!String.IsNullOrEmpty(previous) && !String.Equals(Path.GetFullPath(previous).TrimEnd(Path.DirectorySeparatorChar), root.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("已安装到其他目录，请先卸载现有版本。");
            }
        }
        if (Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any() && !File.Exists(manifest))
            throw new InvalidOperationException("安装目录不是空目录，请选择新的空目录。");
        Directory.CreateDirectory(root);
        var paths = File.Exists(manifest) ? new List<string>(File.ReadAllLines(manifest, Encoding.UTF8)) : new List<string>();
        try
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FlangePayload.zip"))
            {
                if (stream == null) throw new InvalidOperationException("安装包数据缺失。");
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (String.IsNullOrEmpty(entry.Name)) continue;
                        string relative = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                        string target = FullTarget(root, relative);
                        Directory.CreateDirectory(Path.GetDirectoryName(target));
                        if (!paths.Contains(relative, StringComparer.OrdinalIgnoreCase)) paths.Add(relative);
                        using (Stream source = entry.Open())
                        using (Stream destination = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None)) source.CopyTo(destination);
                    }
                }
            }
        }
        finally { if (paths.Count > 0) File.WriteAllLines(manifest, paths.ToArray(), new UTF8Encoding(false)); }
        if (shortcuts)
        {
            Directory.CreateDirectory(ProgramDirectory);
            string launcher = Path.Combine(root, "FlangeDrawingTool.exe");
            Shortcut(Path.Combine(ProgramDirectory, ShortcutName), launcher);
            if (desktop) Shortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), ShortcutName), launcher);
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath))
            {
                key.SetValue("DisplayName", Product); key.SetValue("DisplayVersion", "1.0");
                key.SetValue("DisplayIcon", launcher); key.SetValue("InstallLocation", root);
                key.SetValue("UninstallString", "\"" + Path.Combine(root, "FlangeUninstall.exe") + "\" --uninstall");
                key.SetValue("NoModify", 1, RegistryValueKind.DWord); key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            }
        }
    }
    private static void Remove(string directory, bool shortcuts)
    {
        string root = Path.GetFullPath(directory);
        string manifest = Path.Combine(root, ManifestName);
        if (!File.Exists(manifest)) throw new InvalidOperationException("未找到安装记录，无法卸载。");
        string self = Path.GetFullPath(Assembly.GetExecutingAssembly().Location);
        var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool deleteSelf = false;
        foreach (string relative in File.ReadAllLines(manifest, Encoding.UTF8))
        {
            if (String.IsNullOrWhiteSpace(relative)) continue;
            string path = FullTarget(root, relative);
            if (String.Equals(path, self, StringComparison.OrdinalIgnoreCase)) { deleteSelf = true; continue; }
            if (File.Exists(path)) File.Delete(path);
            string parent = Path.GetDirectoryName(path);
            while (!String.IsNullOrEmpty(parent) && parent.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) { folders.Add(parent); parent = Path.GetDirectoryName(parent); }
        }
        File.Delete(manifest);
        if (shortcuts)
        {
            string programLink = Path.Combine(ProgramDirectory, ShortcutName), desktopLink = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), ShortcutName);
            if (File.Exists(programLink)) File.Delete(programLink);
            if (File.Exists(desktopLink)) File.Delete(desktopLink);
            try { if (Directory.Exists(ProgramDirectory)) Directory.Delete(ProgramDirectory, false); } catch (IOException) { }
            Registry.CurrentUser.DeleteSubKeyTree(RegistryPath, false);
        }
        foreach (string folder in folders.OrderByDescending(x => x.Length)) try { if (Directory.Exists(folder)) Directory.Delete(folder, false); } catch (IOException) { }
        if (deleteSelf)
        {
            // Delete the running uninstaller after it exits; never remove a non-empty folder.
            string quotedSelf = self.Replace("'", "''"), quotedRoot = root.Replace("'", "''");
            string script = "Wait-Process -Id " + Process.GetCurrentProcess().Id + " -ErrorAction SilentlyContinue; Remove-Item -LiteralPath '" + quotedSelf + "' -Force -ErrorAction SilentlyContinue; try { [IO.Directory]::Delete('" + quotedRoot + "', $false) } catch {}";
            string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
            Process.Start(new ProcessStartInfo("powershell.exe", "-NoProfile -NonInteractive -EncodedCommand " + encoded) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden });
        }
        else try { if (Directory.Exists(root)) Directory.Delete(root, false); } catch (IOException) { }
    }
    [STAThread]
    private static int Main(string[] args)
    {
        if (!Environment.Is64BitOperatingSystem) { MessageBox.Show("此安装程序需要 64 位 Windows。", Product); return 1; }
        if (args.Length == 2 && (args[0] == "--test-install" || args[0] == "--test-remove"))
        {
            try { if (args[0] == "--test-install") Install(args[1], false, false); else Remove(args[1], false); return 0; }
            catch { return 1; }
        }
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        if (args.Length == 1 && args[0] == "--uninstall")
        {
            if (MessageBox.Show("卸载法兰图纸生成工具？", Product, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return 0;
            try { Remove(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar), true); MessageBox.Show("卸载完成。", Product); return 0; }
            catch (Exception ex) { MessageBox.Show(ex.Message, "卸载失败", MessageBoxButtons.OK, MessageBoxIcon.Error); return 1; }
        }
        using (var form = new Form { Text = Product + " - 安装", ClientSize = new Size(540, 205), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, StartPosition = FormStartPosition.CenterScreen })
        {
            var heading = new Label { Text = "安装法兰图纸生成工具", Left = 18, Top = 15, Width = 500, Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold) };
            var label = new Label { Text = "安装位置：", Left = 18, Top = 56, Width = 90 };
            var path = new TextBox { Left = 18, Top = 78, Width = 405, Text = DefaultDirectory };
            var browse = new Button { Left = 432, Top = 76, Width = 90, Text = "浏览…" };
            var desktop = new CheckBox { Left = 18, Top = 115, Width = 240, Text = "创建桌面快捷方式", Checked = true };
            var install = new Button { Left = 336, Top = 153, Width = 90, Text = "安装" };
            var cancel = new Button { Left = 432, Top = 153, Width = 90, Text = "取消", DialogResult = DialogResult.Cancel };
            browse.Click += delegate { using (var dialog = new FolderBrowserDialog()) { dialog.Description = "选择空的安装目录"; if (dialog.ShowDialog(form) == DialogResult.OK) path.Text = dialog.SelectedPath; } };
            install.Click += delegate {
                try { install.Enabled = false; form.UseWaitCursor = true; Application.DoEvents(); Install(path.Text, true, desktop.Checked); form.UseWaitCursor = false;
                    if (MessageBox.Show("安装完成。现在打开工具？", Product, MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes) Process.Start(Path.Combine(Path.GetFullPath(path.Text), "FlangeDrawingTool.exe"));
                    form.DialogResult = DialogResult.OK; form.Close();
                } catch (Exception ex) { form.UseWaitCursor = false; install.Enabled = true; MessageBox.Show(ex.Message, "安装失败", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };
            form.Controls.AddRange(new Control[] { heading, label, path, browse, desktop, install, cancel }); form.AcceptButton = install; form.CancelButton = cancel; Application.Run(form);
        }
        return 0;
    }
}
