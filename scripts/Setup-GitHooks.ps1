Write-Host "Setting up git hooks..." -ForegroundColor Cyan

$hooksPath = ".githooks"

if (Test-Path $hooksPath) {
    git config core.hooksPath $hooksPath
    Write-Host "Git hooks path configured to: $hooksPath" -ForegroundColor Green
    Write-Host ""
    Write-Host "Commit message validation is now active." -ForegroundColor Green
    Write-Host "Run 'git commit' to test the hooks."
} else {
    Write-Host "ERROR: .githooks directory not found at $hooksPath" -ForegroundColor Red
    exit 1
}
