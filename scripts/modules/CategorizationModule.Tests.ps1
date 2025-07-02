# CategorizationModule.Tests.ps1
# Unit tests for CategorizationModule

# Import the module under test
$ModulePath = Join-Path $PSScriptRoot "CategorizationModule.psm1"
Import-Module $ModulePath -Force

Describe "CategorizationModule Tests" {
    
    Context "Get-CategorizationConfig Tests" {
        
        BeforeAll {
            # Create a test config file
            $testConfig = @{
                categories = @{
                    features = @{
                        name = "Features"
                        patterns = @("^[Ff]eat:", "^[Aa]dd\\s+")
                    }
                    bugFixes = @{
                        name = "Bug Fixes"
                        patterns = @("^[Ff]ix:", "^[Bb]ug:")
                    }
                }
            }
            $testConfigPath = "test-categorization-config.json"
            $testConfig | ConvertTo-Json -Depth 10 | Out-File $testConfigPath -Encoding UTF8
        }
        
        AfterAll {
            # Clean up test config file
            Remove-Item "test-categorization-config.json" -ErrorAction SilentlyContinue
        }
        
        It "Should load valid configuration file" {
            $config = Get-CategorizationConfig -ConfigPath "test-categorization-config.json"
            $config | Should -Not -BeNullOrEmpty
            $config.features | Should -Not -BeNullOrEmpty
            $config.features.name | Should -Be "Features"
            $config.features.patterns.Count | Should -Be 2
        }
        
        It "Should return null for non-existent file" {
            $config = Get-CategorizationConfig -ConfigPath "non-existent.json"
            $config | Should -BeNullOrEmpty
        }
        
        It "Should handle malformed JSON gracefully" {
            "{ invalid json" | Out-File "invalid-cat.json" -Encoding UTF8
            $config = Get-CategorizationConfig -ConfigPath "invalid-cat.json"
            $config | Should -BeNullOrEmpty
            Remove-Item "invalid-cat.json" -ErrorAction SilentlyContinue
        }
    }
    
    Context "Test-CommitCategory Tests" {
        
        It "Should match against single pattern" {
            $message = "feat: add new user authentication"
            $patterns = @("^[Ff]eat:")
            Test-CommitCategory -Message $message -Patterns $patterns | Should -Be $true
        }
        
        It "Should match against multiple patterns" {
            $message = "Add new dashboard feature"
            $patterns = @("^[Ff]eat:", "^[Aa]dd\\s+", "^[Nn]ew\\s+")
            Test-CommitCategory -Message $message -Patterns $patterns | Should -Be $true
        }
        
        It "Should not match when no patterns match" {
            $message = "Update documentation"
            $patterns = @("^[Ff]eat:", "^[Ff]ix:", "^[Bb]ug:")
            Test-CommitCategory -Message $message -Patterns $patterns | Should -Be $false
        }
        
        It "Should handle empty pattern array" {
            $message = "Any commit message"
            $patterns = @()
            Test-CommitCategory -Message $message -Patterns $patterns | Should -Be $false
        }
        
        It "Should be case sensitive when pattern specifies it" {
            $message = "FEAT: new feature"
            $patterns = @("^[Ff]eat:")  # Should match both F and f
            Test-CommitCategory -Message $message -Patterns $patterns | Should -Be $false
            
            $patterns = @("^[Ff]EAT:")  # Should match FEAT
            Test-CommitCategory -Message $message -Patterns $patterns | Should -Be $true
        }
    }
    
    Context "Get-CommitCategory Tests - Default Logic" {
        
        # Test Breaking Changes (highest priority)
        It "Should categorize BREAKING CHANGE as Breaking Changes" {
            $message = "feat: new API BREAKING CHANGE in authentication"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            $category | Should -Be "Breaking Changes"
        }
        
        It "Should categorize breaking: prefix as Breaking Changes" {
            $message = "breaking: remove deprecated API endpoints"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            $category | Should -Be "Breaking Changes"
        }
        
        It "Should categorize major: prefix as Breaking Changes" {
            $message = "major: update to new framework version"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            $category | Should -Be "Breaking Changes"
        }
        
        It "Should categorize Remove as Breaking Changes" {
            $message = "Remove legacy authentication system"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            $category | Should -Be "Breaking Changes"
        }
        
        # Test Features
        It "Should categorize feat: prefix as Features" {
            $message = "feat: add user profile management"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            $category | Should -Be "Features"
        }
        
        It "Should categorize feature: prefix as Features" {
            $message = "feature: implement dark mode"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            $category | Should -Be "Features"
        }
        
        It "Should categorize Add prefix as Features" {
            $message = "Add new dashboard widgets"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            $category | Should -Be "Features"
        }
        
        It "Should categorize New prefix as Features" {
            $message = "New user registration flow"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            $category | Should -Be "Features"
        }
        
        It "Should categorize Implement prefix as Features" {
            $message = "Implement OAuth authentication"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            $category | Should -Be "Features"
        }
        
        # Test Bug Fixes
        It "Should categorize fix: prefix as Bug Fixes" {
            $message = "fix: resolve login validation issue"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            $category | Should -Be "Bug Fixes"
        }
        
        It "Should categorize bug: prefix as Bug Fixes" {
            $message = "bug: fix memory leak in data processing"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            $category | Should -Be "Bug Fixes"
        }
        
        It "Should categorize patch: prefix as Bug Fixes" {
            $message = "patch: fix minor UI alignment issues"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            $category | Should -Be "Bug Fixes"
        }
        
        It "Should categorize Resolve prefix as Bug Fixes" {
            $message = "Resolve database connection timeout"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            $category | Should -Be "Bug Fixes"
        }
        
        It "Should categorize Fix prefix as Bug Fixes" {
            $message = "Fix broken navigation links"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            $category | Should -Be "Bug Fixes"
        }
        
        # Test Improvements
        It "Should categorize Improve prefix as Improvements" {
            $message = "Improve database query performance"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            $category | Should -Be "Improvements"
        }
        
        It "Should categorize Enhance prefix as Improvements" {
            $message = "Enhance user interface responsiveness"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            $category | Should -Be "Improvements"
        }
        
        It "Should categorize Optimize prefix as Improvements" {
            $message = "Optimize image loading performance"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            $category | Should -Be "Improvements"
        }
        
        It "Should categorize refactor: prefix as Improvements" {
            $message = "refactor: restructure authentication module"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            $category | Should -Be "Improvements"
        }
        
        It "Should categorize Update prefix as Improvements" {
            $message = "Update user profile validation logic"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            $category | Should -Be "Improvements"
        }
        
        It "Should categorize Modify prefix as Improvements" {
            $message = "Modify search algorithm for better results"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            $category | Should -Be "Improvements"
        }
        
        # Test Other Changes (fallback)
        It "Should categorize unmatched commits as Other Changes" {
            $message = "Random commit that doesn't match any pattern"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            $category | Should -Be "Other Changes"
        }
        
        It "Should prioritize Breaking Changes over Features" {
            $message = "feat: add new API BREAKING CHANGE"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            $category | Should -Be "Breaking Changes"
        }
    }
    
    Context "Get-CommitCategory Tests - Configuration-based" {
        
        BeforeAll {
            # Create a test config file
            $testConfig = @{
                categories = @{
                    features = @{
                        name = "New Features"
                        patterns = @("^[Ff]eat:", "^[Aa]dd\\s+")
                    }
                    bugFixes = @{
                        name = "Bug Fixes"
                        patterns = @("^[Ff]ix:", "^[Bb]ug:")
                    }
                    breakingChanges = @{
                        name = "Breaking Changes"
                        patterns = @("\\bBREAKING\\s+CHANGE", "^[Bb]reaking:")
                    }
                    improvements = @{
                        name = "Improvements"
                        patterns = @("^[Ii]mprove\\s+", "^[Ee]nhance\\s+")
                    }
                }
            }
            $testConfigPath = "test-cat-config.json"
            $testConfig | ConvertTo-Json -Depth 10 | Out-File $testConfigPath -Encoding UTF8
        }
        
        AfterAll {
            Remove-Item "test-cat-config.json" -ErrorAction SilentlyContinue
        }
        
        It "Should use configuration-based category names" {
            $message = "feat: add new user dashboard"
            $category = Get-CommitCategory -Message $message -ConfigPath "test-cat-config.json"
            $category | Should -Be "New Features"  # Custom name from config
        }
        
        It "Should respect configuration patterns" {
            $message = "Add new authentication method"
            $category = Get-CommitCategory -Message $message -ConfigPath "test-cat-config.json"
            $category | Should -Be "New Features"  # Matches ^[Aa]dd\\s+ pattern
        }
        
        It "Should prioritize Breaking Changes in config" {
            $message = "feat: new API with BREAKING CHANGE"
            $category = Get-CommitCategory -Message $message -ConfigPath "test-cat-config.json"
            $category | Should -Be "Breaking Changes"
        }
        
        It "Should fall back to Other Changes when no config patterns match" {
            $message = "Some random commit message"
            $category = Get-CommitCategory -Message $message -ConfigPath "test-cat-config.json"
            $category | Should -Be "Other Changes"
        }
    }
    
    Context "Categorize-Commits Tests" {
        
        BeforeAll {
            # Create test commit objects
            $script:testCommits = @(
                [PSCustomObject]@{ Hash = "abc123"; Message = "feat: add user authentication" },
                [PSCustomObject]@{ Hash = "def456"; Message = "fix: resolve login bug" },
                [PSCustomObject]@{ Hash = "ghi789"; Message = "improve performance of queries" },
                [PSCustomObject]@{ Hash = "jkl012"; Message = "breaking: remove old API" },
                [PSCustomObject]@{ Hash = "mno345"; Message = "random commit message" }
            )
        }
        
        It "Should categorize commits into proper groups" {
            $categorized = Categorize-Commits -CommitList $script:testCommits -ConfigPath "non-existent.json"
            
            $categorized.Keys.Count | Should -BeGreaterThan 0
            $categorized["Features"] | Should -Not -BeNullOrEmpty
            $categorized["Bug Fixes"] | Should -Not -BeNullOrEmpty
            $categorized["Improvements"] | Should -Not -BeNullOrEmpty
            $categorized["Breaking Changes"] | Should -Not -BeNullOrEmpty
            $categorized["Other Changes"] | Should -Not -BeNullOrEmpty
        }
        
        It "Should add Category property to commit objects" {
            $categorized = Categorize-Commits -CommitList $script:testCommits -ConfigPath "non-existent.json"
            
            $featCommit = $categorized["Features"][0]
            $featCommit.Category | Should -Be "Features"
            
            $bugCommit = $categorized["Bug Fixes"][0]
            $bugCommit.Category | Should -Be "Bug Fixes"
        }
        
        It "Should handle empty commit list" {
            $categorized = Categorize-Commits -CommitList @() -ConfigPath "non-existent.json"
            $categorized.Keys.Count | Should -Be 0
        }
        
        It "Should group multiple commits in same category" {
            $multipleFeatures = @(
                [PSCustomObject]@{ Hash = "abc123"; Message = "feat: add feature A" },
                [PSCustomObject]@{ Hash = "def456"; Message = "feat: add feature B" }
            )
            
            $categorized = Categorize-Commits -CommitList $multipleFeatures -ConfigPath "non-existent.json"
            $categorized["Features"].Count | Should -Be 2
        }
    }
    
    Context "Get-CategoryOrder Tests" {
        
        It "Should return default order when no config file" {
            $order = Get-CategoryOrder -ConfigPath "non-existent.json"
            $order | Should -Contain "Breaking Changes"
            $order | Should -Contain "Features"
            $order | Should -Contain "Bug Fixes"
            $order | Should -Contain "Improvements"
            $order | Should -Contain "Other Changes"
            
            # Breaking Changes should be first
            $order[0] | Should -Be "Breaking Changes"
            # Other Changes should be last
            $order[-1] | Should -Be "Other Changes"
        }
        
        BeforeAll {
            # Create config with custom category names
            $testConfig = @{
                categories = @{
                    features = @{ name = "New Features"; patterns = @() }
                    bugFixes = @{ name = "Bug Fixes"; patterns = @() }
                    breakingChanges = @{ name = "Breaking Changes"; patterns = @() }
                    improvements = @{ name = "Enhancements"; patterns = @() }
                }
            }
            $testConfigPath = "test-order-config.json"
            $testConfig | ConvertTo-Json -Depth 10 | Out-File $testConfigPath -Encoding UTF8
        }
        
        AfterAll {
            Remove-Item "test-order-config.json" -ErrorAction SilentlyContinue
        }
        
        It "Should use configuration-based category names in order" {
            $order = Get-CategoryOrder -ConfigPath "test-order-config.json"
            $order | Should -Contain "New Features"
            $order | Should -Contain "Enhancements"
            $order | Should -Contain "Breaking Changes"
            $order | Should -Contain "Bug Fixes"
            $order | Should -Contain "Other Changes"
        }
        
        It "Should maintain proper order with config categories" {
            $order = Get-CategoryOrder -ConfigPath "test-order-config.json"
            
            # Breaking Changes should still be first
            $order[0] | Should -Be "Breaking Changes"
            # Other Changes should still be last
            $order[-1] | Should -Be "Other Changes"
        }
    }
}
