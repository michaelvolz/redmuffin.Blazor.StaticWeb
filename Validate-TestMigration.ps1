#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Validates test file migration against the migration mapping JSON.

.DESCRIPTION
    Compares actual test method names in files with expected method names from the migration mapping.
    Ensures all test methods are properly migrated to their target locations.

.EXAMPLE
    .\Validate-TestMigration.ps1
#>

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

# Load the migration mapping
$mappingPath = 'tasks\PRD-012-TestPartialClassReorganization-ToDo-MigrationMapping.json'
if (-not (Test-Path $mappingPath)) {
    Write-Error "Migration mapping file not found: $mappingPath"
    exit 1
}

$mapping = Get-Content $mappingPath | ConvertFrom-Json
Write-Host "Loaded migration mapping with $($mapping.Count) test classes" -ForegroundColor Green

$validationErrors = @()
$validationWarnings = @()
$totalFiles = 0
$validFiles = 0

foreach ($testClass in $mapping) {
    $className = $testClass.TestClass
    Write-Host "`nValidating test class: $className" -ForegroundColor Cyan
    
    # Group tests by target file
    $fileGroups = $testClass.Tests | Group-Object TargetPath
    
    foreach ($fileGroup in $fileGroups) {
        $targetPath = $fileGroup.Name
        $expectedMethods = $fileGroup.Group | ForEach-Object { $_.MethodName }
        $totalFiles++
        
        # Convert relative path to absolute
        $absolutePath = Join-Path $PWD $targetPath
        
        if (-not (Test-Path $absolutePath)) {
            $errorMsg = "[ERROR] File not found: $targetPath"
            $validationErrors += $errorMsg
            Write-Host "  [ERROR] File not found: $targetPath" -ForegroundColor Red
            continue
        }
        
        # Extract test method names from the file
        $fileContent = Get-Content $absolutePath -Raw
        $testMethodPattern = '\[Test\]\s*(?:\[Arguments\([^\]]*\)\]\s*)*(?:public\s+)?(?:async\s+)?(?:Task|void)\s+(\w+)'
        $actualMethods = [regex]::Matches($fileContent, $testMethodPattern) | ForEach-Object { $_.Groups[1].Value }
        
        Write-Host "  File: $targetPath" -ForegroundColor Yellow
        Write-Host "    Expected methods: $($expectedMethods.Count)" -ForegroundColor Gray
        Write-Host "    Actual methods: $($actualMethods.Count)" -ForegroundColor Gray
        
        # Check for missing methods
        $missingMethods = $expectedMethods | Where-Object { $_ -notin $actualMethods }
        if ($missingMethods) {
            foreach ($missing in $missingMethods) {
                $errorMsg = "[ERROR] Missing method in ${targetPath}: $missing"
                $validationErrors += $errorMsg
                Write-Host "    [ERROR] Missing: $missing" -ForegroundColor Red
            }
        }
        
        # Check for extra methods (methods in file but not in mapping)
        $extraMethods = $actualMethods | Where-Object { $_ -notin $expectedMethods }
        if ($extraMethods) {
            foreach ($extra in $extraMethods) {
                $warningMsg = "[WARNING] Extra method in ${targetPath}: $extra"
                $validationWarnings += $warningMsg
                Write-Host "    [WARNING] Extra: $extra" -ForegroundColor Yellow
            }
        }
        
        # Check if all expected methods are present
        if (-not $missingMethods) {
            $validFiles++
            Write-Host "    [SUCCESS] All expected methods present" -ForegroundColor Green
        }
    }
}

# Summary
Write-Host "`n" + "="*60 -ForegroundColor Magenta
Write-Host "VALIDATION SUMMARY" -ForegroundColor Magenta
Write-Host "="*60 -ForegroundColor Magenta

Write-Host "Total files validated: $totalFiles" -ForegroundColor Cyan
Write-Host "Files with all expected methods: $validFiles" -ForegroundColor Green
Write-Host "Files with missing methods: $($totalFiles - $validFiles)" -ForegroundColor Red
Write-Host "Total errors: $($validationErrors.Count)" -ForegroundColor Red
Write-Host "Total warnings: $($validationWarnings.Count)" -ForegroundColor Yellow

if ($validationErrors.Count -gt 0) {
    Write-Host "`n[ERROR] VALIDATION ERRORS:" -ForegroundColor Red
    foreach ($error in $validationErrors) {
        Write-Host "  $error" -ForegroundColor Red
    }
}

if ($validationWarnings.Count -gt 0) {
    Write-Host "`n[WARNING] VALIDATION WARNINGS:" -ForegroundColor Yellow
    foreach ($warning in $validationWarnings) {
        Write-Host "  $warning" -ForegroundColor Yellow
    }
}

if ($validationErrors.Count -eq 0) {
    Write-Host "`n[SUCCESS] All test files have the expected methods!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n[FAILURE] Validation failed with $($validationErrors.Count) errors" -ForegroundColor Red
    exit 1
}