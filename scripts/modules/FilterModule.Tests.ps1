# FilterModule.Tests.ps1
# Unit tests for FilterModule

# Import the module under test
$ModulePath = Join-Path $PSScriptRoot "FilterModule.psm1"
Import-Module $ModulePath -Force

Describe "FilterModule Tests" {
    
    Context "Test-MergeCommit Tests" {
        
        It "Should detect merge commit with 'Merge branch'" {
            $message = "Merge branch 'feature/new-feature'"
            Test-MergeCommit -Message $message | Should -Be $true
        }
        
        It "Should detect merge commit with 'Merge pull request'" {
            $message = "Merge pull request #123 from user/feature"
            Test-MergeCommit -Message $message | Should -Be $true
        }
        
        It "Should detect auto-merge commit" {
            $message = "Auto-merge from develop"
            Test-MergeCommit -Message $message | Should -Be $true
        }
        
        It "Should detect merge with 'into' pattern" {
            $message = "Feature implementation into main"
            Test-MergeCommit -Message $message | Should -Be $true
        }
        
        It "Should not detect regular commit as merge" {
            $message = "Add new feature to application"
            Test-MergeCommit -Message $message | Should -Be $false
        }
        
        It "Should handle case variations" {
            $message = "merge branch feature"
            Test-MergeCommit -Message $message | Should -Be $true
        }
    }
    
    Context "Test-DependabotCommit Tests" {
        
        It "Should detect Dependabot commit" {
            $message = "Dependabot: Bump lodash from 4.17.15 to 4.17.21"
            Test-DependabotCommit -Message $message | Should -Be $true
        }
        
        It "Should detect bump pattern" {
            $message = "Bump express from 4.17.1 to 4.18.0"
            Test-DependabotCommit -Message $message | Should -Be $true
        }
        
        It "Should detect dependabot[bot] pattern" {
            $message = "Update dependency by dependabot[bot]"
            Test-DependabotCommit -Message $message | Should -Be $true
        }
        
        It "Should detect auto update pattern" {
            $message = "Auto update dependencies"
            Test-DependabotCommit -Message $message | Should -Be $true
        }
        
        It "Should detect security update" {
            $message = "Security update for vulnerable package"
            Test-DependabotCommit -Message $message | Should -Be $true
        }
        
        It "Should not detect regular commit as dependabot" {
            $message = "Fix user authentication issue"
            Test-DependabotCommit -Message $message | Should -Be $false
        }
    }
    
    Context "Test-DocumentationCommit Tests" {
        
        It "Should detect docs commit" {
            $message = "Docs: Update API documentation"
            Test-DocumentationCommit -Message $message | Should -Be $true
        }
        
        It "Should detect documentation commit" {
            $message = "Documentation update for new features"
            Test-DocumentationCommit -Message $message | Should -Be $true
        }
        
        It "Should detect README update" {
            $message = "Update README.md with installation instructions"
            Test-DocumentationCommit -Message $message | Should -Be $true
        }
        
        It "Should detect typo fix" {
            $message = "Fix typo in documentation"
            Test-DocumentationCommit -Message $message | Should -Be $true
        }
        
        It "Should detect comment update" {
            $message = "Update comments in main function"
            Test-DocumentationCommit -Message $message | Should -Be $true
        }
        
        It "Should not detect code change as documentation" {
            $message = "Implement new authentication logic"
            Test-DocumentationCommit -Message $message | Should -Be $false
        }
    }
    
    Context "Test-FormattingCommit Tests" {
        
        It "Should detect formatting fix" {
            $message = "Fix formatting in main.js"
            Test-FormattingCommit -Message $message | Should -Be $true
        }
        
        It "Should detect code formatting" {
            $message = "Format code according to style guide"
            Test-FormattingCommit -Message $message | Should -Be $true
        }
        
        It "Should detect linting fix" {
            $message = "Fix linting errors in components"
            Test-FormattingCommit -Message $message | Should -Be $true
        }
        
        It "Should detect prettier fix" {
            $message = "Run prettier on all files"
            Test-FormattingCommit -Message $message | Should -Be $true
        }
        
        It "Should detect ESLint fix" {
            $message = "ESLint fix for unused variables"
            Test-FormattingCommit -Message $message | Should -Be $true
        }
        
        It "Should detect whitespace fix" {
            $message = "Fix whitespace issues"
            Test-FormattingCommit -Message $message | Should -Be $true
        }
        
        It "Should detect StyleCop fix" {
            $message = "StyleCop fix for naming conventions"
            Test-FormattingCommit -Message $message | Should -Be $true
        }
        
        It "Should detect CS warnings fix" {
            $message = "Fix CS warnings in project"
            Test-FormattingCommit -Message $message | Should -Be $true
        }
        
        It "Should not detect feature as formatting" {
            $message = "Add new user authentication feature"
            Test-FormattingCommit -Message $message | Should -Be $false
        }
    }
    
    Context "Test-PackageUpdate Tests" {
        
        It "Should detect package.json update" {
            $message = "Update package.json dependencies"
            Test-PackageUpdate -Message $message | Should -Be $true
        }
        
        It "Should detect yarn.lock update" {
            $message = "Update yarn.lock file"
            Test-PackageUpdate -Message $message | Should -Be $true
        }
        
        It "Should detect dependency update pattern" {
            $message = "Update dependencies to latest versions"
            Test-PackageUpdate -Message $message | Should -Be $true
        }
        
        It "Should detect version bump" {
            $message = "Bump version to 1.2.3"
            Test-PackageUpdate -Message $message | Should -Be $true
        }
        
        It "Should detect package upgrade" {
            $message = "Upgrade package to latest"
            Test-PackageUpdate -Message $message | Should -Be $true
        }
        
        It "Should detect specific version update" {
            $message = "Update react to v18.0.0"
            Test-PackageUpdate -Message $message | Should -Be $true
        }
        
        It "Should detect chore dependency update" {
            $message = "chore: update deps"
            Test-PackageUpdate -Message $message | Should -Be $true
        }
        
        It "Should not detect feature as package update" {
            $message = "Implement new user dashboard"
            Test-PackageUpdate -Message $message | Should -Be $false
        }
    }
    
    Context "Test-CommitAgainstPatterns Tests" {
        
        It "Should match against single pattern" {
            $message = "Fix bug in authentication"
            $patterns = @("^Fix\\s+")
            Test-CommitAgainstPatterns -Message $message -Patterns $patterns | Should -Be $true
        }
        
        It "Should match against multiple patterns" {
            $message = "Add new feature"
            $patterns = @("^Fix\\s+", "^Add\\s+", "^Remove\\s+")
            Test-CommitAgainstPatterns -Message $message -Patterns $patterns | Should -Be $true
        }
        
        It "Should not match when no patterns match" {
            $message = "Implement new logic"
            $patterns = @("^Fix\\s+", "^Add\\s+", "^Remove\\s+")
            Test-CommitAgainstPatterns -Message $message -Patterns $patterns | Should -Be $false
        }
        
        It "Should handle empty pattern array" {
            $message = "Any commit message"
            $patterns = @()
            Test-CommitAgainstPatterns -Message $message -Patterns $patterns | Should -Be $false
        }
    }
    
    Context "Get-FilteringConfig Tests" {
        
        BeforeAll {
            # Create a test config file
            $testConfig = @{
                filteringRules = @{
                    mergeCommits = @{
                        enabled = $true
                        patterns = @("^Merge\\s+")
                    }
                }
            }
            $testConfigPath = "test-config.json"
            $testConfig | ConvertTo-Json -Depth 10 | Out-File $testConfigPath -Encoding UTF8
        }
        
        AfterAll {
            # Clean up test config file
            if (Test-Path "test-config.json") {
                Remove-Item "test-config.json"
            }
        }
        
        It "Should load valid configuration file" {
            $config = Get-FilteringConfig -ConfigPath "test-config.json"
            $config | Should -Not -BeNullOrEmpty
            $config.filteringRules | Should -Not -BeNullOrEmpty
            $config.filteringRules.mergeCommits.enabled | Should -Be $true
        }
        
        It "Should return null for non-existent file" {
            $config = Get-FilteringConfig -ConfigPath "non-existent.json"
            $config | Should -BeNullOrEmpty
        }
        
        It "Should handle malformed JSON gracefully" {
            "{ invalid json" | Out-File "invalid.json" -Encoding UTF8
            $config = Get-FilteringConfig -ConfigPath "invalid.json"
            $config | Should -BeNullOrEmpty
            Remove-Item "invalid.json" -ErrorAction SilentlyContinue
        }
    }
    
    Context "Filter-Commits Tests" {
        
        BeforeAll {
            # Create test commit objects
            $script:testCommits = @(
                [PSCustomObject]@{ Hash = "abc123"; Message = "Add new feature to dashboard" },
                [PSCustomObject]@{ Hash = "def456"; Message = "Merge branch 'feature/auth'" },
                [PSCustomObject]@{ Hash = "ghi789"; Message = "Fix formatting in main.js" },
                [PSCustomObject]@{ Hash = "jkl012"; Message = "Update package.json dependencies" },
                [PSCustomObject]@{ Hash = "mno345"; Message = "Dependabot: Bump lodash version" },
                [PSCustomObject]@{ Hash = "pqr678"; Message = "Docs: Update API documentation" },
                [PSCustomObject]@{ Hash = "stu901"; Message = "Implement user authentication" }
            )
        }
        
        It "Should filter out merge commits" {
            $filtered = Filter-Commits -CommitList $script:testCommits
            $mergeCommit = $filtered | Where-Object { $_.Message -like "*Merge branch*" }
            $mergeCommit | Should -BeNullOrEmpty
        }
        
        It "Should filter out formatting commits" {
            $filtered = Filter-Commits -CommitList $script:testCommits
            $formattingCommit = $filtered | Where-Object { $_.Message -like "*formatting*" }
            $formattingCommit | Should -BeNullOrEmpty
        }
        
        It "Should filter out package updates" {
            $filtered = Filter-Commits -CommitList $script:testCommits
            $packageCommit = $filtered | Where-Object { $_.Message -like "*package.json*" }
            $packageCommit | Should -BeNullOrEmpty
        }
        
        It "Should filter out dependabot commits" {
            $filtered = Filter-Commits -CommitList $script:testCommits
            $dependabotCommit = $filtered | Where-Object { $_.Message -like "*Dependabot*" }
            $dependabotCommit | Should -BeNullOrEmpty
        }
        
        It "Should filter out documentation commits" {
            $filtered = Filter-Commits -CommitList $script:testCommits
            $docCommit = $filtered | Where-Object { $_.Message -like "*Docs:*" }
            $docCommit | Should -BeNullOrEmpty
        }
        
        It "Should keep feature and implementation commits" {
            $filtered = Filter-Commits -CommitList $script:testCommits
            $featureCommits = $filtered | Where-Object { 
                $_.Message -like "*feature*" -or $_.Message -like "*Implement*" 
            }
            $featureCommits.Count | Should -Be 2
        }
        
        It "Should return empty array for empty input" {
            $filtered = Filter-Commits -CommitList @()
            $filtered | Should -BeNullOrEmpty
        }
    }
    
    Context "Filter-CommitsWithConfig Tests" {
        
        BeforeAll {
            # Create test commits
            $script:testCommits = @(
                [PSCustomObject]@{ Hash = "abc123"; Message = "Add new feature" },
                [PSCustomObject]@{ Hash = "def456"; Message = "Merge branch 'main'" }
            )
            
            # Create test config
            $testConfig = @{
                filteringRules = @{
                    mergeCommits = @{
                        enabled = $true
                        patterns = @("^Merge\\s+")
                    }
                    dependabotCommits = @{
                        enabled = $false
                        patterns = @("^Dependabot")
                    }
                }
            }
            $testConfigPath = "test-filter-config.json"
            $testConfig | ConvertTo-Json -Depth 10 | Out-File $testConfigPath -Encoding UTF8
        }
        
        AfterAll {
            Remove-Item "test-filter-config.json" -ErrorAction SilentlyContinue
        }
        
        It "Should use configuration for filtering" {
            $filtered = Filter-CommitsWithConfig -CommitList $script:testCommits -ConfigPath "test-filter-config.json"
            $filtered.Count | Should -Be 1
            $filtered[0].Message | Should -Be "Add new feature"
        }
        
        It "Should fall back to default filtering when config not found" {
            $filtered = Filter-CommitsWithConfig -CommitList $script:testCommits -ConfigPath "non-existent.json"
            $filtered.Count | Should -Be 1
            $filtered[0].Message | Should -Be "Add new feature"
        }
        
        It "Should respect disabled filters in config" {
            # Add a dependabot commit
            $commitsWithDependabot = $script:testCommits + @(
                [PSCustomObject]@{ Hash = "xyz789"; Message = "Dependabot update" }
            )
            $filtered = Filter-CommitsWithConfig -CommitList $commitsWithDependabot -ConfigPath "test-filter-config.json"
            
            # Should keep dependabot commit since it's disabled in config
            $dependabotCommit = $filtered | Where-Object { $_.Message -like "*Dependabot*" }
            $dependabotCommit | Should -Not -BeNullOrEmpty
        }
    }
}
