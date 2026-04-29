param(
    [string]$UnityPath = "",
    [string]$ProjectPath = "",
    [string]$OutputDir = "",
    [string]$BundleName = "woodbury_avatars.bundle",
    [string]$InstallToGameDir = ""
)

$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path

function Resolve-RepoPath {
    param(
        [string]$PathValue
    )

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return [System.IO.Path]::GetFullPath($PathValue)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $RepoRoot $PathValue))
}

function Resolve-UnityEditor {
    param(
        [string]$ExplicitPath
    )

    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) {
        if (-not (Test-Path -LiteralPath $ExplicitPath)) {
            throw "UnityPath does not exist: $ExplicitPath"
        }

        return (Resolve-Path -LiteralPath $ExplicitPath).Path
    }

    $hubRoots = @(
        "C:\Program Files\Unity\Hub\Editor",
        (Join-Path $env:USERPROFILE "Unity\Hub\Editor")
    )
    foreach ($hubRoot in $hubRoots) {
        if (Test-Path -LiteralPath $hubRoot) {
            $candidate = Get-ChildItem -LiteralPath $hubRoot -Directory |
                Where-Object { $_.Name -like "2021.3.*" } |
                Sort-Object Name -Descending |
                Select-Object -First 1
            if ($candidate -ne $null) {
                $unityExe = Join-Path $candidate.FullName "Editor\Unity.exe"
                if (Test-Path -LiteralPath $unityExe) {
                    return (Resolve-Path -LiteralPath $unityExe).Path
                }
            }
        }
    }

    throw "Unity 2021.3.x editor was not found. Install Unity 2021.3.x or pass -UnityPath."
}

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Join-Path $RepoRoot "tools\AvatarBundle"
} else {
    $ProjectPath = Resolve-RepoPath -PathValue $ProjectPath
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $RepoRoot "output\avatars"
} else {
    $OutputDir = Resolve-RepoPath -PathValue $OutputDir
}

$ProjectPath = [System.IO.Path]::GetFullPath($ProjectPath)
$OutputDir = [System.IO.Path]::GetFullPath($OutputDir)
$ResolvedUnityPath = Resolve-UnityEditor -ExplicitPath $UnityPath

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
$LogDir = Join-Path $RepoRoot "output\logs"
New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
$LogFile = Join-Path $LogDir "AvatarBundleBuild.log"

$unityArgs = @(
    "-batchmode",
    "-quit",
    "-projectPath", $ProjectPath,
    "-executeMethod", "WoodburyAvatarBundle.BuildAvatarBundle.Build",
    "-outputDir", $OutputDir,
    "-bundleName", $BundleName,
    "-logFile", $LogFile
)

function Quote-ProcessArg {
    param(
        [string]$Value
    )

    if ($null -eq $Value) {
        return '""'
    }

    if ($Value.IndexOfAny([char[]]" `t`"") -ge 0) {
        return '"' + $Value.Replace('"', '\"') + '"'
    }

    return $Value
}

$BundlePath = Join-Path $OutputDir $BundleName
$unityArgLine = ($unityArgs | ForEach-Object { Quote-ProcessArg -Value $_ }) -join " "
$unityProcess = Start-Process -FilePath $ResolvedUnityPath -ArgumentList $unityArgLine -Wait -PassThru -WindowStyle Hidden
$unityExitCode = $unityProcess.ExitCode
$logReportedSuccess = (Test-Path -LiteralPath $LogFile) -and
    [bool](Select-String -LiteralPath $LogFile -Pattern "Woodbury avatar bundle written:" -Quiet)
if ($unityExitCode -ne 0 -and (-not (Test-Path -LiteralPath $BundlePath) -or -not $logReportedSuccess)) {
    throw "Unity avatar bundle build failed with exit code $unityExitCode. See log: $LogFile"
}

if ($unityExitCode -ne 0) {
    Write-Warning "Unity returned exit code $unityExitCode, but the bundle was produced and the build log reported success. See log: $LogFile"
}

if (Test-Path -LiteralPath $LogFile) {
    $disabledModuleWarning = Select-String -LiteralPath $LogFile -Pattern @(
        "module Animation is disabled",
        "module AssetBundle is disabled",
        "'AssetBundle' is not supported",
        "AssetBundleModule.*disabled",
        "AnimationModule.*disabled"
    ) -Quiet
    if ($disabledModuleWarning) {
        throw "Avatar bundle build produced disabled Unity module warnings. See log: $LogFile"
    }
}

if (-not (Test-Path -LiteralPath $BundlePath)) {
    throw "Avatar bundle was not produced: $BundlePath"
}

if (-not [string]::IsNullOrWhiteSpace($InstallToGameDir)) {
    $InstallDir = Join-Path $InstallToGameDir "BepInEx\plugins\WoodburySpectatorSync\avatars"
    New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
    Copy-Item -LiteralPath $BundlePath -Destination (Join-Path $InstallDir $BundleName) -Force
}

[pscustomobject]@{
    UnityPath = $ResolvedUnityPath
    ProjectPath = $ProjectPath
    BundlePath = (Resolve-Path -LiteralPath $BundlePath).Path
    LogFile = $LogFile
    InstalledTo = if ([string]::IsNullOrWhiteSpace($InstallToGameDir)) { $null } else { $InstallDir }
}
