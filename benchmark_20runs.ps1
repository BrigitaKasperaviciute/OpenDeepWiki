Write-Host "Running integration tests 20 times..." -ForegroundColor Green
Write-Host "=========================================="
$times = @()

for ($i = 1; $i -le 20; $i++) {
    $start = Get-Date
    dotnet test tests/KoalaWiki.IntegrationTests/KoalaWiki.IntegrationTests.csproj -q 2>&1 | Out-Null
    $end = Get-Date
    $duration = ($end - $start).TotalSeconds
    $times += $duration
    
    $durationMs = [math]::Round($duration * 1000, 2)
    $progressNum = "$i".PadLeft(2)
    Write-Host "Run ${progressNum}: ${duration}s (${durationMs}ms)"
}

$avg = ($times | Measure-Object -Average).Average
$avgMs = [math]::Round($avg * 1000, 2)

Write-Host "`n========== RESULTS ==========" -ForegroundColor Cyan
Write-Host "Total runs: 20"
Write-Host "Average execution time: ${avg}s (${avgMs}ms)"
Write-Host "========================================"
