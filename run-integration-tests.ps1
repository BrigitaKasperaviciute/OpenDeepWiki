# Script to run integration tests with server
Write-Host "Starting KoalaWiki server..." -ForegroundColor Cyan

# Start server in background job
$serverJob = Start-Job -ScriptBlock {
    Set-Location 'C:\Users\pauli\Desktop\OpenDeepWiki'
    dotnet run --project src/KoalaWiki/KoalaWiki.csproj 2>&1
}

# Wait for server to start
Write-Host "Waiting for server to start (8 seconds)..." -ForegroundColor Yellow
Start-Sleep -Seconds 8

# Check if server is running
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5085/health" -Method GET -TimeoutSec 5
    Write-Host "Server is running!" -ForegroundColor Green
} catch {
    Write-Host "Server failed to start" -ForegroundColor Red
    Stop-Job $serverJob
    Remove-Job $serverJob
    exit 1
}

# Run tests
Write-Host ""
Write-Host "Running integration tests..." -ForegroundColor Cyan
dotnet test tests/KoalaWiki.IntegrationTests/KoalaWiki.IntegrationTests.csproj --verbosity normal --no-build

$testExitCode = $LASTEXITCODE

# Clean up
Write-Host ""
Write-Host "Stopping server..." -ForegroundColor Yellow
Stop-Job $serverJob
Remove-Job $serverJob

if ($testExitCode -eq 0) {
    Write-Host "Tests completed successfully!" -ForegroundColor Green
} else {
    Write-Host "Tests failed with exit code: $testExitCode" -ForegroundColor Red
}

exit $testExitCode
