param(
    [string]$GameDir = "C:\Games\Fears to Fathom - Woodbury Getaway",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$ProjectPath = (Join-Path $PSScriptRoot "..\src\WoodburySpectatorSync\WoodburySpectatorSync.csproj"),
    [switch]$Build
)

$ErrorActionPreference = "Stop"

$resolvedProject = Resolve-Path -LiteralPath $ProjectPath
if (-not $resolvedProject) {
    throw "Project not found: $ProjectPath"
}

if ($Build) {
    dotnet build $resolvedProject -c $Configuration
}

$projectDir = Split-Path -Parent $resolvedProject
$sourceDll = Join-Path $projectDir "bin\$Configuration\net472\WoodburySpectatorSync.dll"
if (-not (Test-Path -LiteralPath $sourceDll)) {
    throw "Build output not found: $sourceDll"
}

$pluginDir = Join-Path $GameDir "BepInEx\plugins"
New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null

$destinationDll = Join-Path $pluginDir "WoodburySpectatorSync.dll"
Copy-Item -LiteralPath $sourceDll -Destination $destinationDll -Force

$hash = (Get-FileHash -LiteralPath $destinationDll -Algorithm SHA256).Hash
$stamp = (Get-Item -LiteralPath $destinationDll).LastWriteTime

Write-Host "Deployed: $destinationDll"
Write-Host "SHA256 : $hash"
Write-Host "Stamp  : $stamp"

[pscustomobject]@{
    Source = $sourceDll
    Destination = $destinationDll
    Sha256 = $hash
    LastWriteTime = $stamp
}
