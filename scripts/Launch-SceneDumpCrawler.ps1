param(
    [string]$GameDir = "C:\Users\tudor\OneDrive\Plocha\Fears.to.Fathom.Woodbury.Getaway",
    [string]$Scenes = "",
    [float]$TimedDumpIntervalSeconds = 10.0,
    [float]$CrawlerStartDelaySeconds = 20.0,
    [float]$CrawlerSceneSeconds = 30.0,
    [switch]$Loop,
    [switch]$NoBuild,
    [switch]$ForceStopExisting
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

if (-not $NoBuild.IsPresent) {
    powershell -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "Deploy-Coop.ps1") `
        -GameDir $GameDir `
        -Build
}

powershell -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "Launch-CoopPair.ps1") `
    -GameDir $GameDir `
    -AutoStartHost `
    -AutoConnectClient `
    -EnableSessionLog `
    -SceneDiscoveryDumpIntervalSeconds $TimedDumpIntervalSeconds `
    -ExperimentalSceneDumpCrawler `
    -SceneDumpCrawlerScenes $Scenes `
    -SceneDumpCrawlerStartDelaySeconds $CrawlerStartDelaySeconds `
    -SceneDumpCrawlerSceneSeconds $CrawlerSceneSeconds `
    -SceneDumpCrawlerLoop:$Loop `
    -ForceStopExisting:$ForceStopExisting
