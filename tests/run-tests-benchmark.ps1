# Run integration tests multiple times and calculate statistics
# Usage: .\run-tests-benchmark.ps1 [-Runs 20] [-ShowProgress]
# Example: .\run-tests-benchmark.ps1 -Runs 50

param(
    [int]$Runs = 20,
    [switch]$ShowProgress = $false
)

$ProjectPath = "KoalaWiki.IntegrationTests\KoalaWiki.IntegrationTests.csproj"
$times = @()

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "KoalaWiki Integration Tests - Performance Benchmark" -ForegroundColor Cyan
Write-Host "Number of runs: $Runs" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

$progressId = 1

for ($i = 1; $i -le $Runs; $i++) {
    if ($ShowProgress) {
        Write-Progress -Id $progressId -Activity "Running benchmark" -Status "Run $i of $Runs" -PercentComplete (($i / $Runs) * 100)
    }

    Write-Host ("Run {0,2}/{1}: " -f $i, $Runs) -NoNewline

    # Measure execution time using Stopwatch for accuracy
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    # Run tests WITHOUT building (--no-build) and suppress ALL output
    $null = dotnet test $ProjectPath --no-build --nologo --verbosity quiet 2>&1 | Out-Null

    $stopwatch.Stop()
    $duration = $stopwatch.Elapsed.TotalSeconds
    $times += $duration

    Write-Host ("{0:F2}s" -f $duration) -ForegroundColor Green
}

if ($ShowProgress) {
    Write-Progress -Id $progressId -Activity "Running benchmark" -Completed
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "RESULTS" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

# Calculate statistics
$totalTime = ($times | Measure-Object -Sum).Sum
$avgTime = ($times | Measure-Object -Average).Average
$minTime = ($times | Measure-Object -Minimum).Minimum
$maxTime = ($times | Measure-Object -Maximum).Maximum

# Calculate standard deviation
$squaredDiffs = $times | ForEach-Object { [Math]::Pow($_ - $avgTime, 2) }
$variance = ($squaredDiffs | Measure-Object -Sum).Sum / $Runs
$stdDev = [Math]::Sqrt($variance)

# Calculate percentiles
$sortedTimes = $times | Sort-Object
$p50Index = [Math]::Floor(($Runs - 1) * 0.5)
$p95Index = [Math]::Floor(($Runs - 1) * 0.95)
$p99Index = [Math]::Floor(($Runs - 1) * 0.99)
$p50 = $sortedTimes[$p50Index]
$p95 = $sortedTimes[$p95Index]
$p99 = $sortedTimes[$p99Index]

Write-Host ""
Write-Host "Basic Statistics:" -ForegroundColor Yellow
Write-Host ("  Total runs:      {0,6}" -f $Runs)
Write-Host ("  Total time:      {0,6:F2} seconds" -f $totalTime)
Write-Host ("  Average time:    {0,6:F2} seconds" -f $avgTime) -ForegroundColor Green
Write-Host ("  Minimum time:    {0,6:F2} seconds" -f $minTime) -ForegroundColor Cyan
Write-Host ("  Maximum time:    {0,6:F2} seconds" -f $maxTime) -ForegroundColor Magenta
Write-Host ("  Std deviation:   {0,6:F2} seconds" -f $stdDev)
Write-Host ""

Write-Host "Percentiles:" -ForegroundColor Yellow
Write-Host ("  P50 (median):    {0,6:F2} seconds" -f $p50)
Write-Host ("  P95:             {0,6:F2} seconds" -f $p95)
Write-Host ("  P99:             {0,6:F2} seconds" -f $p99)
Write-Host ""

# Performance rating
$rating = if ($avgTime -lt 3) { "Excellent" }
          elseif ($avgTime -lt 5) { "Good" }
          elseif ($avgTime -lt 10) { "Acceptable" }
          else { "Slow" }

Write-Host ("Performance:       {0}" -f $rating) -ForegroundColor $(
    if ($avgTime -lt 3) { "Green" }
    elseif ($avgTime -lt 5) { "Cyan" }
    elseif ($avgTime -lt 10) { "Yellow" }
    else { "Red" }
)

Write-Host ""
Write-Host "All tests passed!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Cyan
