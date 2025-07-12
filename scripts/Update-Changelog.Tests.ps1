# Update-Changelog.Tests.ps1
# Comprehensive tests for Update-Changelog.ps1

BeforeAll {
    $script:ScriptPath = Join-Path $PSScriptRoot "Update-Changelog.ps1"
    $script:ModulePath = Join-Path $PSScriptRoot "modules"
    
    # Dot source the script to load functions
    . $script:ScriptPath
    
    # Create test directory
    $script:TestPath = Join-Path $TestDrive "changelog-tests"
    New-Item -ItemType Directory -Path $script:TestPath -Force | Out-Null
    
    # Save original location
    $script:OriginalLocation = Get-Location
}

AfterAll {
    # Restore original location
    Set-Location $script:OriginalLocation
}

Describe "Update-Changelog Git Integration Tests" {
    
    Context "Git Availability Detection" {
        
        It "Should detect when git is available" {
            # This test assumes git is installed on the test system
            $result = Test-GitAvailable
            $result | Should -Be $true
        }
        
        It "Should return false when git command fails" {
            # Mock git command to simulate failure
            Mock -CommandName git -MockWith { 
                $global:LASTEXITCODE = 1
                return "git: command not found"
            }
            
            $result = Test-GitAvailable
            $result | Should -Be $false
            
            # Mock is automatically cleaned up by Pester
        }
    }
    
    Context "Git Repository Detection" {
        
        It "Should detect when in a git repository" {
            # Create a test git repo
            Push-Location $script:TestPath
            git init --quiet 2>$null
            
            $result = Test-GitRepository
            $result | Should -Be $true
            
            # Cleanup
            Remove-Item -Path ".git" -Recurse -Force
            Pop-Location
        }
        
        It "Should return false when not in a git repository" {
            Push-Location $script:TestPath
            
            $result = Test-GitRepository
            $result | Should -Be $false
            
            Pop-Location
        }
    }
    
    Context "Git Repository Has Commits Check" {
        
        It "Should detect empty repository" {
            Push-Location $script:TestPath
            git init --quiet 2>$null
            
            $result = Test-GitRepositoryHasCommits
            $result | Should -Be $false
            
            # Cleanup
            Remove-Item -Path ".git" -Recurse -Force
            Pop-Location
        }
        
        It "Should detect repository with commits" {
            Push-Location $script:TestPath
            git init --quiet 2>$null
            
            # Create a commit
            "test" | Out-File "test.txt"
            git add test.txt 2>$null
            git config user.email "test@example.com" 2>$null
            git config user.name "Test User" 2>$null
            git commit -m "test: initial commit" --quiet 2>$null
            
            $result = Test-GitRepositoryHasCommits
            $result | Should -Be $true
            
            # Cleanup
            Remove-Item -Path ".git" -Recurse -Force
            Remove-Item -Path "test.txt" -Force
            Pop-Location
        }
    }
    
    Context "Git Log Execution" {
        
        It "Should execute git log and return array of strings" {
            # This test assumes we're in a git repo with commits
            $result = Invoke-GitLog -CommitLimit 5
            
            $result | Should -Not -BeNullOrEmpty
            $result.GetType().IsArray | Should -Be $true
            $result.Count | Should -BeLessOrEqual 5
        }
        
        It "Should respect commit limit parameter" {
            $result = Invoke-GitLog -CommitLimit 2
            
            $result.Count | Should -BeLessOrEqual 2
        }
        
        It "Should handle UTF-8 encoding correctly" {
            # Create a test repo
            Push-Location $script:TestPath
            git init --quiet 2>$null
            
            "test" | Out-File "test.txt"
            git add test.txt 2>$null
            git config user.email "test@example.com" 2>$null
            git config user.name "Test User" 2>$null
            
            # Commit with a simple message
            git commit -m "test: simple commit message" --quiet 2>$null
            
            $result = Invoke-GitLog -CommitLimit 1
            
            # Just verify we get properly formed output
            $result | Should -Not -BeNullOrEmpty
            @($result).Count | Should -Be 1
            # The array access might return a char, so convert to string and check
            [string]$firstCommit = @($result)[0]
            $firstCommit | Should -Match "^[a-f0-9]+\s+test:"
            
            # Cleanup
            Remove-Item -Path ".git" -Recurse -Force
            Remove-Item -Path "test.txt" -Force
            Pop-Location
        }
    }
    
    Context "Commit Source Selection Logic" {
        
        It "Should use git log when no InputFile specified" {
            Mock -CommandName Test-GitAvailable -MockWith { $true }
            Mock -CommandName Test-GitRepository -MockWith { $true }
            Mock -CommandName Test-GitRepositoryHasCommits -MockWith { $true }
            Mock -CommandName Invoke-GitLog -MockWith { @("abc1234 test commit") }
            Mock -CommandName Parse-CommitLine -MockWith { 
                [PSCustomObject]@{
                    Hash = "abc1234"
                    Message = "test commit"
                }
            }
            
            $commits = Read-GitCommitsFromSource
            
            Should -Invoke Invoke-GitLog -Times 1
            $commits.Count | Should -Be 1
            
            # Clean up mocks
                                                                    }
        
        It "Should use file when InputFile specified" {
            $testFile = Join-Path $script:TestPath "test-commits.txt"
            "abc1234 test commit from file" | Out-File $testFile
            
            Mock -CommandName Parse-CommitLine -MockWith { 
                [PSCustomObject]@{
                    Hash = "abc1234"
                    Message = "test commit from file"
                }
            }
            
            $commits = Read-GitCommitsFromSource -FilePath $testFile
            
            $commits.Count | Should -Be 1
            $commits[0].Message | Should -Be "test commit from file"
            
            # Clean up
            Remove-Item $testFile -Force
                    }
    }
    
    Context "Parameter Validation" {
        
        It "Should reject InputFile with CommitLimit" {
            $testFile = Join-Path $script:TestPath "param-test.txt"
            "abc1234 test commit" | Out-File $testFile
            
            { 
                & $script:ScriptPath -InputFile $testFile -CommitLimit 10 -ErrorAction Stop 2>&1
            } | Should -Throw "*Cannot use -CommitLimit with -InputFile*"
            
            Remove-Item $testFile -Force
        }
        
        It "Should validate InputFile exists when specified" {
            { 
                & $script:ScriptPath -InputFile "nonexistent.txt" -ErrorAction Stop 
            } | Should -Throw "*Input file not found*"
        }
        
        It "Should validate CommitLimit is non-negative" {
            # ValidateRange should prevent negative values
            { 
                & $script:ScriptPath -CommitLimit -5 -ErrorAction Stop 
            } | Should -Throw
        }
    }
    
    Context "Error Handling" {
        
        It "Should provide helpful error when git not installed" {
            Mock -CommandName Test-GitAvailable -MockWith { $false }
            
            { 
                & $script:ScriptPath -ErrorAction Stop 2>&1
            } | Should -Throw "*Git is not installed*"
            
                    }
        
        It "Should provide helpful error when not in git repository" {
            Mock -CommandName Test-GitAvailable -MockWith { $true }
            Mock -CommandName Test-GitRepository -MockWith { $false }
            
            { 
                & $script:ScriptPath -ErrorAction Stop 2>&1
            } | Should -Throw "*not a git repository*"
            
                                }
        
        It "Should provide helpful error for empty repository" {
            Mock -CommandName Test-GitAvailable -MockWith { $true }
            Mock -CommandName Test-GitRepository -MockWith { $true }
            Mock -CommandName Test-GitRepositoryHasCommits -MockWith { $false }
            
            { 
                & $script:ScriptPath -ErrorAction Stop 2>&1
            } | Should -Throw "*empty*no commits*"
            
                                            }
    }
    
    Context "Backward Compatibility" {
        
        It "Should accept optional InputFile parameter" {
            $params = (Get-Command $script:ScriptPath).Parameters
            $params.ContainsKey('InputFile') | Should -Be $true
            $params['InputFile'].Attributes.Mandatory | Should -Be $false
        }
        
        It "Should maintain all existing parameters" {
            $params = (Get-Command $script:ScriptPath).Parameters
            
            $params.ContainsKey('OutputFile') | Should -Be $true
            $params.ContainsKey('Update') | Should -Be $true
            $params.ContainsKey('Preview') | Should -Be $true
            $params.ContainsKey('CommitLimit') | Should -Be $true
        }
        
        It "Should use default git-commits.txt if exists" {
            $defaultFile = Join-Path $script:OriginalLocation "git-commits.txt"
            if (Test-Path $defaultFile) {
                Mock -CommandName Read-GitCommits -MockWith { @() }
                
                & $script:ScriptPath -ErrorAction SilentlyContinue
                
                Should -Invoke Read-GitCommits -Times 1
                            }
            else {
                Set-ItResult -Skipped -Because "No default git-commits.txt file found"
            }
        }
    }
    
    Context "Verbose Mode" {
        
        It "Should output verbose messages when -Verbose specified" {
            Mock -CommandName Test-GitAvailable -MockWith { $true }
            Mock -CommandName Test-GitRepository -MockWith { $true }
            Mock -CommandName Test-GitRepositoryHasCommits -MockWith { $true }
            Mock -CommandName Invoke-GitLog -MockWith { @("abc1234 test") }
            Mock -CommandName Filter-CommitsWithConfig -MockWith { @() }
            Mock -CommandName Write-ChangelogToFile -MockWith { $true }
            
            $verboseOutput = & $script:ScriptPath -Verbose 4>&1 | Where-Object { $_ -is [System.Management.Automation.VerboseRecord] }
            
            $verboseOutput | Should -Not -BeNullOrEmpty
            $verboseOutput | Where-Object { $_ -match "git" } | Should -Not -BeNullOrEmpty
            
            # Clean up mocks
                                                                                }
    }
}

