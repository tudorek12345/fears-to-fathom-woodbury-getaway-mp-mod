param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)
$ErrorActionPreference = "Stop"

$project = Join-Path $PSScriptRoot "..\src\WoodburySpectatorSync\WoodburySpectatorSync.csproj"

if (-not (Test-Path $project)) {
    throw "Project not found: $project"
}

dotnet build $project -c $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "Plugin build failed (exit $LASTEXITCODE)."
}
