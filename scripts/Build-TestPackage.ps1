[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string]$LoaderRoot,
    [string]$SevenZip = 'C:\Program Files\7-Zip\7z.exe'
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$loader = (Resolve-Path -LiteralPath $LoaderRoot).Path
if (-not (Test-Path -LiteralPath $SevenZip)) { $SevenZip = (Get-Command 7z -ErrorAction Stop).Source }
foreach ($required in @('winhttp.dll','doorstop_config.ini','BepInEx/core/BepInEx.dll','BepInEx/core/Mono.Cecil.dll')) {
    if (-not (Test-Path -LiteralPath (Join-Path $loader $required))) { throw "Missing loader file: $required" }
}
& (Join-Path $PSScriptRoot 'Build.ps1')
& (Join-Path $PSScriptRoot 'Test-SyncRegression.ps1')
$dllPath = Join-Path $repoRoot 'src/WoodburySpectatorSync/bin/Release/net472/WoodburySpectatorSync.dll'
[void][Reflection.Assembly]::LoadFrom((Join-Path $loader 'BepInEx/core/Mono.Cecil.dll'))
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($dllPath)
try {
    $plugin = $assembly.MainModule.Types | Where-Object FullName -eq 'WoodburySpectatorSync.Plugin'
    $attribute = $plugin.CustomAttributes | Where-Object { $_.AttributeType.Name -eq 'BepInPlugin' }
    $version = [string]$attribute.ConstructorArguments[2].Value
    $wire = $assembly.MainModule.Types | Where-Object FullName -eq 'WoodburySpectatorSync.Net.Protocol'
    $protocol = [int]($wire.Fields | Where-Object Name -eq 'Version').Constant
    $wirePlugin = [string]($wire.Fields | Where-Object Name -eq 'PluginVersion').Constant
} finally { $assembly.Dispose() }
$source = Get-Content -LiteralPath (Join-Path $repoRoot 'src/WoodburySpectatorSync/Plugin.cs') -Raw
if ($source -notmatch '\[BepInPlugin\([^\r\n]*"([0-9]+\.[0-9]+\.[0-9]+)"\)' -or
    $version -ne $Matches[1] -or $version -ne $wirePlugin) { throw 'Built DLL version does not match source/protocol metadata.' }

$work = Join-Path $repoRoot ('build/package-' + [guid]::NewGuid().ToString('N'))
$stage = Join-Path $work 'overlay'
New-Item -ItemType Directory -Path (Join-Path $stage 'BepInEx/plugins'),(Join-Path $stage 'BepInEx/config') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $loader 'BepInEx/core') -Destination (Join-Path $stage 'BepInEx/core') -Recurse
foreach ($name in @('winhttp.dll','doorstop_config.ini','.doorstop_version')) {
    $path = Join-Path $loader $name
    if (Test-Path -LiteralPath $path) { Copy-Item -LiteralPath $path -Destination (Join-Path $stage $name) }
}
Copy-Item -LiteralPath $dllPath -Destination (Join-Path $stage 'BepInEx/plugins/WoodburySpectatorSync.dll')
$avatars = Join-Path $repoRoot 'output/avatars/woodbury_avatars.bundle'
if (Test-Path -LiteralPath $avatars) {
    $avatarDir = Join-Path $stage 'BepInEx/plugins/WoodburySpectatorSync/avatars'
    New-Item -ItemType Directory -Path $avatarDir -Force | Out-Null
    Copy-Item -LiteralPath $avatars -Destination $avatarDir
}
@'
[General]
Mode = CoopHost
[Host]
HostBindIP = 0.0.0.0
HostPort = 27055
[Spectator]
SpectatorHostIP = 127.0.0.1
[Network]
UdpEnabled = true
UdpPort = 27056
[Coop]
AutoStartHost = false
AutoConnectClient = false
HostWaitForClient = true
ForceCabinStartSequence = false
RemotePlayerAvatarSource = Auto
[UI]
CoopMenuEnabled = true
OverlayEnabled = true
[Debug]
VerboseLogging = true
SceneDiscoveryDump = true
[Steamworks]
AppIdMode = Disabled
'@ | Set-Content -LiteralPath (Join-Path $stage 'BepInEx/config/com.woodbury.spectatorsync.cfg') -Encoding UTF8
@"
Woodbury Co-op $version - development test build / protocol $protocol

1. Each player needs their own valid game installation.
2. Extract the CONTENTS of this archive into the game root. winhttp.dll and
   doorstop_config.ini must sit next to Fears to Fathom - Woodbury Getaway.exe.
3. Launch the game and press F11. Host selects CoopHost; friend selects CoopClient.
4. Enter the host's reachable LAN/VPN IP. Internet hosting requires reachable
   TCP 27055 and UDP 27056 (port forwarding or a virtual LAN).
5. Both players should use this same package. F8 shows diagnostics; F10 dumps state.

BepInEx is included. No separate SDK, source build, or game files are included.
This package disables the forced Cabin test start and Steamworks test-app override.
The network transport is direct TCP/UDP; Steam friend invites/relay are not implemented.
Back up an existing custom mod config before replacing it with this test config.

Validation: Release build and snapshot/log regression checks passed.
A complete two-player story run of this build is still pending.
Known gaps: full save/resume parity, reconnect validation, ending/cinematic edge cases.
Cabin client-catch/death mirroring has historical 0.4.43 log evidence (2026-07-08).
"@ | Set-Content -LiteralPath (Join-Path $stage 'README_INSTALL.txt') -Encoding UTF8

$commit = (git -C $repoRoot rev-parse HEAD).Trim()
$dirty = -not [string]::IsNullOrWhiteSpace((git -C $repoRoot status --porcelain -- src scripts | Out-String))
$manifest = [ordered]@{
    plugin = $version; protocol = $protocol; builtAtUtc = [DateTime]::UtcNow.ToString('o')
    sourceCommit = $commit; sourceHasLocalChanges = $dirty
    pluginSha256 = (Get-FileHash -LiteralPath $dllPath -Algorithm SHA256).Hash.ToLowerInvariant()
    validation = 'Release build and snapshot/log regression checks passed; full gameplay pass pending.'
}
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $stage 'BUILD_INFO.json') -Encoding UTF8
$checksums = Get-ChildItem -LiteralPath $stage -Recurse -File | Sort-Object FullName | ForEach-Object {
    (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash + '  ' + $_.FullName.Substring($stage.Length + 1)
}
$checksums | Set-Content -LiteralPath (Join-Path $stage 'CHECKSUMS_SHA256.txt') -Encoding UTF8
$archive = Join-Path $work 'woodbury-coop-latest.7z'
Push-Location -LiteralPath $stage
try {
    & $SevenZip a -t7z $archive '*' | Out-Null
    if ($LASTEXITCODE -ne 0) { throw '7-Zip packaging failed.' }
    & $SevenZip t $archive | Out-Null
    if ($LASTEXITCODE -ne 0) { throw '7-Zip integrity check failed.' }
} finally { Pop-Location }
$manifest['archiveSha256'] = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
$manifest['sizeBytes'] = (Get-Item -LiteralPath $archive).Length
$downloads = Join-Path $repoRoot 'site/downloads'
Copy-Item -LiteralPath $archive -Destination (Join-Path $downloads 'woodbury-coop-latest.7z') -Force
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $downloads 'build-info.json') -Encoding UTF8
Write-Host "Packaged $version / protocol $protocol with loader files at archive root."
