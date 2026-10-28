# Build-Presentation.ps1
# Builds the PCH Agent Workflow Marp presentation

param(
    [switch]$Watch,
    [switch]$Preview
)

$SourceFile = "pch-agent-workflow.md"
$OutputFile = "pch-agent-workflow.html"

if (-not (Test-Path $SourceFile)) {
    Write-Host "Error: $SourceFile not found" -ForegroundColor Red
    exit 1
}

Write-Host "Building presentation..." -ForegroundColor Cyan

if ($Watch) {
    Write-Host "Starting watch mode (Ctrl+C to stop)..." -ForegroundColor Yellow
    & marp --html --allow-local-files --watch $SourceFile -o $OutputFile
} elseif ($Preview) {
    Write-Host "Starting preview mode..." -ForegroundColor Yellow
    & marp --html --allow-local-files --preview $SourceFile
} else {
    & marp --html --allow-local-files $SourceFile -o $OutputFile
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✓ Generated: $OutputFile" -ForegroundColor Green
        Write-Host "  Open in browser to view presentation" -ForegroundColor Gray
    } else {
        Write-Host "`n✗ Build failed" -ForegroundColor Red
        exit 1
    }
}
