Write-Host "Setting up git hooks..." -ForegroundColor Cyan

$hooksPath = ".githooks"

if (Test-Path $hooksPath) {
    git config core.hooksPath $hooksPath
    Write-Host "Git hooks path configured to: $hooksPath" -ForegroundColor Green
    Write-Host ""
    Write-Host "Commit message validation is now active using commitlint." -ForegroundColor Green
    Write-Host ""
    Write-Host "Commit format requirements:" -ForegroundColor Yellow
    Write-Host "  - Title: <type>(<scope>): <description>" -ForegroundColor White
    Write-Host "  - Body: Blank line after title, then description (optional but recommended)" -ForegroundColor White
    Write-Host "  - Types: feat, fix, docs, style, refactor, perf, test, chore, security, ci, config, revert" -ForegroundColor White
    Write-Host ""
    Write-Host "Run 'git commit' to test the hooks."
} else {
    Write-Host "ERROR: .githooks directory not found." -ForegroundColor Red
    exit 1
}
