param(
  [string]$Project = "d:\\Projects\\IOEmulator\\IOEmulator.Tests\\IOEmulator.Tests.csproj",
  [int]$ThresholdSeconds = 10
)

$ErrorActionPreference = 'Stop'

# Run once to generate TRX with per-test durations
$resultsDir = Join-Path (Split-Path $Project -Parent) 'TestResults'
$newItem = New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null
$trxPath = Join-Path $resultsDir 'bench.trx'

Write-Host "Running tests to collect durations..." -ForegroundColor Cyan
& dotnet test $Project --no-build --logger "trx;LogFileName=$(Split-Path $trxPath -Leaf)" | Out-Host

if (!(Test-Path $trxPath)) {
  Write-Error "TRX not found at $trxPath"
}

[xml]$trx = Get-Content $trxPath
$ns = @{ ns = 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010' }
$results = @()
foreach ($r in $trx.TestRun.Results.UnitTestResult) {
  $name = $r.testName
  $duration = [TimeSpan]::Parse($r.duration)
  $seconds = [math]::Round($duration.TotalSeconds, 2)
  $slow = $seconds -ge $ThresholdSeconds
  $results += [pscustomobject]@{
    Name = $name
    Seconds = $seconds
    Slow = $slow
  }
}

$results = $results | Sort-Object -Property Seconds -Descending

Write-Host "Per-test durations (>= $ThresholdSeconds s marked as SLOW):" -ForegroundColor Yellow
$results | ForEach-Object {
  $mark = if ($_.Slow) { 'SLOW' } else { '' }
  "{0,7} s`t{1} {2}" -f ($_.Seconds.ToString('0.00')), $_.Name, $mark
}
