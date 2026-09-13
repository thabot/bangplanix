# Bangplanix 1-Command Run Script for Windows (PowerShell)
Continue = "Stop"

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host " Bangplanix — Enterprise High Performance Reporter" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

 = 9545
 = 9546

Write-Host "[1/3] Ensuring Volume Directories..." -ForegroundColor Yellow
 = @("volumes/data", "volumes/templates", "volumes/fonts", "volumes/logs")
foreach ( in ) {
    if (-not (Test-Path )) {
        New-Item -ItemType Directory -Path  -Force | Out-Null
    }
}

Write-Host "[2/3] Building & Running Bangplanix Server..." -ForegroundColor Yellow
 = 
 = 

Write-Host "[3/3] Server starting on:" -ForegroundColor Green
Write-Host "  -> HTTP REST & Web: http://localhost:" -ForegroundColor White
Write-Host "  -> gRPC Service:    grpc://localhost:" -ForegroundColor White

dotnet run --project src/Bangplanix.Server/Bangplanix.Server.csproj
