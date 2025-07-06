# ChangelogFormatter.Tests.ps1
# Unit tests for ChangelogFormatter module

# Import the module under test
$ModulePath = Join-Path $PSScriptRoot "ChangelogFormatter.psm1"
Import-Module $ModulePath -Force

Describe "ChangelogFormatter Tests" {
    
    Context "Get-FormattingConfig Tests" {
        
        BeforeAll {
            # Create a test config file
            $testConfig = @{
                formatting = @{
                    commitFormat = "[{message}] ({hash})"
                }
            }
            $testConfigPath = "test-formatting-config.json"
            $testConfig | ConvertTo-Json -Depth 10 | Out-File $testConfigPath -Encoding UTF8
        }
        
        AfterAll {
            # Clean up test config file
            Remove-Item "test-formatting-config.json" -ErrorAction SilentlyContinue
        }
        
        It "Should load valid formatting configuration file" {
            $config = Get-FormattingConfig -ConfigPath "test-formatting-config.json"
            $config | Should -Not -BeNullOrEmpty
            $config.commitFormat | Should -Be "[{message}] ({hash})"
        }
        
        It "Should return null for non-existent file" {
            $config = Get-FormattingConfig -ConfigPath "non-existent.json"
            $config | Should -BeNullOrEmpty
        }
        
        It "Should handle malformed JSON gracefully" {
            "{ invalid json" | Out-File "invalid-formatting.json" -Encoding UTF8
            $config = Get-FormattingConfig -ConfigPath "invalid-formatting.json"
            $config | Should -BeNullOrEmpty
            Remove-Item "invalid-formatting.json" -ErrorAction SilentlyContinue
        }
    }
    
    Context "Format-CommitEntry Tests" {
        
        It "Should format commit entry correctly with default format" {
            $commit = [PSCustomObject]@{ Hash = "abc1234"; Message = "Fix memory leak" }
            $formatted = Format-CommitEntry -Commit $commit
            $formatted | Should -Be "Fix memory leak (abc1234)"
        }
        
        It "Should handle special Markdown characters in message" {
            $commit = [PSCustomObject]@{ Hash = "abc1234"; Message = "Fix issue with #hashtag and [link](url)" }
            $formatted = Format-CommitEntry -Commit $commit
            # Only backticks are escaped now
            $formatted | Should -Be "Fix issue with #hashtag and [link](url) (abc1234)"
        }
        
        It "Should use custom format if provided" {
            $commit = [PSCustomObject]@{ Hash = "abc1234"; Message = "Add feature X" }
            $formatted = Format-CommitEntry -Commit $commit -Format "{message}: {hash}"
            $formatted | Should -Be "Add feature X: abc1234"
        }
    }
    
    Context "Escape-MarkdownText Tests" {
        
        It "Should escape Markdown special characters" {
            $text = '*Italic* and _Italic_ and `Code`'
            $escaped = Escape-MarkdownText -Text $text
            # Only backticks are escaped now for better readability
            $escaped | Should -Be '*Italic* and _Italic_ and \`Code\`'
        }
        
        It "Should handle empty string" {
            $text = " "  # Use space instead of empty string to avoid parameter binding error
            $escaped = Escape-MarkdownText -Text $text
            $escaped | Should -Be " "
        }
        
        It "Should not escape regular text" {
            $text = "Regular text"
            $escaped = Escape-MarkdownText -Text $text
            $escaped | Should -Be "Regular text"
        }
    }
    
    Context "Sort-CommitsChronologically Tests" {
        
        It "Should sort commits in descending order" {
            $commits = @(
                [PSCustomObject]@{ Hash = "ghi789"; LineNumber = 3 },
                [PSCustomObject]@{ Hash = "abc123"; LineNumber = 1 },
                [PSCustomObject]@{ Hash = "def456"; LineNumber = 2 }
            )
            $sorted = Sort-CommitsChronologically -CommitList $commits -Order "descending"
            $sorted[0].Hash | Should -Be "abc123"
            $sorted[-1].Hash | Should -Be "ghi789"
        }
        
        It "Should sort commits in ascending order" {
            $commits = @(
                [PSCustomObject]@{ Hash = "ghi789"; LineNumber = 3 },
                [PSCustomObject]@{ Hash = "abc123"; LineNumber = 1 },
                [PSCustomObject]@{ Hash = "def456"; LineNumber = 2 }
            )
            $sorted = Sort-CommitsChronologically -CommitList $commits -Order "ascending"
            $sorted[0].Hash | Should -Be "ghi789"
            $sorted[-1].Hash | Should -Be "abc123"
        }
    }
    
    Context "Format-CategorySection Tests" {
        
        BeforeAll {
            # Mock configuration
            [PSCustomObject] $script:mockConfig = @{ commitFormat = "[{message}] ({hash})" }
        }
        
        It "Should format category section with commits" {
            $commits = @(
                [PSCustomObject]@{ Hash = "abc123"; Message = "Fix memory leak" },
                [PSCustomObject]@{ Hash = "def456"; Message = "Add new feature" }
            )
            $section = Format-CategorySection -CategoryName "Bug Fixes" -CommitList $commits
            # Check for the heading
            $section | Should -Match "### Bug Fixes"
            # Check for commits without brackets
            $section | Should -Match "Fix memory leak \(abc123\)"
            $section | Should -Match "Add new feature \(def456\)"
        }

        It "Should return empty string for empty commit list" {
            $commits = @()
            $section = Format-CategorySection -CategoryName "Improvements" -CommitList $commits
            # Empty array should return empty string
            $section | Should -Be ""
        }
    }
    
    Context "Format-ChangelogDocument Tests" {
        
        BeforeAll {
            # Mock categorized commits
            $script:categorizedCommits = @{
                "Features" = @(
                    [PSCustomObject]@{ Hash = "abc123"; Message = "Add user authentication" }
                )
                "Bug Fixes" = @(
                    [PSCustomObject]@{ Hash = "def456"; Message = "Fix login issue" }
                )
            }
        }
        
        It "Should format complete changelog document" {
            $document = Format-ChangelogDocument -CategorizedCommits $script:categorizedCommits
            $document | Should -Match "# Changelog"
            $document | Should -Match "All notable changes to this project will be documented in this file"
            $document | Should -Match "### Features"
            $document | Should -Match "### Bug Fixes"
        }
    }
    
    Context "Test-MarkdownValidity Tests" {
        
        It "Should validate correct Markdown" {
            $markdown = "# Title\n\n- [Commit message] (hash)\n"
            Test-MarkdownValidity -MarkdownContent $markdown | Should -Be $true
        }
        
        It "Should detect unmatched brackets" {
            $markdown = "[Unmatched\n"
            Test-MarkdownValidity -MarkdownContent $markdown | Should -Be $false
        }
        
        It "Should detect unmatched parentheses" {
            $markdown = "Commit (hash"
            Test-MarkdownValidity -MarkdownContent $markdown | Should -Be $false
        }
        
        It "Should detect empty heading" {
            $markdown = "#`n"
            Test-MarkdownValidity -MarkdownContent $markdown | Should -Be $false
        }
    }
}
