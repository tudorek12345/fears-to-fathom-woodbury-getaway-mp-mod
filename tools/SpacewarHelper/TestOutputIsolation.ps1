[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$HelperPath)
$ErrorActionPreference = 'Stop'
$start = [Diagnostics.ProcessStartInfo]::new()
$start.FileName = (Resolve-Path -LiteralPath $HelperPath).Path
$start.Arguments = '--test-stdout-isolation'
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$start.RedirectStandardOutput = $true
$start.RedirectStandardError = $true
$process = [Diagnostics.Process]::Start($start)
try {
    $buffer = [IO.MemoryStream]::new()
    $process.StandardOutput.BaseStream.CopyTo($buffer)
    $diagnostics = $process.StandardError.ReadToEnd()
    if (-not $process.WaitForExit(10000)) {
        $process.Kill()
        throw 'Spacewar output isolation probe timed out'
    }
    if ($process.ExitCode -ne 0) { throw "Spacewar output isolation probe failed: $diagnostics" }
    $expected = [byte[]]@(9,0,0,0,6,0,109,0,120,86,52,18,1)
    $actual = $buffer.ToArray()
    if ($actual.Length -ne $expected.Length) {
        throw "Binary IPC pipe contains diagnostic output: expected $($expected.Length) bytes, got $($actual.Length)"
    }
    for ($i=0; $i -lt $expected.Length; $i++) {
        if ($actual[$i] -ne $expected[$i]) { throw "Binary IPC byte $i differs from the framed acknowledgement" }
    }
    foreach ($message in @('cpp stdout diagnostic','crt stdout diagnostic','win32 stdout diagnostic')) {
        if (-not $diagnostics.Contains($message)) { throw "Missing redirected diagnostic: $message" }
    }
    Write-Output 'Spacewar binary stdout isolated; C++, CRT and Win32 diagnostics reached stderr.'
}
finally { $process.Dispose() }
