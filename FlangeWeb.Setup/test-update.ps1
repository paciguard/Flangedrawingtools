param([string]$SetupRoot = $PSScriptRoot)
$ErrorActionPreference = 'Stop'
$testBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$testRoot = Join-Path $testBase ('flange-update-test-' + [guid]::NewGuid().ToString('N'))
$servers = @()
if (-not ([IO.Path]::GetFullPath($testRoot).StartsWith($testBase.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase))) { throw 'Invalid test directory.' }
New-Item -ItemType Directory -Path $testRoot | Out-Null
try {
    $payload = Join-Path $testRoot 'payload'
    New-Item -ItemType Directory -Path $payload | Out-Null
    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    $dummy = Join-Path $testRoot 'dummy.cs'
    [IO.File]::WriteAllText($dummy, 'using System.Threading; class Dummy { static void Main() { Thread.Sleep(120000); } }')
    & $csc /nologo /target:winexe "/out:$(Join-Path $payload 'FlangeWeb.exe')" $dummy
    if ($LASTEXITCODE -ne 0) { throw 'Dummy application compilation failed.' }
    [IO.File]::WriteAllText((Join-Path $payload 'first.txt'), 'initial')
    [IO.File]::WriteAllText((Join-Path $payload 'clrjit.dll'), 'runtime')
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = Join-Path $testRoot 'payload.zip'
    [IO.Compression.ZipFile]::CreateFromDirectory($payload, $archive)
    $exe = Join-Path $testRoot 'setup-test.exe'
    & $csc /nologo /target:winexe /codepage:65001 "/out:$exe" "/resource:$archive,FlangePayload.zip" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll (Join-Path $SetupRoot 'Setup.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Test installer compilation failed.' }
    $cancelSource = Join-Path $testRoot 'CancelTest.cs'
    @'
using System; using System.Reflection; using System.Windows.Forms;
internal static class CancelTest {
    [STAThread] public static int Main() {
        try {
            var form = new Form(); var button = new Button(); form.Controls.Add(button);
            typeof(Setup).GetMethod("ConfigureCancel", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { form, button });
            typeof(Button).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(button, new object[] { EventArgs.Empty });
            return form.IsDisposed ? 0 : 2;
        } catch { return 1; }
    }
}
'@ | Set-Content -LiteralPath $cancelSource -Encoding utf8
    $cancelExe = Join-Path $testRoot 'cancel-test.exe'
    & $csc /nologo /target:winexe /codepage:65001 /main:CancelTest "/out:$cancelExe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll (Join-Path $SetupRoot 'Setup.cs') $cancelSource
    if ($LASTEXITCODE -ne 0) { throw 'Cancel test compilation failed.' }
    $cancelProcess = Start-Process -FilePath $cancelExe -WindowStyle Hidden -Wait -PassThru
    if ($cancelProcess.ExitCode -ne 0) { throw 'Cancel did not close the installer window.' }
    function RunSetup([string]$operation, [string]$target) {
        $process = Start-Process -FilePath $exe -ArgumentList @($operation, ('"' + $target + '"')) -WindowStyle Hidden -Wait -PassThru
        return $process.ExitCode
    }
    $one = Join-Path $testRoot 'one'; $two = Join-Path $testRoot 'two'
    foreach ($target in @($one, $two)) { if ((RunSetup '--test-install' $target) -ne 0) { throw 'Test installation failed.' } }
    foreach ($target in @($one, $two)) { $servers += ,(Start-Process -FilePath (Join-Path $target 'FlangeWeb.exe') -WindowStyle Hidden -PassThru) }
    if ((RunSetup '--test-install' $one) -ne 0) { throw 'Update failed while the application was running.' }
    $servers[0].Refresh(); $servers[1].Refresh()
    if (!$servers[0].HasExited) { throw 'The target application was not stopped.' }
    if ($servers[1].HasExited) { throw 'An unrelated installation was stopped.' }
    $first = Join-Path $one 'first.txt'
    [IO.File]::WriteAllText($first, 'sentinel')
    $lock = [IO.File]::Open((Join-Path $one 'clrjit.dll'), [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        if ((RunSetup '--test-install' $one) -ne 1) { throw 'Update should fail while a file is locked.' }
        if ([IO.File]::ReadAllText($first) -ne 'sentinel') { throw 'Update changed a file before detecting the lock.' }
    } finally { $lock.Dispose() }
    Write-Output 'PASS Cancel, running update, other installation preservation, and locked-file preflight'
} finally {
    foreach ($process in $servers) { try { $process.Refresh(); if (!$process.HasExited) { Stop-Process -Id $process.Id -Force } } catch { } }
    $resolved = (Resolve-Path -LiteralPath $testRoot).ProviderPath
    if (([IO.Path]::GetFullPath($resolved).StartsWith($testBase.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) -and
        ((Split-Path -Leaf $resolved) -match '^flange-update-test-[0-9a-f]{32}$') -and
        -not ((Get-Item -LiteralPath $resolved).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
