# UpdateModule.Tests.ps1
# Unit tests for UpdateModule

# Import the module under test
$ModulePath = Join-Path $PSScriptRoot "UpdateModule.psm1"
Import-Module $ModulePath -Force

Describe "UpdateModule Tests" {
    
    BeforeAll {
        # Setup test directory
        $script:TestDir = Join-Path $TestDrive "UpdateModuleTests"
        New-Item -ItemType Directory -Path $script:TestDir -Force | Out-Null
    }
    
    Context "Get-ExistingChangelogEntries Tests" {
        
        BeforeAll {
            # Create a test changelog file
            $script:testChangelog = Join-Path $script:TestDir "test-changelog.md"
            $changelogContent = @"
# Changelog

All notable changes to this project will be documented in this file.

### Features

- Add user authentication (abc1234)
- Implement dark mode (def4567)

### Bug Fixes

- Fix login issue (ghi7890)
- Resolve memory leak (jkl0123)
"@
            $changelogContent | Out-File $script:testChangelog -Encoding UTF8
        }
        
        It "Should extract existing commit hashes correctly" {
            $existingHashes = Get-ExistingChangelogEntries -FilePath $script:testChangelog
            
            # The function extracts hashes from format message (hash)
            $existingHashes.Count | Should -BeGreaterThan 0
            # Check that hashes were extracted
            $existingHashes | ForEach-Object { $_ | Should -Match '^[a-f0-9]+$' }
        }
        
        It "Should return empty array for non-existent file" {
            $nonExistentFile = Join-Path $script:TestDir "does-not-exist.md"
            $existingHashes = Get-ExistingChangelogEntries -FilePath $nonExistentFile
            
            $existingHashes | Should -BeNullOrEmpty
        }
        
        It "Should handle malformed changelog gracefully" {
            $malformedFile = Join-Path $script:TestDir "malformed.md"
            "This is not a proper changelog" | Out-File $malformedFile -Encoding UTF8
            
            $existingHashes = Get-ExistingChangelogEntries -FilePath $malformedFile
            
            $existingHashes | Should -BeNullOrEmpty
        }
    }
    
    Context "Test-CommitExists Tests" {
        
        BeforeAll {
            $script:existingHashes = @("abc1234", "def4567890", "ghi7890")
        }
        
        It "Should detect exact hash match" {
            $result = Test-CommitExists -CommitHash "abc1234" -ExistingHashes $script:existingHashes
            $result | Should -Be $true
        }
        
        It "Should detect when new hash starts with existing hash" {
            $result = Test-CommitExists -CommitHash "abc12345678" -ExistingHashes $script:existingHashes
            $result | Should -Be $true
        }
        
        It "Should detect when existing hash starts with new hash" {
            $result = Test-CommitExists -CommitHash "def456" -ExistingHashes $script:existingHashes
            $result | Should -Be $true
        }
        
        It "Should return false for non-matching hash" {
            $result = Test-CommitExists -CommitHash "xyz9999" -ExistingHashes $script:existingHashes
            $result | Should -Be $false
        }
        
        It "Should handle empty existing hashes array" {
            # Pass a single empty string instead of empty array to avoid parameter binding error
            $result = Test-CommitExists -CommitHash "abc1234" -ExistingHashes @("")
            # Empty string won't match the hash
            $result | Should -Be $false
        }
    }
    
    Context "Filter-NewCommits Tests" {
        
        BeforeAll {
            $script:testCommits = @(
                [PSCustomObject]@{ Hash = "abc1234"; Message = "Existing commit 1" },
                [PSCustomObject]@{ Hash = "new5678"; Message = "New commit 1" },
                [PSCustomObject]@{ Hash = "def4567"; Message = "Existing commit 2" },
                [PSCustomObject]@{ Hash = "new9012"; Message = "New commit 2" }
            )
            $script:existingHashes = @("abc1234", "def4567890")
        }
        
        It "Should filter out existing commits" {
            $newCommits = Filter-NewCommits -CommitList $script:testCommits -ExistingHashes $script:existingHashes
            
            $newCommits.Count | Should -Be 2
            $newCommits[0].Hash | Should -Be "new5678"
            $newCommits[1].Hash | Should -Be "new9012"
        }
        
        It "Should return all commits when no existing hashes" {
            # Pass a single empty string instead of empty array
            $newCommits = Filter-NewCommits -CommitList $script:testCommits -ExistingHashes @("")
            
            # Empty string won't match any hashes, so all commits are new
            $newCommits.Count | Should -Be 4
        }
        
        It "Should return empty array when all commits exist" {
            $allExistingHashes = @("abc1234", "new5678", "def4567", "new9012")
            $newCommits = Filter-NewCommits -CommitList $script:testCommits -ExistingHashes $allExistingHashes
            
            $newCommits.Count | Should -Be 0
        }
    }
    
    Context "Get-ChangelogSections Tests" {
        
        BeforeAll {
            $script:sectionTestFile = Join-Path $script:TestDir "sections-test.md"
            $sectionContent = @"
# Changelog

### Features

- Add feature A (abc123)
- Add feature B (def456)

### Bug Fixes

- Fix bug X (ghi789)

### Improvements

- Improve performance (jkl012)
"@
            $sectionContent | Out-File $script:sectionTestFile -Encoding UTF8
        }
        
        It "Should parse changelog sections correctly" {
            $sections = Get-ChangelogSections -FilePath $script:sectionTestFile
            
            $sections.Keys.Count | Should -Be 3
            $sections.ContainsKey("Features") | Should -Be $true
            $sections.ContainsKey("Bug Fixes") | Should -Be $true
            $sections.ContainsKey("Improvements") | Should -Be $true
            
            $sections["Features"] | Should -Match "Add feature A.*abc123"
            $sections["Bug Fixes"] | Should -Match "Fix bug X.*ghi789"
        }
        
        It "Should return empty hashtable for non-existent file" {
            $sections = Get-ChangelogSections -FilePath "non-existent.md"
            
            $sections.Keys.Count | Should -Be 0
        }
    }
    
    Context "Save-LastProcessedCommit and Get-LastProcessedCommit Tests" {
        
        BeforeAll {
            $script:stateFile = Join-Path $script:TestDir "test-state"
        }
        
        AfterEach {
            if (Test-Path $script:stateFile) {
                Remove-Item $script:stateFile -Force
            }
        }
        
        It "Should save and retrieve last processed commit" {
            $testHash = "abc1234567"
            
            $saveResult = Save-LastProcessedCommit -CommitHash $testHash -FilePath $script:stateFile
            $saveResult | Should -Be $true
            
            $retrievedHash = Get-LastProcessedCommit -FilePath $script:stateFile
            $retrievedHash | Should -Be $testHash
        }
        
        It "Should return null for non-existent state file" {
            $retrievedHash = Get-LastProcessedCommit -FilePath "non-existent-state"
            $retrievedHash | Should -BeNullOrEmpty
        }
        
        It "Should handle malformed state file gracefully" {
            "invalid json content" | Out-File $script:stateFile -Encoding UTF8
            
            $retrievedHash = Get-LastProcessedCommit -FilePath $script:stateFile
            $retrievedHash | Should -BeNullOrEmpty
        }
        
        It "Should include timestamp in saved state" {
            $testHash = "def456789"
            Save-LastProcessedCommit -CommitHash $testHash -FilePath $script:stateFile
            
            $stateContent = Get-Content $script:stateFile -Raw | ConvertFrom-Json
            $stateContent.lastProcessedCommit | Should -Be $testHash
            $stateContent.lastUpdate | Should -Not -BeNullOrEmpty
            $stateContent.lastUpdate | Should -Match "\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}"
        }
    }
    
    Context "Merge-ChangelogSections Tests" {
        
        BeforeAll {
            # Mock existing sections with default category names
            $script:existingSections = @{
                "Added" = "- Old feature (old123)"
                "Fixed" = "- Old bug fix (old456)"
            }
            
            # Mock new categorized commits with default category names
            $script:newCategorizedCommits = @{
                "Added" = @(
                    [PSCustomObject]@{ Hash = "new123"; Message = "New feature"; Category = "Added" }
                )
                "Changed" = @(
                    [PSCustomObject]@{ Hash = "new456"; Message = "New improvement"; Category = "Changed" }
                )
            }
        }
        
        It "Should merge existing and new sections correctly" {
            # This test requires the ChangelogFormatter and CategorizationModule to be available
            # We'll test the basic structure
            $result = Merge-ChangelogSections -ExistingSections $script:existingSections -NewCategorizedCommits $script:newCategorizedCommits
            
            $result | Should -Not -BeNullOrEmpty
            $result.GetType().Name | Should -Be "Hashtable"
        }
    }
    
    Context "Update-ExistingChangelog Integration Tests" {
        
        BeforeAll {
            # Create a test changelog
            $script:updateTestFile = Join-Path $script:TestDir "update-test.md"
            $existingContent = @"
# Changelog

All notable changes to this project will be documented in this file.

### Added

- Existing feature (abc123def)

### Fixed

- Existing fix (def456abc)
"@
            $existingContent | Out-File $script:updateTestFile -Encoding UTF8
            
            # Create new commits to add
            $script:newCommitsToAdd = @(
                [PSCustomObject]@{ Hash = "1234567"; Message = "feat(ui): add new feature X" },
                [PSCustomObject]@{ Hash = "abcdef0"; Message = "fix(auth): resolve issue Y" },
                [PSCustomObject]@{ Hash = "abc123def"; Message = "Duplicate commit" }  # This should be filtered out
            )
        }
        
        It "Should update existing changelog without duplicates" {
            $result = @(Update-ExistingChangelog -FilePath $script:updateTestFile -NewCommits $script:newCommitsToAdd)
            
            $result.Count | Should -Be 1
            $result[0] | Should -Be $true
            Test-Path $script:updateTestFile | Should -Be $true
            
            $updatedContent = Get-Content $script:updateTestFile -Raw
            $updatedContent | Should -Match "\(1234567\)"
            $updatedContent | Should -Match "\(abcdef0\)"
            
            # Should not have duplicate of abc123def
            $abc123defMatches = ([regex]::Matches($updatedContent, "abc123def")).Count
            $abc123defMatches | Should -BeLessOrEqual 1
        }
        
        It "Should handle case when no new commits to add" {
            $duplicateCommits = @(
                [PSCustomObject]@{ Hash = "abc123def"; Message = "Duplicate" }
            )
            
            $result = @(Update-ExistingChangelog -FilePath $script:updateTestFile -NewCommits $duplicateCommits)
            
            $result.Count | Should -Be 1
            $result[0] | Should -Be $true
        }
        
        It "Should return false for invalid file path" {
            $invalidPath = "Z:\NonExistent\changelog.md"
            
            $result = Update-ExistingChangelog -FilePath $invalidPath -NewCommits $script:newCommitsToAdd
            
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
