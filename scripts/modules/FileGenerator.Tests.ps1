# FileGenerator.Tests.ps1
# Unit tests for FileGenerator module

# Import the module under test
$ModulePath = Join-Path $PSScriptRoot "FileGenerator.psm1"
Import-Module $ModulePath -Force

Describe "FileGenerator Tests" {
    
    BeforeAll {
        # Setup test directory
        $script:TestDir = Join-Path $TestDrive "FileGeneratorTests"
        New-Item -ItemType Directory -Path $script:TestDir -Force | Out-Null
    }
    
    Context "New-ChangelogFile Tests" {
        
        It "Should create changelog file successfully" {
            $testFile = Join-Path $script:TestDir "test-changelog.md"
            $testContent = "# Test Changelog`n`nThis is a test."
            
            $result = New-ChangelogFile -FilePath $testFile -Content $testContent
            
            $result | Should -Be $true
            Test-Path $testFile | Should -Be $true
            
            $fileContent = Get-Content $testFile -Raw
            $fileContent | Should -Match "# Test Changelog"
        }
        
        It "Should create directory if it doesn't exist" {
            $newDir = Join-Path $script:TestDir "newsubdir"
            $testFile = Join-Path $newDir "changelog.md"
            $testContent = "# Test"
            
            $result = New-ChangelogFile -FilePath $testFile -Content $testContent
            
            $result | Should -Be $true
            Test-Path $newDir | Should -Be $true
            Test-Path $testFile | Should -Be $true
        }
        
        It "Should handle UTF8 encoding correctly" {
            $testFile = Join-Path $script:TestDir "utf8-test.md"
            $testContent = "# Test with special chars: àáâãäå"
            
            $result = New-ChangelogFile -FilePath $testFile -Content $testContent -Encoding "UTF8"
            
            $result | Should -Be $true
            $fileContent = Get-Content $testFile -Raw -Encoding UTF8
            $fileContent | Should -Match "àáâãäå"
        }
        
        It "Should return false on write failure" {
            # Try to write to an invalid path
            $invalidPath = "Z:\NonExistent\Path\file.md"
            $testContent = "Test content"
            
            $result = New-ChangelogFile -FilePath $invalidPath -Content $testContent
            
            $result | Should -Be $false
        }
    }
    
    Context "Test-FileWritePermission Tests" {
        
        It "Should return true for writable directory" {
            $testFile = Join-Path $script:TestDir "permission-test.md"
            
            $result = Test-FileWritePermission -FilePath $testFile
            
            $result | Should -Be $true
        }
        
        It "Should return false for non-existent drive" {
            $invalidPath = "Z:\NonExistent\file.md"
            
            $result = Test-FileWritePermission -FilePath $invalidPath
            
            $result | Should -Be $false
        }
        
        It "Should handle current directory correctly" {
            $result = Test-FileWritePermission -FilePath "test.md"
            
            $result | Should -Be $true
        }
    }
    
    Context "Get-ChangelogHeader Tests" {
        
        It "Should create default header when no config" {
            $header = Get-ChangelogHeader -ConfigPath "non-existent.json"
            
            $header | Should -Match "# Changelog"
            $header | Should -Match "All notable changes to this project will be documented in this file\."
            $header | Should -Match "\*Generated on \d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\*"
        }
        
        BeforeAll {
            # Create test config file
            $testConfig = @{
                output = @{
                    title = "# Custom Changelog Title"
                    description = "Custom description for the changelog."
                }
            }
            $script:testConfigPath = Join-Path $script:TestDir "test-header-config.json"
            $testConfig | ConvertTo-Json -Depth 10 | Out-File $script:testConfigPath -Encoding UTF8
        }
        
        It "Should use custom config when available" {
            $header = Get-ChangelogHeader -ConfigPath $script:testConfigPath
            
            $header | Should -Match "# Custom Changelog Title"
            $header | Should -Match "Custom description for the changelog\."
        }
    }
    
    Context "Add-ChangelogMetadata Tests" {
        
        It "Should add metadata correctly" {
            $content = "# Changelog`n`nSome content"
            $result = Add-ChangelogMetadata -Content $content -CommitCount 50 -FilteredCount 10
            
            $result | Should -Match "---"
            $result | Should -Match "\*This changelog was automatically generated from 50 commits\.\*"
            $result | Should -Match "\*10 commits were filtered out"
        }
        
        It "Should handle zero filtered commits" {
            $content = "# Changelog"
            $result = Add-ChangelogMetadata -Content $content -CommitCount 25 -FilteredCount 0
            
            $result | Should -Match "\*This changelog was automatically generated from 25 commits\.\*"
            $result | Should -Not -Match "filtered out"
        }
    }
    
    Context "Backup-ExistingChangelog Tests" {
        
        BeforeAll {
            # Create a test changelog file
            $script:existingChangelog = Join-Path $script:TestDir "existing-changelog.md"
            "# Existing Changelog`n`nOld content" | Out-File $script:existingChangelog -Encoding UTF8
        }
        
        It "Should create backup of existing file" {
            $backupPath = Backup-ExistingChangelog -FilePath $script:existingChangelog
            
            $backupPath | Should -Not -BeNullOrEmpty
            Test-Path $backupPath | Should -Be $true
            
            $backupContent = Get-Content $backupPath -Raw
            $backupContent | Should -Match "# Existing Changelog"
            $backupContent | Should -Match "Old content"
        }
        
        It "Should return null for non-existent file" {
            $nonExistentFile = Join-Path $script:TestDir "does-not-exist.md"
            
            $backupPath = Backup-ExistingChangelog -FilePath $nonExistentFile
            
            $backupPath | Should -BeNullOrEmpty
        }
        
        It "Should create backup with timestamp" {
            $backupPath = Backup-ExistingChangelog -FilePath $script:existingChangelog
            
            $backupPath | Should -Match "_backup_\d{8}-\d{6}\.md$"
        }
    }
    
    Context "Write-ChangelogToFile Tests" {
        
        BeforeAll {
            # Create mock categorized commits
            $script:mockCategorizedCommits = @{
                "Features" = @(
                    [PSCustomObject]@{ Hash = "abc123"; Message = "Add user authentication"; Category = "Features" }
                )
                "Bug Fixes" = @(
                    [PSCustomObject]@{ Hash = "def456"; Message = "Fix login issue"; Category = "Bug Fixes" }
                )
            }
        }
        
        It "Should write changelog file successfully" {
            $outputFile = Join-Path $script:TestDir "generated-changelog.md"
            
            $result = Write-ChangelogToFile -FilePath $outputFile -CategorizedCommits $script:mockCategorizedCommits -CommitCount 10 -FilteredCount 3
            
            $result | Should -Be $true
            Test-Path $outputFile | Should -Be $true
            
            $content = Get-Content $outputFile -Raw
            $content | Should -Match "# Changelog"
            $content | Should -Match "### Features"
            $content | Should -Match "### Bug Fixes"
            $content | Should -Match "Add user authentication.*abc123"
            $content | Should -Match "Fix login issue.*def456"
        }
        
        It "Should create backup when requested" {
            $outputFile = Join-Path $script:TestDir "backup-test-changelog.md"
            
            # Create initial file
            "# Old Changelog" | Out-File $outputFile -Encoding UTF8
            
            # Write new changelog with backup
            $result = Write-ChangelogToFile -FilePath $outputFile -CategorizedCommits $script:mockCategorizedCommits -CreateBackup
            
            $result | Should -Be $true
            
            # Check that backup was created
            $backupFiles = Get-ChildItem $script:TestDir -Filter "*backup*changelog*.md"
            $backupFiles.Count | Should -BeGreaterThan 0
        }
        
        It "Should handle write permission errors gracefully" {
            $invalidPath = "Z:\NonExistent\changelog.md"
            
            $result = Write-ChangelogToFile -FilePath $invalidPath -CategorizedCommits $script:mockCategorizedCommits
            
            $result | Should -Be $false
        }
    }
    
    Context "Test-ChangelogFileIntegrity Tests" {
        
        BeforeAll {
            # Create a valid changelog file
            $script:validChangelog = Join-Path $script:TestDir "valid-changelog.md"
            $validContent = @"
# Changelog

All notable changes to this project will be documented in this file.

### Features

- [Add new feature] (abc123)

### Bug Fixes

- [Fix critical bug] (def456)
"@
            $validContent | Out-File $script:validChangelog -Encoding UTF8
            
            # Create an invalid changelog file
            $script:invalidChangelog = Join-Path $script:TestDir "invalid-changelog.md"
            "Invalid content without proper structure" | Out-File $script:invalidChangelog -Encoding UTF8
        }
        
        It "Should validate correct changelog file" {
            $result = Test-ChangelogFileIntegrity -FilePath $script:validChangelog
            
            $result | Should -Be $true
        }
        
        It "Should detect invalid changelog structure" {
            $result = Test-ChangelogFileIntegrity -FilePath $script:invalidChangelog
            
            $result | Should -Be $false
        }
        
        It "Should return false for non-existent file" {
            $nonExistentFile = Join-Path $script:TestDir "does-not-exist.md"
            
            $result = Test-ChangelogFileIntegrity -FilePath $nonExistentFile
            
            $result | Should -Be $false
        }
        
        It "Should detect empty file" {
            $emptyFile = Join-Path $script:TestDir "empty-changelog.md"
            "" | Out-File $emptyFile -Encoding UTF8
            
            $result = Test-ChangelogFileIntegrity -FilePath $emptyFile
            
            $result | Should -Be $false
        }
    }
    
    AfterAll {
        # Clean up test files
        if (Test-Path $script:TestDir) {
            Remove-Item $script:TestDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}
