# Generate Migration Mapping JSON for PRD-012 Test Partial Class Reorganization
# This script scans all test files and creates a migration mapping based on categorization rules

param(
    [string]$OutputPath = "tasks\PRD-012-TestPartialClassReorganization-ToDo-MigrationMapping.json"
)

Write-Host "Starting migration mapping generation..." -ForegroundColor Green

# Get current location for relative path calculation
$rootPath = Get-Location
Write-Host "Root path: $rootPath"

# Function to categorize test method based on name and content
function Get-TestCategory {
    param(
        [string]$MethodName,
        [string]$FileContent,
        [int]$MethodStartIndex
    )

    # Extract method content (approximate - get next 20 lines after method declaration)
    $lines = $FileContent -split "`n"
    $methodLineIndex = ($FileContent.Substring(0, $MethodStartIndex) -split "`n").Count - 1
    $methodContent = ($lines[$methodLineIndex..($methodLineIndex + 20)] -join "`n").ToLower()
    $methodNameLower = $MethodName.ToLower()

    # 1. EdgeCases - Check FIRST
    $edgeCaseKeywords = @('error', 'exception', 'fail', 'invalid', 'null', 'empty', 'timeout', 'malformed', 'corrupt')
    $edgeCaseAsserts = @('assert.throws', 'throwsasync', 'setexception', 'httprequestexception', 'invalidoperationexception')
    $edgeCaseSetup = @('createfailing', 'withfailing', 'setupfailure', 'setupexception', 'setupthrows')

    foreach ($keyword in $edgeCaseKeywords) {
        if ($methodNameLower.Contains($keyword)) {
            return "EdgeCases"
        }
    }

    foreach ($assert in $edgeCaseAsserts) {
        if ($methodContent.Contains($assert)) {
            return "EdgeCases"
        }
    }

    foreach ($setup in $edgeCaseSetup) {
        if ($methodContent.Contains($setup)) {
            return "EdgeCases"
        }
    }

    # 2. Infrastructure - Check SECOND
    $infraKeywords = @('lifecycle', 'logging', 'cache', 'auth', 'di', 'jsinterop', 'serializ', 'disposal', 'memory', 'event')
    $infraMethods = @('oninitialized', 'onparametersset', 'onafterrender', 'statehaschanged', 'dispose')
    $infraComponents = @('cascadingvalue', 'authenticationstate', 'jsinterop', 'localstorage')

    foreach ($keyword in $infraKeywords) {
        if ($methodNameLower.Contains($keyword)) {
            return "Infrastructure"
        }
    }

    foreach ($method in $infraMethods) {
        if ($methodContent.Contains($method)) {
            return "Infrastructure"
        }
    }

    foreach ($component in $infraComponents) {
        if ($methodContent.Contains($component)) {
            return "Infrastructure"
        }
    }

    # 3. Behavior - Check THIRD
    $behaviorKeywords = @('click', 'submit', 'change', 'interaction', 'workflow', 'concurrent', 'multiple', 'rapid')
    $behaviorMethods = @('clickasync', 'changeasync', 'triggereventasync', 'mouseeventargs', 'changeeventargs')

    foreach ($keyword in $behaviorKeywords) {
        if ($methodNameLower.Contains($keyword)) {
            return "Behavior"
        }
    }

    foreach ($method in $behaviorMethods) {
        if ($methodContent.Contains($method)) {
            return "Behavior"
        }
    }

    # 4. Default to Main (basic functionality)
    return "Main"
}

