# Generates HTML coverage report for KoalaWiki integration tests
param(
    [string]$TestProjectPath = "tests/KoalaWiki.IntegrationTests/KoalaWiki.IntegrationTests.csproj",
    [string]$ResultsDir = "TestResults",
    [string]$OutputDir = "coverage-report",
    [string]$RunSettings = "coverlet.runsettings"
)

Write-Host "Preparing coverage tools and folders..." -ForegroundColor Green

# Ensure reportgenerator global tool is installed
$reportGenCmd = Get-Command reportgenerator -ErrorAction SilentlyContinue
if (-not $reportGenCmd) {
    Write-Host "Installing reportgenerator global tool..." -ForegroundColor Yellow
    dotnet tool install -g dotnet-reportgenerator-globaltool
    # Re-resolve the command after install
    $reportGenCmd = Get-Command reportgenerator -ErrorAction SilentlyContinue
}

# Fallback to full path if command not resolved
$reportGenPath = $null
if ($reportGenCmd) {
    $reportGenPath = $reportGenCmd.Source
} else {
    $candidate = Join-Path $env:USERPROFILE ".dotnet/tools/reportgenerator.exe"
    if (Test-Path $candidate) {
        $reportGenPath = $candidate
    }
}

if (-not $reportGenPath) {
    Write-Host "ERROR: reportgenerator tool not found after installation." -ForegroundColor Red
    Write-Host "Ensure %USERPROFILE%\\.dotnet\\tools is on PATH and re-run." -ForegroundColor Red
    exit 1
}

# Clean previous results
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $ResultsDir
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $OutputDir
New-Item -ItemType Directory -Force -Path $ResultsDir | Out-Null

Write-Host "Running tests with coverage collection..." -ForegroundColor Green
if (Test-Path $RunSettings) {
    dotnet test $TestProjectPath --settings $RunSettings --collect:"XPlat Code Coverage" --results-directory $ResultsDir -v minimal | Out-Null
} else {
    dotnet test $TestProjectPath --collect:"XPlat Code Coverage" --results-directory $ResultsDir -v minimal | Out-Null
}
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: dotnet test failed." -ForegroundColor Red
    exit $LASTEXITCODE
}

# Find cobertura coverage reports
$reports = Get-ChildItem -Recurse -Path $ResultsDir -Filter "coverage.cobertura.xml"
if (-not $reports -or $reports.Count -eq 0) {
    Write-Host "ERROR: No coverage.cobertura.xml found in $ResultsDir." -ForegroundColor Red
    Write-Host "Check that coverlet.collector is referenced and tests executed." -ForegroundColor Red
    exit 1
}

Write-Host "Generating HTML report..." -ForegroundColor Green
$reportPaths = ($reports | ForEach-Object { $_.FullName }) -join ";"
& $reportGenPath -reports:$reportPaths -targetdir:$OutputDir -reporttypes:Html | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: reportgenerator failed." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "Coverage HTML generated at: $OutputDir/index.html" -ForegroundColor Cyan
