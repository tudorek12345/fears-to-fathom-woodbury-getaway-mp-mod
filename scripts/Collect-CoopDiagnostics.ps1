[CmdletBinding()]
param([string]$GameDir = (Get-Location).Path, [string]$OutputDirectory = '')
$ErrorActionPreference='Stop'
$game=(Resolve-Path -LiteralPath $GameDir).Path
if(-not(Test-Path -LiteralPath (Join-Path $game 'BepInEx'))) { throw 'GameDir must contain the installed BepInEx folder.' }
if(-not $OutputDirectory) { $OutputDirectory=Join-Path $game 'CoopDiagnostics' }
$run=Join-Path $OutputDirectory (Get-Date -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $run -Force | Out-Null
$metadata=[ordered]@{capturedAtUtc=[DateTime]::UtcNow.ToString('o');game=$game;steamRunning=[bool](Get-Process steam -ErrorAction SilentlyContinue);files=@();logs=@()}
foreach($relative in @('BepInEx/plugins/WoodburySpectatorSync.dll','BepInEx/plugins/Spacewar/Woodbury.Spacewar.exe','BepInEx/plugins/Spacewar/steam_api64.dll','BepInEx/plugins/Spacewar/steam_appid.txt')) {
    $path=Join-Path $game $relative
    $metadata.files += if(Test-Path -LiteralPath $path) { [ordered]@{path=$relative;sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash;bytes=(Get-Item -LiteralPath $path).Length} } else { [ordered]@{path=$relative;missing=$true} }
}
$logs=@()
$bep=Join-Path $game 'BepInEx/LogOutput.log'
if(Test-Path -LiteralPath $bep) { $logs+=Get-Item -LiteralPath $bep }
$logDir=Join-Path $game 'BepInEx/logs'
if(Test-Path -LiteralPath $logDir) { $logs+=Get-ChildItem -LiteralPath $logDir -File -Filter '*.log' | Sort-Object LastWriteTime -Descending | Select-Object -First 12 }
$unity=Join-Path $env:USERPROFILE 'AppData/LocalLow/Rayll Studios/Fears to Fathom - Woodbury Getaway'
foreach($name in @('Player.log','Player-prev.log')) { $path=Join-Path $unity $name; if(Test-Path -LiteralPath $path) { $logs+=Get-Item -LiteralPath $path } }
foreach($log in $logs) {
    $destination=Join-Path $run $log.Name
    Copy-Item -LiteralPath $log.FullName -Destination $destination
    $metadata.logs += [ordered]@{name=$log.Name;lastWriteTimeUtc=$log.LastWriteTimeUtc.ToString('o');bytes=$log.Length}
}
$metadata | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $run 'diagnostics.json') -Encoding UTF8
'These are local logs and installed-file hashes. Nothing was uploaded. Logs may contain player names, lobby IDs and network addresses.' | Set-Content -LiteralPath (Join-Path $run 'README.txt') -Encoding UTF8
$archive=$run+'.zip'
Compress-Archive -LiteralPath @(Get-ChildItem -LiteralPath $run -File | ForEach-Object {$_.FullName}) -DestinationPath $archive
Write-Output $archive
