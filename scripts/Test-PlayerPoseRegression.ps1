[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
foreach ($project in @('SyncRegression','FakeSpacewar')) {
    dotnet build (Join-Path $repo "tests/$project/$project.csproj") -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw "Regression build failed: $project" }
}
$fake = Join-Path $repo 'tests/FakeSpacewar/bin/Release/net472'
# The fake helper never loads this marker; it satisfies the production file-presence check.
Set-Content -LiteralPath (Join-Path $fake 'steam_api64.dll') -Value 'Regression fixture only; not a native library.'
& (Join-Path $repo 'tests/SyncRegression/bin/Release/net472/SyncRegression.exe') $fake
if ($LASTEXITCODE -ne 0) { throw 'Player pose/Steam transport regressions failed.' }
