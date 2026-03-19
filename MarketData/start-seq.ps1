# PowerShell script to start Seq in Docker for MarketData logging

Write-Host "Starting Seq container..." -ForegroundColor Green

# Check if container already exists
$container = docker ps -aq -f name=seq

if ($container) {
    Write-Host "Seq container already exists. Starting it..." -ForegroundColor Yellow
    docker start seq
} else {
    Write-Host "Creating new Seq container..." -ForegroundColor Yellow
    docker run --name seq -d --restart unless-stopped `
      -e ACCEPT_EULA=Y `
      -p 5341:80 `
      -v seq-data:/data `
      datalust/seq:latest
}

Write-Host ""
Write-Host "Seq is now running!" -ForegroundColor Green
Write-Host "UI: http://localhost:5341" -ForegroundColor Cyan
Write-Host ""
Write-Host "Commands:" -ForegroundColor Yellow
Write-Host "  To stop Seq: docker stop seq"
Write-Host "  To view logs: docker logs seq"
Write-Host "  To remove: docker rm seq"
