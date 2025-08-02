# PowerShell script to rename all test doubles to follow the new naming convention
# This script processes all *Helpers.cs files in the test directories
# 
# Author: Agent Mode
# Purpose: Standardize test double naming with underscore separator
# Format: OldName -> InterfaceName_TestDoubleType

param(
    [switch]$DryRun = $false,  # Show what would be changed without making changes
    [switch]$Backup = $true    # Create backup files before changes
)

Write-Host "=== Test Double Renaming Script ===" -ForegroundColor Green
Write-Host "Dry Run Mode: $DryRun" -ForegroundColor Yellow
Write-Host "Backup Mode: $Backup" -ForegroundColor Yellow
Write-Host ""

# Define the renaming mappings
$renameMappings = @{
    # Navigation-related mocks
    'NavigationManagerMock' = 'NavigationManager_Mock'
    'ThrowingNavigationManagerMock' = 'NavigationManager_ThrowingMock'
    'FaultyNavigationManagerMock' = 'NavigationManager_FaultyMock'
    
    # Image-related mocks
    'SimpleImagePlaceholderServiceMock' = 'ImagePlaceholderService_Mock'
    'ImagePlaceholderServiceMock' = 'ImagePlaceholderService_Mock'
    'SimpleImageValidationCacheServiceMock' = 'ImageValidationCacheService_Mock'
    'ImageValidationCacheServiceMock' = 'ImageValidationCacheService_Mock'
    'SimpleImageValidationServiceMock' = 'ImageValidationService_Mock'
    
    # Raindrop API-related mocks
    'SimpleRaindropAPIMock' = 'RaindropAPI_Mock'
    'RaindropAPIMock' = 'RaindropAPI_Mock'
    'FailingRaindropAPIMock' = 'RaindropAPI_FailingMock'
    'EmptyRaindropAPIMock' = 'RaindropAPI_EmptyMock'
    
    # HTTP-related mocks and handlers
    'HttpMessageHandlerMock' = 'HttpMessageHandler_Mock'
    'CancellationAwareHttpMessageHandlerMock' = 'HttpMessageHandler_CancellationAwareMock'
    'FailingHttpMessageHandlerMock' = 'HttpMessageHandler_FailingMock'
    'FastTimeoutHttpMessageHandler' = 'HttpMessageHandler_FastTimeoutMock'
    'TestHttpMessageHandler' = 'HttpMessageHandler_Stub'
    'TestHttpMessageHandlerFailing' = 'HttpMessageHandler_FailingStub'
    'TestHttpMessageHandlerMalformed' = 'HttpMessageHandler_MalformedStub'
    'TestHttpMessageHandlerMissingFiles' = 'HttpMessageHandler_MissingFilesStub'
    'TestHttpMessageHandlerRealAPI' = 'HttpMessageHandler_RealAPIStub'
    
    # Storage and cache mocks
    'LocalStorageServiceMock' = 'LocalStorageService_Mock'
    'CacheServiceMock' = 'CacheService_Mock'
    
    # Azure Functions mocks
    'MockBindingContext' = 'BindingContext_Mock'
    'MockFunctionContext' = 'FunctionContext_Mock'
    'MockFunctionDefinition' = 'FunctionDefinition_Mock'
    'MockHttpRequestData' = 'HttpRequestData_Mock'
    'MockHttpResponseData' = 'HttpResponseData_Mock'
    'MockTraceContext' = 'TraceContext_Mock'
    
    # Other test doubles
    'TestJSRuntime' = 'JSRuntime_Stub'
    'MockClaimsIdentity' = 'ClaimsIdentity_Mock'
}

# Get all Helper files in test directories
$helperFiles = Get-ChildItem -Recurse -Filter "*Helpers.cs" | Where-Object { $_.FullName -like "*tests*" }

Write-Host "Found $($helperFiles.Count) Helper files to process:" -ForegroundColor Cyan
$helperFiles | ForEach-Object { Write-Host "  $($_.FullName)" -ForegroundColor Gray }
Write-Host ""

$totalChanges = 0
$filesChanged = 0

foreach ($file in $helperFiles) {
    Write-Host "Processing: $($file.Name)" -ForegroundColor Yellow
    
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    $fileChanges = 0
    
    # Apply each renaming mapping
    foreach ($oldName in $renameMappings.Keys) {
        $newName = $renameMappings[$oldName]
        
        # Count occurrences before replacement
        $matches = [regex]::Matches($content, "\b$([regex]::Escape($oldName))\b")
        if ($matches.Count -gt 0) {
            Write-Host "  $oldName -> $newName ($($matches.Count) occurrences)" -ForegroundColor Green
            $fileChanges += $matches.Count
            
            if (-not $DryRun) {
                # Use word boundary regex to avoid partial matches
                $content = $content -replace "\b$([regex]::Escape($oldName))\b", $newName
            }
        }
    }
    
    # If changes were made and not in dry run mode
    if ($fileChanges -gt 0 -and -not $DryRun) {
        # Create backup if requested
        if ($Backup) {
            $backupPath = $file.FullName + ".backup"
            Copy-Item $file.FullName $backupPath
            Write-Host "  Created backup: $backupPath" -ForegroundColor Cyan
        }
        
        # Write the updated content
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8
        Write-Host "  File updated successfully" -ForegroundColor Green
        $filesChanged++
    }
    elseif ($fileChanges -gt 0 -and $DryRun) {
        Write-Host "  Would update file (DRY RUN)" -ForegroundColor Magenta
    }
    else {
        Write-Host "  No changes needed" -ForegroundColor Gray
    }
    
    $totalChanges += $fileChanges
    Write-Host ""
}

Write-Host "=== Summary ===" -ForegroundColor Green
Write-Host "Files processed: $($helperFiles.Count)"
Write-Host "Files changed: $filesChanged"
Write-Host "Total renaming operations: $totalChanges"

if ($DryRun) {
    Write-Host ""
    Write-Host "This was a DRY RUN. No files were actually changed." -ForegroundColor Yellow
    Write-Host "Run without -DryRun parameter to apply changes." -ForegroundColor Yellow
}
else {
    Write-Host ""
    Write-Host "All changes have been applied!" -ForegroundColor Green
    Write-Host "Remember to:" -ForegroundColor Yellow
    Write-Host "1. Run a clean build to check for any issues" -ForegroundColor Yellow
    Write-Host "2. Run all tests to ensure functionality" -ForegroundColor Yellow
    Write-Host "3. Commit changes to version control" -ForegroundColor Yellow
}
