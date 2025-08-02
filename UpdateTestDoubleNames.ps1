# PowerShell script to update remaining test double names
Write-Host "Updating remaining test double names..." -ForegroundColor Green

# Get all Helper files that need updating
$helperFiles = @(
    "tests\redmuffin.Blazor.StaticWeb.Tests\Core\ImagePlaceholderServiceTests.Helpers.cs",
    "tests\redmuffin.Blazor.StaticWeb.Tests\Core\ImageValidationCacheServiceTests.Helpers.cs",
    "tests\redmuffin.Blazor.StaticWeb.Tests\Core\PlaceholderGenerationServiceTests.Helpers.cs",
    "tests\redmuffin.Blazor.StaticWeb.Tests\Features\Cache\Services\RaindropItemsCacheTests.Helpers.cs",
    "tests\redmuffin.Blazor.StaticWeb.Tests\Features\Raindrop\Services\IRaindropAPITests.Helpers.cs",
    "tests\redmuffin.Blazor.StaticWeb.Tests\Features\Raindrop\Services\RaindropAPITests.Helpers.cs"
)

$totalFiles = $helperFiles.Count
$currentFile = 0

foreach ($file in $helperFiles) {
    $currentFile++
    $fullPath = Join-Path $PSScriptRoot $file
    
    if (Test-Path $fullPath) {
        Write-Host "[$currentFile/$totalFiles] Updating: $file" -ForegroundColor Yellow
        
        $content = Get-Content $fullPath -Raw
        
        # Update TestLogger<T> to Logger_Spy<T>
        $content = $content -replace 'TestLogger<([^>]+)>', 'Logger_Spy<$1>'
        $content = $content -replace 'class TestLogger<T>', 'class Logger_Spy<T>'
        $content = $content -replace 'public TestLogger<T>', 'public Logger_Spy<T>'
        $content = $content -replace 'sealed class TestLogger<T>', 'sealed class Logger_Spy<T>'
        $content = $content -replace 'new TestLogger<([^>]+)>\(\)', 'new Logger_Spy<$1>()'
        
        # Update TestHttpClientFactory to HttpClientFactory_Stub
        $content = $content -replace 'TestHttpClientFactory', 'HttpClientFactory_Stub'
        
        # Update TestDelayProvider to DelayProvider_Stub
        $content = $content -replace 'TestDelayProvider', 'DelayProvider_Stub'
        
        Set-Content $fullPath $content -NoNewline
        Write-Host "  ✓ Updated successfully" -ForegroundColor Green
    } else {
        Write-Host "  ✗ File not found: $fullPath" -ForegroundColor Red
    }
}

Write-Host "`nAll updates completed!" -ForegroundColor Green
Write-Host "Running build to verify changes..." -ForegroundColor Cyan

# Run build to verify
$buildResult = & dotnet build --no-restore --verbosity quiet
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Build successful - all updates applied correctly!" -ForegroundColor Green
} else {
    Write-Host "✗ Build failed - please check for errors" -ForegroundColor Red
    Write-Host $buildResult
}
