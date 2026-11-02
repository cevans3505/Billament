# Builds Billament as a single self-contained .exe, installs it for the current user and adds a Start menu shortcut.
# Works in Windows PowerShell 5.1. Close Billament before running it.
# It also removes the old PrintInvoicer install (its Start menu shortcut and program folder), never its data.
param(
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA 'Programs\Billament')
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$out = Join-Path $root 'publish'

if (Get-Process -Name 'Billament', 'PrintInvoicer' -ErrorAction SilentlyContinue) {
    throw 'Billament (or the old PrintInvoicer) is running. Close it and run this again.'
}

# Unit tests and the offscreen screen tests (no windows appear).
& dotnet test (Join-Path $root 'Billament.sln') --nologo
if ($LASTEXITCODE -ne 0) { throw 'Tests failed; not publishing.' }

if (Test-Path $out) { Remove-Item (Join-Path $out '*') -Recurse -Force }
& dotnet publish (Join-Path $root 'src\Billament.App') -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -o $out --nologo
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }

New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
Copy-Item (Join-Path $out '*') $InstallDir -Recurse -Force

$exe = Join-Path $InstallDir 'Billament.exe'
$lnk = Join-Path ([Environment]::GetFolderPath('Programs')) 'Billament.lnk'
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($lnk)
$shortcut.TargetPath = $exe
$shortcut.WorkingDirectory = $InstallDir
$shortcut.IconLocation = "$exe,0"
$shortcut.Description = '3D print job invoicing'
$shortcut.Save()

# The old PrintInvoicer install from before 4.0: only its Start menu shortcut and its program folder. Its data
# (Documents\PrintInvoicer and %LOCALAPPDATA%\PrintInvoicer) is moved by Billament itself on its first start and is
# never touched here. This runs only now, after the new install and shortcut exist, so a failed publish never leaves you
# without a working program.
$oldLnk = Join-Path ([Environment]::GetFolderPath('Programs')) 'PrintInvoicer.lnk'
# AUDIT: file read. Whether the old shortcut exists and points at PrintInvoicer.exe.
if ((Test-Path -LiteralPath $oldLnk) -and ($shell.CreateShortcut($oldLnk).TargetPath -like '*\PrintInvoicer.exe')) {
    # AUDIT: file delete. The old Start menu shortcut, only when it points at PrintInvoicer.exe.
    Remove-Item -LiteralPath $oldLnk -Force
    Write-Host "Removed the old shortcut: $oldLnk"
}

# The fixed default location of the old install, never derived from -InstallDir.
$oldDir = Join-Path $env:LOCALAPPDATA 'Programs\PrintInvoicer'
# AUDIT: file read. Whether the old program folder holds PrintInvoicer.exe and no data (no .db file, no Backups or Invoices folder).
$oldIsProgramOnly = (Test-Path -LiteralPath (Join-Path $oldDir 'PrintInvoicer.exe')) -and
    -not (Get-ChildItem -LiteralPath $oldDir -Recurse -Force -Filter '*.db') -and
    -not (Test-Path -LiteralPath (Join-Path $oldDir 'Backups')) -and
    -not (Test-Path -LiteralPath (Join-Path $oldDir 'Invoices')) -and
    ((Resolve-Path -LiteralPath $oldDir).Path -ne (Resolve-Path -LiteralPath $InstallDir).Path)
if ($oldIsProgramOnly) {
    # AUDIT: file delete. The old program folder (PrintInvoicer.exe and its files), never a data folder.
    Remove-Item -LiteralPath $oldDir -Recurse -Force
    Write-Host "Removed the old program folder: $oldDir"
}
Write-Host 'If PrintInvoicer was pinned to the taskbar, unpin it and pin Billament instead.'

# Ask Explorer to refresh its icon cache so the shortcut picks up icon changes.
Start-Process -FilePath "$env:SystemRoot\System32\ie4uinit.exe" -ArgumentList '-show' -Wait -ErrorAction SilentlyContinue

Write-Host "Installed $exe"
Write-Host "Start menu shortcut: $lnk"