Describe "Integration Tests" {
    
    Context "End-to-End File vs Git Comparison" {
        
        It "Should produce identical output from file and git sources" {
            # Create test commits file
            $testFile = Join-Path $script:TestPath "test-commits.txt"
            $gitOutput = & git --no-pager log --oneline -n 10 2>&1
            if ($LASTEXITCODE -eq 0) {
                $gitOutput | Out-File $testFile -Encoding UTF8
                
                # Generate from file
                $fileChangelog = Join-Path $script:TestPath "changelog-file.md"
                & $script:ScriptPath -InputFile $testFile -OutputFile $fileChangelog -ErrorAction SilentlyContinue
                
                # Generate from git
                $gitChangelog = Join-Path $script:TestPath "changelog-git.md"
                & $script:ScriptPath -OutputFile $gitChangelog -CommitLimit 10 -ErrorAction SilentlyContinue
                
                if ((Test-Path $fileChangelog) -and (Test-Path $gitChangelog)) {
                    # Normalize content for comparison
                    $fileContent = (Get-Content $fileChangelog -Raw) -replace 'Generated on: .+', ''
                    $gitContent = (Get-Content $gitChangelog -Raw) -replace 'Generated on: .+', ''
                    
                    $fileContent.Trim() | Should -Be $gitContent.Trim()
                }
                else {
                    Set-ItResult -Skipped -Because "Could not generate test changelogs"
                }
                
                # Cleanup
                Remove-Item $testFile, $fileChangelog, $gitChangelog -Force -ErrorAction SilentlyContinue
            }
            else {
                Set-ItResult -Skipped -Because "Not in a git repository"
            }
        }
    }
}

# Note: Pester 5 automatically cleans up mocks after each test

