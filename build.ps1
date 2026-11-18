[CmdletBinding()]
param(
    [switch]$Test,
    [switch]$Installer,
    [string]$InnoCompiler
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$dist = Join-Path $root 'dist'
$portable = Join-Path $dist 'portable'

$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $framework -PathType Leaf)) {
    $framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $framework -PathType Leaf)) {
    throw 'The .NET Framework C# compiler was not found. Install .NET Framework 4.8 and use Windows PowerShell 5.1.'
}
$release = Get-ItemPropertyValue -LiteralPath 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -Name Release -ErrorAction SilentlyContinue
if (-not $release -or $release -lt 528040) {
    throw 'MicMute requires .NET Framework 4.8 or newer. Install it before building.'
}

$sources = @(Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' -File | Sort-Object Name | ForEach-Object { $_.FullName })
if ($sources.Count -eq 0) { throw 'No C# source files were found in src.' }
New-Item -ItemType Directory -Path $portable -Force | Out-Null
$compilerArgs = @('/nologo', '/codepage:65001', '/langversion:5', '/optimize+', '/platform:anycpu', '/r:System.Windows.Forms.dll', '/r:System.Drawing.dll', '/r:System.Core.dll', '/r:System.Net.Http.dll', '/r:System.Runtime.Serialization.dll', '/r:System.Xml.dll')
$manifest = Join-Path $root 'src\app.manifest'
if (Test-Path -LiteralPath $manifest -PathType Leaf) { $compilerArgs += '/win32manifest:' + $manifest }
$icon = Join-Path $root 'assets\default\mic_on.ico'
$appArgs = $compilerArgs + @('/target:winexe', '/main:MicMute.Program', ('/out:' + (Join-Path $portable 'MicMute2.exe')))
if (Test-Path -LiteralPath $icon -PathType Leaf) { $appArgs += '/win32icon:' + $icon }
& $framework @appArgs @sources
if ($LASTEXITCODE -ne 0) { throw "Application compilation failed (exit $LASTEXITCODE)." }

if ($Test) {
    $tests = @(Get-ChildItem -LiteralPath (Join-Path $root 'tests') -Filter '*.cs' -File | Sort-Object Name | ForEach-Object { $_.FullName })
    if ($tests.Count -eq 0) { throw 'No smoke tests were found in tests.' }
    $testExe = Join-Path $dist 'MicMute.SmokeTests.exe'
    $testArgs = $compilerArgs + @('/target:exe', '/main:MicMute.SmokeTests', ('/out:' + $testExe))
    & $framework @testArgs @sources @tests
    if ($LASTEXITCODE -ne 0) { throw "Test compilation failed (exit $LASTEXITCODE)." }
    & $testExe
    if ($LASTEXITCODE -ne 0) { throw "Smoke tests failed (exit $LASTEXITCODE)." }
}

function Copy-PortableFiles([string]$Destination) {
    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    $destinationIcons = Join-Path $Destination 'assets\default'
    New-Item -ItemType Directory -Path $destinationIcons -Force | Out-Null
    foreach ($name in @('LICENSE', 'README.md', 'README.ru.md', 'README.es.md', 'PUBLISHING.md', 'CHANGELOG.md', 'THIRD-PARTY-NOTICES.md')) {
        Copy-Item -LiteralPath (Join-Path $root $name) -Destination (Join-Path $Destination $name) -Force
    }
    foreach ($name in @('mic_on.ico', 'mic_off.ico')) {
        Copy-Item -LiteralPath (Join-Path $root ('assets\default\' + $name)) -Destination (Join-Path $destinationIcons $name) -Force
    }
}

Copy-PortableFiles $portable
$temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')
$staging = Join-Path $temporaryRoot ('MicMute-package-' + [Guid]::NewGuid().ToString('N'))
try {
    Copy-PortableFiles $staging
    Copy-Item -LiteralPath (Join-Path $portable 'MicMute2.exe') -Destination (Join-Path $staging 'MicMute2.exe') -Force
    Compress-Archive -Path (Join-Path $staging '*') -DestinationPath (Join-Path $dist 'MicMute-portable.zip') -Force
}
finally {
    # Delete only this build's unique staging directory, never dist or user files.
    $resolvedStaging = [IO.Path]::GetFullPath($staging)
    if ([IO.Path]::GetDirectoryName($resolvedStaging) -ne $temporaryRoot -or
        -not [IO.Path]::GetFileName($resolvedStaging).StartsWith('MicMute-package-')) {
        throw 'Refusing to remove an unexpected staging path.'
    }
    if (Test-Path -LiteralPath $resolvedStaging -PathType Container) {
        Remove-Item -LiteralPath $resolvedStaging -Recurse -Force
    }
}
Write-Host ('Portable ZIP: ' + (Join-Path $dist 'MicMute-portable.zip'))

if ($Installer) {
    if ($InnoCompiler) {
        if (-not (Test-Path -LiteralPath $InnoCompiler -PathType Leaf)) {
            throw "Inno Setup compiler does not exist: $InnoCompiler"
        }
    }
    else {
        $command = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
        if ($command) { $InnoCompiler = $command.Source }
        foreach ($version in @('Inno Setup 7', 'Inno Setup 6')) {
            foreach ($programFiles in @(${env:ProgramFiles(x86)}, $env:ProgramFiles)) {
                if (-not $InnoCompiler -and $programFiles) {
                    $candidate = Join-Path $programFiles ($version + '\ISCC.exe')
                    if (Test-Path -LiteralPath $candidate -PathType Leaf) { $InnoCompiler = $candidate }
                }
            }
        }
    }
    if (-not $InnoCompiler) {
        throw 'Inno Setup 6 or 7 was not found. Install it, add ISCC.exe to PATH, or pass -InnoCompiler "C:\path\to\ISCC.exe".'
    }
    $installerOutput = Join-Path $dist 'installer'
    New-Item -ItemType Directory -Path $installerOutput -Force | Out-Null
    & $InnoCompiler ('/DSourceRoot=' + $root) ('/DOutputDir=' + $installerOutput) (Join-Path $root 'installer\MicMute.iss')
    if ($LASTEXITCODE -ne 0) { throw "Installer compilation failed (exit $LASTEXITCODE)." }
    Write-Host ('Installer: ' + (Join-Path $installerOutput 'MicMute-Setup.exe'))
}
