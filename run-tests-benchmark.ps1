# PowerShell script to run integration tests multiple times and calculate average execution time

$RUNS = 20
$TotalTime = 0
$SuccessfulRuns = 0
$TestProject = "tests/KoalaWiki.IntegrationTests/KoalaWiki.IntegrationTests.csproj"

Write-Host "Running integration tests $RUNS times to calculate average execution time..." -ForegroundColor Cyan
Write-Host "==========================================================================" -ForegroundColor Cyan
Write-Host ""

# Build once before starting benchmark to avoid rebuild overhead
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build $TestProject --nologo --verbosity quiet > $null
Write-Host "Build complete. Starting benchmark..." -ForegroundColor Green
Write-Host ""

for ($i = 1; $i -le $RUNS; $i++) {
    Write-Host "Run $i/$RUNS..." -ForegroundColor Yellow

    # Run tests and capture output (use normal verbosity with --no-build for faster runs)
    $Output = dotnet test $TestProject --nologo --verbosity normal --no-build 2>&1 | Out-String

    # Extract duration from output (multiple patterns for different output formats)
    # Pattern 1: "Time Elapsed 00:00:19.94" format
    if ($Output -match "Time Elapsed (\d+):(\d+):([\d.]+)") {
        $hours = [int]$Matches[1]
        $minutes = [int]$Matches[2]
        $seconds = [double]$Matches[3]
        $Duration = ($hours * 3600) + ($minutes * 60) + $seconds
    }
    # Pattern 2: "Total time: X.X Seconds" format
    elseif ($Output -match "Total time: ([\d.]+) Seconds") {
        $Duration = [double]$Matches[1]
    }
    # Pattern 3: "Duration: X s" format (fallback, less precise)
    elseif ($Output -match "Duration: ([\d.]+) s") {
        $Duration = [double]$Matches[1]
    }
    else {
        $Duration = 0
        Write-Host "  WARNING: Could not extract duration from output" -ForegroundColor Red
    }

    if ($Duration -gt 0) {
        # Format to show 2 decimal places for millisecond precision
        $DurationFormatted = "{0:F2}s" -f $Duration
        Write-Host "  Duration: $DurationFormatted" -ForegroundColor Green
        $TotalTime += $Duration
        $SuccessfulRuns++
    }

    # Check if tests passed (multiple patterns for different verbosity levels)
    if ($Output -match "Passed!" -or $Output -match "Test Run Successful" -or ($Output -match "Total tests: (\d+)" -and $Output -match "Passed:\s+(\d+)" -and $Matches[1] -eq $Matches[2])) {
        Write-Host "  Status: PASSED" -ForegroundColor Green
    } else {
        Write-Host "  Status: FAILED" -ForegroundColor Red
    }

    Write-Host ""
}

Write-Host "==========================================================================" -ForegroundColor Cyan
Write-Host "Benchmark Results:" -ForegroundColor Cyan
Write-Host "==========================================================================" -ForegroundColor Cyan
Write-Host "Total runs: $RUNS"
Write-Host "Successful runs: $SuccessfulRuns"

$TotalTimeFormatted = "{0:F2}s" -f $TotalTime
Write-Host "Total time: $TotalTimeFormatted"

if ($SuccessfulRuns -gt 0) {
    $Average = $TotalTime / $SuccessfulRuns
    $AverageFormatted = "{0:F2}s" -f $Average
    Write-Host "Average execution time: $AverageFormatted" -ForegroundColor Green

    # Calculate min and max if we stored them (for future enhancement)
    # For now, just show the average with proper precision
} else {
    Write-Host "ERROR: Could not calculate average (no valid durations captured)" -ForegroundColor Red
}

Write-Host "==========================================================================" -ForegroundColor Cyan
