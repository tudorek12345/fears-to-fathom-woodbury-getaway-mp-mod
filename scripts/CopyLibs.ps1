param(
    [Parameter(Mandatory = $true)]
    [string]$GameDir
)

$ErrorActionPreference = "Stop"

$libDir = Join-Path $PSScriptRoot "..\lib"
New-Item -ItemType Directory -Force -Path $libDir | Out-Null

$unityEnginePath = Get-ChildItem -Path $GameDir -Recurse -Filter "UnityEngine.dll" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $unityEnginePath) {
    throw "Could not find UnityEngine.dll under $GameDir."
}

$managedDir = $unityEnginePath.Directory.FullName

$required = @(
    "UnityEngine.dll",
    "UnityEngine.CoreModule.dll",
    "Assembly-CSharp.dll"
)

$optional = @(
    "UnityEngine.InputLegacyModule.dll",
    "UnityEngine.IMGUIModule.dll",
    "UnityEngine.AudioModule.dll",
    "UnityEngine.PhysicsModule.dll",
    "UnityEngine.AnimationModule.dll",
    "UnityEngine.UIModule.dll",
    "UnityEngine.SceneManagementModule.dll",
    "UnityEngine.UnityWebRequestModule.dll"
)

foreach ($dll in $required) {
    $src = Join-Path $managedDir $dll
    if (-not (Test-Path $src)) {
        throw "Missing $dll in $managedDir."
    }
    Copy-Item -Path $src -Destination $libDir -Force
}

foreach ($dll in $optional) {
    $src = Join-Path $managedDir $dll
    if (Test-Path $src) {
        Copy-Item -Path $src -Destination $libDir -Force
    }
}

$bepInExPath = Get-ChildItem -Path $GameDir -Recurse -Filter "BepInEx.dll" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $bepInExPath) {
    throw "Could not find BepInEx.dll under $GameDir. Install BepInEx 5 first."
}

Copy-Item -Path $bepInExPath.FullName -Destination $libDir -Force

Write-Host "Copied Unity + BepInEx assemblies to $libDir"
