Write-Host "Updating test double names..."

$files = @(
    "tests\redmuffin.Blazor.StaticWeb.Tests\Core\ImagePlaceholderServiceTests.Helpers.cs",
    "tests\redmuffin.Blazor.StaticWeb.Tests\Core\ImageValidationCacheServiceTests.Helpers.cs",
    "tests\redmuffin.Blazor.StaticWeb.Tests\Core\PlaceholderGenerationServiceTests.Helpers.cs",
    "tests\redmuffin.Blazor.StaticWeb.Tests\Features\Cache\Services\RaindropItemsCacheTests.Helpers.cs",
    "tests\redmuffin.Blazor.StaticWeb.Tests\Features\Raindrop\Services\IRaindropAPITests.Helpers.cs",
    "tests\redmuffin.Blazor.StaticWeb.Tests\Features\Raindrop\Services\RaindropAPITests.Helpers.cs"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        Write-Host "Updating $file"
        $content = Get-Content $file -Raw
        $content = $content -replace 'TestLogger<([^>]+)>', 'Logger_Spy<$1>'
        $content = $content -replace 'class TestLogger<T>', 'class Logger_Spy<T>'
        $content = $content -replace 'TestHttpClientFactory', 'HttpClientFactory_Stub'
        $content = $content -replace 'TestDelayProvider', 'DelayProvider_Stub'
        Set-Content $file $content -NoNewline
    }
}

Write-Host "Done!"
