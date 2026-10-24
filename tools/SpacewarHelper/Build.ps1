[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$SteamworksSdk,
    [string]$BuildDirectory = (Join-Path $PSScriptRoot '../../build/spacewar-native'),
    [string]$PackageDirectory = (Join-Path $PSScriptRoot '../../build/spacewar-package')
)
$ErrorActionPreference = 'Stop'
$sdk = (Resolve-Path -LiteralPath $SteamworksSdk).Path
& cmake -S $PSScriptRoot -B $BuildDirectory -A x64 "-DSTEAMWORKS_SDK=$sdk"
if ($LASTEXITCODE -ne 0) { throw 'Spacewar configure failed' }
& cmake --build $BuildDirectory --config Release
if ($LASTEXITCODE -ne 0) { throw 'Spacewar build failed' }
& ctest --test-dir $BuildDirectory -C Release --output-on-failure
if ($LASTEXITCODE -ne 0) { throw 'Spacewar core tests failed' }
& cmake --install $BuildDirectory --config Release --prefix $PackageDirectory
if ($LASTEXITCODE -ne 0) { throw 'Spacewar packaging failed' }
Write-Output "Helper staged at $PackageDirectory/Spacewar. Copy this folder beside WoodburySpectatorSync.dll for development testing."