# Function to get relative path
function Get-RelativePath {
    param(
        [string]$FullPath,
        [string]$BasePath
    )

    $relativePath = [System.IO.Path]::GetRelativePath($BasePath, $FullPath)
    return $relativePath.Replace('\', '/')
}

# Collect all test methods with their categorization
$testClasses = @{}

Get-ChildItem -Path "tests" -Filter "*.cs" -Recurse |
    Where-Object {
        $_.Name -notlike "*.Helpers.cs" -and
        $_.FullName -notmatch "obj\\" -and
        $_.FullName -notmatch "\.outdated$"
    } |
    ForEach-Object {
        $file = $_
        $content = Get-Content $file.FullName -Raw

        Write-Host "Processing: $($file.Name)" -ForegroundColor Yellow

        # Extract class name from file name (remove .cs extension)
        $className = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)

        # Handle partial class files - extract base class name
        $baseClassName = $className
        $currentCategory = "Main"
        if ($className -match '^(.+)\.(EdgeCases|Infrastructure|Behavior)$') {
            $baseClassName = $matches[1]
            $currentCategory = $matches[2]
            Write-Host "  Processing partial class: $className (base: $baseClassName, category: $currentCategory)" -ForegroundColor Magenta
        }

        # Extract namespace
        $namespace = ""
        if ($content -match "namespace\s+([\w\.]+)") {
            $namespace = $matches[1]
        }

        # Find all [Test] methods with their positions
        $testMatches = [regex]::Matches($content, '\[Test\]\s*\r?\n\s*public\s+(?:async\s+Task|void)\s+(\w+)\s*\(')

        if ($testMatches.Count -eq 0) {
            Write-Host "  No [Test] methods found" -ForegroundColor Gray
            return
        }

        # Initialize test class entry if not exists (use base class name)
        if (-not $testClasses.ContainsKey($baseClassName)) {
            # For main class file, use its path as base
            $baseSourceFile = if ($currentCategory -eq "Main") {
                Get-RelativePath -FullPath $file.FullName -BasePath $rootPath
            } else {
                # For partial class files, construct the main class file path
                $mainFilePath = $file.FullName -replace "\.$currentCategory\.cs$", ".cs"
                Get-RelativePath -FullPath $mainFilePath -BasePath $rootPath
            }

            $testClasses[$baseClassName] = @{
                Namespace = $namespace
                SourceFile = $baseSourceFile
                Tests = @()
            }
        }

        foreach ($match in $testMatches) {
            $methodName = $match.Groups[1].Value

            # For existing partial class files, use their current category
            # For main class files, determine category based on rules
            $category = if ($currentCategory -ne "Main") {
                $currentCategory
            } else {
                Get-TestCategory -MethodName $methodName -FileContent $content -MethodStartIndex $match.Index
            }

            # Determine target path based on category
            $targetPath = $testClasses[$baseClassName].SourceFile
            if ($category -ne "Main") {
                $targetPath = $targetPath -replace '\.cs$', ".$category.cs"
            }

            # Source path is the current file being processed
            $sourcePath = Get-RelativePath -FullPath $file.FullName -BasePath $rootPath

            $testClasses[$baseClassName].Tests += @{
                MethodName = $methodName
                Category = $category
                SourcePath = $sourcePath
                TargetPath = $targetPath
            }

            Write-Host "    $methodName -> $category" -ForegroundColor Cyan
        }
    }

# Convert to the required JSON format
$migrationMapping = @()
foreach ($className in $testClasses.Keys) {
    $classData = $testClasses[$className]

    $migrationMapping += @{
        TestClass = $className
        Tests = $classData.Tests
    }
}

# Sort by TestClass name for consistency
$migrationMapping = $migrationMapping | Sort-Object TestClass

# Output statistics
$totalTests = ($migrationMapping | ForEach-Object { $_.Tests.Count } | Measure-Object -Sum).Sum
$edgeCasesCount = ($migrationMapping | ForEach-Object { ($_.Tests | Where-Object { $_.Category -eq "EdgeCases" }).Count } | Measure-Object -Sum).Sum
$infraCount = ($migrationMapping | ForEach-Object { ($_.Tests | Where-Object { $_.Category -eq "Infrastructure" }).Count } | Measure-Object -Sum).Sum
$behaviorCount = ($migrationMapping | ForEach-Object { ($_.Tests | Where-Object { $_.Category -eq "Behavior" }).Count } | Measure-Object -Sum).Sum
$mainCount = ($migrationMapping | ForEach-Object { ($_.Tests | Where-Object { $_.Category -eq "Main" }).Count } | Measure-Object -Sum).Sum

Write-Host "`nMigration Mapping Statistics:" -ForegroundColor Green
Write-Host "  Total Test Classes: $($migrationMapping.Count)"
Write-Host "  Total Test Methods: $totalTests"
Write-Host "  EdgeCases: $edgeCasesCount"
Write-Host "  Infrastructure: $infraCount"
Write-Host "  Behavior: $behaviorCount"
Write-Host "  Main: $mainCount"

# Save to JSON file
$json = $migrationMapping | ConvertTo-Json -Depth 4
$json | Set-Content -Path $OutputPath -Encoding UTF8

Write-Host "`nMigration mapping saved to: $OutputPath" -ForegroundColor Green
Write-Host "File size: $((Get-Item $OutputPath).Length) bytes"