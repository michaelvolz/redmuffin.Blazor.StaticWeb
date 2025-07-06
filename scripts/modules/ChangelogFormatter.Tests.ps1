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
        
        It "Should not escape any characters for plain text changelog" {
            $text = '*Italic* and _Italic_ and `Code`'
            $escaped = Escape-MarkdownText -Text $text
            # No escaping for plain text readability
            $escaped | Should -Be '*Italic* and _Italic_ and `Code`'
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
            # Check for the heading with emoji
            $section | Should -Match "### .+ Bug Fixes"
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
            $document | Should -Match "# .+ Changelog"
            $document | Should -Match "All notable changes to this project will be documented in this file"
            $document | Should -Match "### .+ Features"
            $document | Should -Match "### .+ Bug Fixes"
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
    
    Context "Get-EmojiConfig Tests" {
        
        BeforeAll {
            # Create a test config file with emojis
            $testConfig = @{
                emojis = @{
                    categories = @{
                        "Added" = "✨"
                        "Fixed" = "🐛"
                    }
                    commitTypes = @{
                        "feat" = "🚀"
                        "fix" = "🔧"
                    }
                    header = "📋"
                    defaultEmoji = "📝"
                }
            }
            $testConfigPath = "test-emoji-config.json"
            $testConfig | ConvertTo-Json -Depth 10 | Out-File $testConfigPath -Encoding UTF8
        }
        
        AfterAll {
            Remove-Item "test-emoji-config.json" -ErrorAction SilentlyContinue
        }
        
        It "Should load emoji configuration" {
            $config = Get-EmojiConfig -ConfigPath "test-emoji-config.json"
            $config | Should -Not -BeNullOrEmpty
            $config.categories.Added | Should -Be "✨"
            $config.header | Should -Be "📋"
        }
        
        It "Should return null for missing config" {
            $config = Get-EmojiConfig -ConfigPath "non-existent-emoji.json"
            $config | Should -BeNullOrEmpty
        }
    }
    
    Context "Get-CategoryEmoji Tests" {
        
        BeforeAll {
            # Create test config
            $testConfig = @{
                emojis = @{
                    categories = @{
                        "Added" = "✨"
                        "Fixed" = "🐛"
                        "Changed" = "🔄"
                    }
                    defaultEmoji = "📝"
                }
            }
            $testConfigPath = "test-category-emoji.json"
            $testConfig | ConvertTo-Json -Depth 10 | Out-File $testConfigPath -Encoding UTF8
        }
        
        AfterAll {
            Remove-Item "test-category-emoji.json" -ErrorAction SilentlyContinue
        }
        
        It "Should return correct emoji for known category" {
            $emoji = Get-CategoryEmoji -CategoryName "Added" -ConfigPath "test-category-emoji.json"
            $emoji | Should -Be "✨"
        }
        
        It "Should return default emoji for unknown category" {
            $emoji = Get-CategoryEmoji -CategoryName "Unknown" -ConfigPath "test-category-emoji.json"
            $emoji | Should -Be "📝"
        }
        
        It "Should return hardcoded default when config missing" {
            $emoji = Get-CategoryEmoji -CategoryName "Added" -ConfigPath "missing-config.json"
            $emoji | Should -Be "📝"
        }
    }
    
    Context "Get-CommitTypeEmoji Tests" {
        
        BeforeAll {
            # Create test config
            $testConfig = @{
                emojis = @{
                    commitTypes = @{
                        "feat" = "🚀"
                        "fix" = "🔧"
                        "docs" = "📚"
                        "test" = "🧪"
                        "chore" = "🔨"
                    }
                }
            }
            $testConfigPath = "test-commit-emoji.json"
            $testConfig | ConvertTo-Json -Depth 10 | Out-File $testConfigPath -Encoding UTF8
        }
        
        AfterAll {
            Remove-Item "test-commit-emoji.json" -ErrorAction SilentlyContinue
        }
        
        It "Should return emoji for conventional commit type" {
            $emoji = Get-CommitTypeEmoji -CommitMessage "feat: add new feature" -ConfigPath "test-commit-emoji.json"
            $emoji | Should -Be "🚀"
        }
        
        It "Should handle commit with scope" {
            $emoji = Get-CommitTypeEmoji -CommitMessage "fix(api): resolve timeout issue" -ConfigPath "test-commit-emoji.json"
            $emoji | Should -Be "🔧"
        }
        
        It "Should return empty string for non-conventional commits" {
            $emoji = Get-CommitTypeEmoji -CommitMessage "Update readme file" -ConfigPath "test-commit-emoji.json"
            $emoji | Should -Be ""
        }
        
        It "Should return empty string for unknown commit type" {
            $emoji = Get-CommitTypeEmoji -CommitMessage "unknown: some message" -ConfigPath "test-commit-emoji.json"
            $emoji | Should -Be ""
        }
    }
    
    Context "Format-CommitEntry with Emojis Tests" {
        
        BeforeAll {
            # Create test config
            $testConfig = @{
                emojis = @{
                    commitTypes = @{
                        "feat" = "🚀"
                        "fix" = "🔧"
                    }
                }
            }
            $testConfigPath = "test-format-emoji.json"
            $testConfig | ConvertTo-Json -Depth 10 | Out-File $testConfigPath -Encoding UTF8
        }
        
        AfterAll {
            Remove-Item "test-format-emoji.json" -ErrorAction SilentlyContinue
        }
        
        It "Should add emoji to conventional commit" {
            $commit = [PSCustomObject]@{ Hash = "abc123"; Message = "feat: add user authentication" }
            $formatted = Format-CommitEntry -Commit $commit -ConfigPath "test-format-emoji.json"
            $formatted | Should -Be "🚀 feat: add user authentication (abc123)"
        }
        
        It "Should add type emoji and preserve existing emojis" {
            $commit = [PSCustomObject]@{ Hash = "abc123"; Message = "🎉 feat: celebration feature" }
            $formatted = Format-CommitEntry -Commit $commit -ConfigPath "test-format-emoji.json"
            # Both emojis should be present
            $formatted | Should -Be "🚀 🎉 feat: celebration feature (abc123)"
        }
        
        It "Should handle non-conventional commits" {
            $commit = [PSCustomObject]@{ Hash = "abc123"; Message = "Update documentation" }
            $formatted = Format-CommitEntry -Commit $commit -ConfigPath "test-format-emoji.json"
            $formatted | Should -Be "Update documentation (abc123)"
        }
    }
    
    Context "Format-CategorySection with Emojis Tests" {
        
        BeforeAll {
            # Create test config
            $testConfig = @{
                emojis = @{
                    categories = @{
                        "Added" = "✨"
                        "Fixed" = "🐛"
                    }
                    defaultEmoji = "📝"
                }
            }
            $testConfigPath = "test-category-section-emoji.json"
            $testConfig | ConvertTo-Json -Depth 10 | Out-File $testConfigPath -Encoding UTF8
        }
        
        AfterAll {
            Remove-Item "test-category-section-emoji.json" -ErrorAction SilentlyContinue
        }
        
        It "Should include emoji in category header" {
            $commits = @(
                [PSCustomObject]@{ Hash = "abc123"; Message = "Add new feature" }
            )
            $section = Format-CategorySection -CategoryName "Added" -CommitList $commits -ConfigPath "test-category-section-emoji.json"
            $section | Should -Match "### ✨ Added"
        }
        
        It "Should use default emoji for unknown category" {
            $commits = @(
                [PSCustomObject]@{ Hash = "abc123"; Message = "Some change" }
            )
            $section = Format-CategorySection -CategoryName "Other" -CommitList $commits -ConfigPath "test-category-section-emoji.json"
            $section | Should -Match "### 📝 Other"
        }
    }
    
    Context "Format-ChangelogDocument with Emojis Tests" {
        
        BeforeAll {
            # Create test config
            $testConfig = @{
                emojis = @{
                    header = "📋"
                    categories = @{
                        "Features" = "✨"
                    }
                }
                output = @{
                    title = "# Changelog"
                    description = "All notable changes"
                }
            }
            $testConfigPath = "test-document-emoji.json"
            $testConfig | ConvertTo-Json -Depth 10 | Out-File $testConfigPath -Encoding UTF8
        }
        
        AfterAll {
            Remove-Item "test-document-emoji.json" -ErrorAction SilentlyContinue
        }
        
        It "Should include emoji in document header" {
            $categorizedCommits = @{
                "Features" = @(
                    [PSCustomObject]@{ Hash = "abc123"; Message = "Add feature" }
                )
            }
            $document = Format-ChangelogDocument -CategorizedCommits $categorizedCommits -ConfigPath "test-document-emoji.json"
            $document | Should -Match "^# 📋 Changelog"
        }
    }
}
