# CommitParser.Tests.ps1
# Unit tests for CommitParser module

# Import the module under test
$ModulePath = Join-Path $PSScriptRoot "CommitParser.psm1"
Import-Module $ModulePath -Force

Describe "CommitParser Module Tests" {
    
    Context "Parse-CommitLine Tests" {
        
        It "Should parse valid commit line correctly" {
            $commitLine = "abc1234 Initial commit"
            $result = Parse-CommitLine -CommitLine $commitLine
            
            $result.Hash | Should -Be "abc1234"
            $result.Message | Should -Be "Initial commit"
            $result.OriginalLine | Should -Be $commitLine
        }
        
        It "Should parse commit with long hash" {
            $commitLine = "abcdef1234567890abcdef1234567890abcdef12 Add feature X"
            $result = Parse-CommitLine -CommitLine $commitLine
            
            $result.Hash | Should -Be "abcdef1234567890abcdef1234567890abcdef12"
            $result.Message | Should -Be "Add feature X"
        }
        
        It "Should parse commit with complex message" {
            $commitLine = "abc1234 fix: resolve issue #123 with authentication"
            $result = Parse-CommitLine -CommitLine $commitLine
            
            $result.Hash | Should -Be "abc1234"
            $result.Message | Should -Be "fix: resolve issue #123 with authentication"
        }
        
        It "Should handle commit line with extra whitespace" {
            $commitLine = "  abc1234   Initial commit  "
            $result = Parse-CommitLine -CommitLine $commitLine
            
            $result.Hash | Should -Be "abc1234"
            $result.Message | Should -Be "Initial commit"
        }
        
        It "Should throw error for invalid hash format" {
            $commitLine = "123 Invalid hash"
            { Parse-CommitLine -CommitLine $commitLine } | Should -Throw "*Invalid commit line format*"
        }
        
        It "Should throw error for empty commit line" {
            { Parse-CommitLine -CommitLine "" } | Should -Throw "*Empty commit line*"
        }
        
        It "Should throw error for whitespace-only commit line" {
            { Parse-CommitLine -CommitLine "   " } | Should -Throw "*Empty commit line*"
        }
        
        It "Should throw error for commit line without message" {
            $commitLine = "abc1234"
            { Parse-CommitLine -CommitLine $commitLine } | Should -Throw "*Invalid commit line format*"
        }
        
        It "Should include line number in result when provided" {
            $commitLine = "abc1234 Test commit"
            $result = Parse-CommitLine -CommitLine $commitLine -LineNumber 42
            
            $result.LineNumber | Should -Be 42
        }
    }
    
    Context "Test-CommitFormat Tests" {
        
        It "Should return true for valid commit format" {
            $commitLine = "abc1234 Valid commit"
            Test-CommitFormat -CommitLine $commitLine | Should -Be $true
        }
        
        It "Should return false for invalid commit format" {
            $commitLine = "Invalid format"
            Test-CommitFormat -CommitLine $commitLine | Should -Be $false
        }
        
        It "Should return false for empty string" {
            Test-CommitFormat -CommitLine "" | Should -Be $false
        }
    }
    
    Context "Get-CommitHash Tests" {
        
        It "Should extract hash correctly" {
            $commitLine = "abc1234 Some commit message"
            $result = Get-CommitHash -CommitLine $commitLine
            
            $result | Should -Be "abc1234"
        }
        
        It "Should extract long hash correctly" {
            $commitLine = "abcdef1234567890abcdef1234567890abcdef12 Message"
            $result = Get-CommitHash -CommitLine $commitLine
            
            $result | Should -Be "abcdef1234567890abcdef1234567890abcdef12"
        }
        
        It "Should throw error for invalid format" {
            $commitLine = "Invalid format"
            { Get-CommitHash -CommitLine $commitLine } | Should -Throw "*Cannot extract hash*"
        }
    }
    
    Context "Get-CommitMessage Tests" {
        
        It "Should extract message correctly" {
            $commitLine = "abc1234 This is the commit message"
            $result = Get-CommitMessage -CommitLine $commitLine
            
            $result | Should -Be "This is the commit message"
        }
        
        It "Should handle message with special characters" {
            $commitLine = "abc1234 fix: resolve issue #123 (urgent)"
            $result = Get-CommitMessage -CommitLine $commitLine
            
            $result | Should -Be "fix: resolve issue #123 (urgent)"
        }
        
        It "Should trim whitespace from message" {
            $commitLine = "abc1234   Message with spaces   "
            $result = Get-CommitMessage -CommitLine $commitLine
            
            $result | Should -Be "Message with spaces"
        }
        
        It "Should throw error for invalid format" {
            $commitLine = "Invalid format"
            { Get-CommitMessage -CommitLine $commitLine } | Should -Throw "*Cannot extract message*"
        }
    }
}
