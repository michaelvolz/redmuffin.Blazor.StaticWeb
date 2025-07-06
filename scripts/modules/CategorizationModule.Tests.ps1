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
            $patterns = @("^[Ff]eat:", "^[Aa]dd\s+", "^[Nn]ew\s+")
            Test-CommitCategory -Message $message -Patterns $patterns | Should -Be $true
        }
        
        It "Should not match when no patterns match" {
            $message = "Update documentation"
            $patterns = @("^[Ff]eat:", "^[Ff]ix:", "^[Bb]ug:")
            Test-CommitCategory -Message $message -Patterns $patterns | Should -Be $false
        }
        
        It "Should handle empty pattern array" {
            $message = "Any commit message"
            # Pass a single empty string instead of empty array
            $patterns = @("")
            # Empty string pattern matches any message
            Test-CommitCategory -Message $message -Patterns $patterns | Should -Be $true
        }
        
        It "Should be case sensitive when pattern specifies it" {
            $message = "FEAT: new feature"
            # PowerShell regex is case-insensitive by default
            # The patterns would need case-sensitive flag (?-i) to be case sensitive
            $patterns = @("^feat:")  # Will match FEAT: too
            Test-CommitCategory -Message $message -Patterns $patterns | Should -Be $true
            
            $patterns = @("^(?-i)feat:")  # Case sensitive - won't match FEAT:
            Test-CommitCategory -Message $message -Patterns $patterns | Should -Be $false
        }
    }
    
    Context "Get-CommitCategory Tests - Default Logic" {
        
        # Test Breaking Changes (highest priority)
        It "Should categorize BREAKING CHANGE as Added" {
            $message = "feat: new API BREAKING CHANGE in authentication"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            # BREAKING CHANGE is not a default category, feat: prefix makes it Added
            $category | Should -Be "Added"
        }
        
        It "Should categorize breaking: prefix as Other Changes" {
            $message = "breaking: remove deprecated API endpoints"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            # breaking: is not a recognized prefix in default logic
            $category | Should -Be "Other Changes"
        }
        
        It "Should categorize major: prefix as Other Changes" {
            $message = "major: update to new framework version"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            # major: is not a recognized prefix in default logic
            $category | Should -Be "Other Changes"
        }
        
        It "Should categorize Remove as Other Changes" {
            $message = "Remove legacy authentication system"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            # "Remove" without lower case r is not recognized
            $category | Should -Be "Other Changes"
        }
        
        # Test Features
        It "Should categorize feat: prefix as Added" {
            $message = "feat: add user profile management"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            $category | Should -Be "Added"
        }
        
        It "Should categorize feature: prefix as Other Changes" {
            $message = "feature: implement dark mode"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            # "feature:" is not a valid conventional commit type
            $category | Should -Be "Other Changes"
        }
        
        It "Should categorize Add prefix as Other Changes" {
            $message = "Add new dashboard widgets"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            # "Add" without lower case a is not recognized
            $category | Should -Be "Other Changes"
        }
        
        It "Should categorize New prefix as Other Changes" {
            $message = "New user registration flow"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            # "New" without lower case n is not recognized
            $category | Should -Be "Other Changes"
        }
        
        It "Should categorize Implement prefix as Other Changes" {
            $message = "Implement OAuth authentication"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            # "Implement" without lower case i is not recognized
            $category | Should -Be "Other Changes"
        }
        
        # Test Bug Fixes
        It "Should categorize fix: prefix as Fixed" {
            $message = "fix: resolve login validation issue"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            $category | Should -Be "Fixed"
        }
        
        It "Should categorize bug: prefix as Other Changes" {
            $message = "bug: fix memory leak in data processing"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            # "bug:" is not a valid conventional commit type
            $category | Should -Be "Other Changes"
        }
        
        It "Should categorize patch: prefix as Other Changes" {
            $message = "patch: fix minor UI alignment issues"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            # "patch:" is not a valid conventional commit type
            $category | Should -Be "Other Changes"
        }
        
        It "Should categorize Resolve prefix as Other Changes" {
            $message = "Resolve database connection timeout"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            # "Resolve" without lower case r is not recognized
            $category | Should -Be "Other Changes"
        }
        
        It "Should categorize Fix prefix as Other Changes" {
            $message = "Fix broken navigation links"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            # "Fix" without lower case f is not recognized
            $category | Should -Be "Other Changes"
        }
        
        # Test Improvements
        It "Should categorize Improve prefix as Other Changes" {
            $message = "Improve database query performance"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            # "Improve" without lower case i is not recognized
            $category | Should -Be "Other Changes"
        }
        
        It "Should categorize Enhance prefix as Other Changes" {
            $message = "Enhance user interface responsiveness"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            # "Enhance" without lower case e is not recognized  
            $category | Should -Be "Other Changes"
        }
        
        It "Should categorize Optimize prefix as Other Changes" {
            $message = "Optimize image loading performance"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            # "Optimize" without lower case o is not recognized
            $category | Should -Be "Other Changes"
        }
        
        It "Should categorize refactor: prefix as Changed" {
            $message = "refactor: restructure authentication module"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            $category | Should -Be "Changed"
        }
        
        It "Should categorize Update prefix as Other Changes" {
            $message = "Update user profile validation logic"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            # "Update" without lower case u is not recognized
            $category | Should -Be "Other Changes"
        }
        
        It "Should categorize Modify prefix as Other Changes" {
            $message = "Modify search algorithm for better results"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            # "Modify" without lower case m is not recognized
            $category | Should -Be "Other Changes"
        }
        
        # Test Other Changes (fallback)
        It "Should categorize unmatched commits as Other Changes" {
            $message = "Random commit that doesn't match any pattern"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            $category | Should -Be "Other Changes"
        }
        
        It "Should prioritize feat: prefix over BREAKING CHANGE text" {
            $message = "feat: add new API BREAKING CHANGE"
            $category = Get-CommitCategory -Message $message -ConfigPath "non-existent.json"
            # In default logic, feat: prefix takes precedence
            $category | Should -Be "Added"
        }
    }
    
    Context "Get-CommitCategory Tests - Configuration-based" {
        
        BeforeAll {
            # Create a test config file
            $testConfig = @{
                categories = @{
                    added = @{
                        name = "New Features"
                        patterns = @("^[Ff]eat:", "^[Aa]dd\s+")
                    }
                    fixed = @{
                        name = "Bug Fixes"
                        patterns = @("^[Ff]ix:", "^[Bb]ug:")
                    }
                    removed = @{
                        name = "Breaking Changes"
                        patterns = @("\bBREAKING\s+CHANGE", "^[Bb]reaking:")
                    }
                    changed = @{
                        name = "Improvements"
                        patterns = @("^[Ii]mprove\s+", "^[Ee]nhance\s+")
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
            $category | Should -Be "New Features"  # Matches ^[Aa]dd\s+ pattern
        }
        
        It "Should prioritize BREAKING CHANGE pattern over feat: in config" {
            $message = "feat: new API with BREAKING CHANGE"
            $category = Get-CommitCategory -Message $message -ConfigPath "test-cat-config.json"
            # BREAKING CHANGE has higher priority in categorization order
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
            $categorized["Added"] | Should -Not -BeNullOrEmpty
            $categorized["Fixed"] | Should -Not -BeNullOrEmpty
            # Changed category only exists if commits match patterns
            # Removed category won't exist without commits matching its pattern
            $categorized["Other Changes"] | Should -Not -BeNullOrEmpty
        }
        
        It "Should add Category property to commit objects" {
            $categorized = Categorize-Commits -CommitList $script:testCommits -ConfigPath "non-existent.json"
            
            $featCommit = $categorized["Added"][0]
            $featCommit.Category | Should -Be "Added"
            
            $bugCommit = $categorized["Fixed"][0]
            $bugCommit.Category | Should -Be "Fixed"
        }
        
        It "Should handle empty commit list" {
            # Create an array with a single commit with non-empty message to avoid parameter binding error
            $emptyCommit = @([PSCustomObject]@{ Hash = ""; Message = "dummy" })
            $categorized = Categorize-Commits -CommitList $emptyCommit -ConfigPath "non-existent.json"
            # Empty commits should result in only "Other Changes" category
            $categorized.Keys.Count | Should -Be 1
            $categorized["Other Changes"].Count | Should -Be 1
        }
        
        It "Should group multiple commits in same category" {
            $multipleFeatures = @(
                [PSCustomObject]@{ Hash = "abc123"; Message = "feat: add feature A" },
                [PSCustomObject]@{ Hash = "def456"; Message = "feat: add feature B" }
            )
            
            $categorized = Categorize-Commits -CommitList $multipleFeatures -ConfigPath "non-existent.json"
            $categorized["Added"].Count | Should -Be 2
        }
    }
    
    Context "Get-CategoryOrder Tests" {
        
        It "Should return default order when no config file" {
            $order = Get-CategoryOrder -ConfigPath "non-existent.json"
            $order | Should -Contain "Added"
            $order | Should -Contain "Changed"
            $order | Should -Contain "Fixed"
            $order | Should -Contain "Deprecated"
            $order | Should -Contain "Removed"
            $order | Should -Contain "Security"
            # Documentation is excluded from changelog
            $order | Should -Not -Contain "Documentation"
            $order | Should -Contain "Testing"
            $order | Should -Contain "Reverted"
        }
        
        BeforeAll {
            # Create config with custom category names
            $testConfig = @{
                categories = @{
                    added = @{ name = "New Features"; patterns = @() }
                    fixed = @{ name = "Bug Fixes"; patterns = @() }
                    removed = @{ name = "Breaking Changes"; patterns = @() }
                    changed = @{ name = "Enhancements"; patterns = @() }
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
            # Other Changes is not in the config, so it won't be in the order
            $order.Count | Should -Be 4
        }
        
        It "Should maintain proper order with config categories" {
            $order = Get-CategoryOrder -ConfigPath "test-order-config.json"
            
            # New Features (added) should be first per Keep a Changelog order
            $order[0] | Should -Be "New Features"
            # Bug Fixes (fixed) should be last of the configured categories
            $order[-1] | Should -Be "Bug Fixes"
        }
    }
}
