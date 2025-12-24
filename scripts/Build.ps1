$ErrorActionPreference = "Stop"

$project = Join-Path $PSScriptRoot "..\src\WoodburySpectatorSync\WoodburySpectatorSync.csproj"

if (-not (Test-Path $project)) {
    throw "Project not found: $project"
}

dotnet build $project -c Release
