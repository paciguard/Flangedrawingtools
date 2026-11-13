param([switch]$Offline, [string]$BuildRoot = $PSScriptRoot)
$ErrorActionPreference = 'Stop'
$setupRoot = $BuildRoot
$workspace = Split-Path -Parent $setupRoot
$project = Join-Path $workspace 'FlangeWeb\FlangeWeb.csproj'
$work = Join-Path $setupRoot ('obj\build-' + [guid]::NewGuid().ToString('N'))
$publish = Join-Path $work 'app'
$output = Join-Path $setupRoot 'Output'
New-Item -ItemType Directory -Force -Path (Join-Path $setupRoot 'obj'),$output | Out-Null
New-Item -ItemType Directory -Force -Path $publish | Out-Null
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $csc)) { throw '找不到 Windows .NET Framework C# 编译器。' }

$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:MSBuildEnableWorkloadResolver = 'false'
if (!$env:NUGET_PACKAGES -and (Test-Path -LiteralPath (Join-Path $env:USERPROFILE '.nuget\packages'))) { $env:NUGET_PACKAGES = Join-Path $env:USERPROFILE '.nuget\packages' }
$arguments = @('publish', $project, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '-p:PublishReadyToRun=false', '-p:DebugType=None', '-p:DebugSymbols=false', '-p:NuGetAudit=false', '-m:1', '-p:UseSharedCompilation=false', '-o', $publish)
if ($Offline) { $arguments += '-p:RestoreSources=C:\Program Files\dotnet\library-packs' }
& dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw '发布应用程序失败。' }
if (Get-ChildItem -LiteralPath $publish -Recurse -File -Filter 'workbook-model.json') { throw '内部分析文件不应包含在安装包中。' }
if (Test-Path -LiteralPath (Join-Path $publish 'tmp')) { throw '临时开发文件不应包含在安装包中。' }
if (!(Test-Path -LiteralPath (Join-Path $publish 'wwwroot\assets\template-border.pdf'))) { throw '缺少图纸边框模板。' }

$launcher = Join-Path $publish 'FlangeDrawingTool.exe'
$uninstaller = Join-Path $publish 'FlangeUninstall.exe'
& $csc /nologo /target:winexe /optimize+ /codepage:65001 "/out:$launcher" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll (Join-Path $setupRoot 'Launcher.cs')
if ($LASTEXITCODE -ne 0) { throw '构建启动程序失败。' }
& $csc /nologo /target:winexe /optimize+ /codepage:65001 "/out:$uninstaller" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll (Join-Path $setupRoot 'Setup.cs')
if ($LASTEXITCODE -ne 0) { throw '构建卸载程序失败。' }

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = Join-Path $work 'FlangePayload.zip'
[IO.Compression.ZipFile]::CreateFromDirectory($publish, $archive, [IO.Compression.CompressionLevel]::Optimal, $false)
$installer = Join-Path $output 'FlangeDrawingTool-Setup.exe'
& $csc /nologo /target:winexe /optimize+ /codepage:65001 "/out:$installer" "/resource:$archive,FlangePayload.zip" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll (Join-Path $setupRoot 'Setup.cs')
if ($LASTEXITCODE -ne 0) { throw '构建安装程序失败。' }
Write-Output "安装程序: $installer"

# Delete only the unique intermediate directory created for this build.
$objRoot = (Resolve-Path -LiteralPath (Join-Path $setupRoot 'obj')).ProviderPath
$resolvedWork = (Resolve-Path -LiteralPath $work).ProviderPath
$allowedPrefix = [IO.Path]::GetFullPath($objRoot).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not ([IO.Path]::GetFullPath($resolvedWork).StartsWith($allowedPrefix, [StringComparison]::OrdinalIgnoreCase)) -or
    (Split-Path -Leaf $resolvedWork) -notmatch '^build-[0-9a-f]{32}$' -or
    ((Get-Item -LiteralPath $resolvedWork).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
    throw '构建临时目录不在预期位置，未执行清理。'
}
Remove-Item -LiteralPath $resolvedWork -Recurse -Force
