@echo off
REM Use PowerShell for better performance and accuracy
REM Batch script timing is unreliable - redirect to PowerShell
echo Redirecting to PowerShell for accurate benchmarking...
powershell -ExecutionPolicy Bypass -File "%~dp0run-tests-benchmark.ps1"
