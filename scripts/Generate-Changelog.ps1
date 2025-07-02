# Generate-Changelog.ps1
# Main script for generating changelog from git-commits.txt

param(
    [string]$InputFile = "git-commits.txt",
    [string]$OutputFile = "CHANGELOG.md",
    [switch]$Update,
    [switch]$Preview
)

# Import required modules
$ModulePath = Join-Path $PSScriptRoot "modules"
Import-Module (Join-Path $ModulePath "CommitParser.psm1") -Force
Import-Module (Join-Path $ModulePath "FilterModule.psm1") -Force
Import-Module (Join-Path $ModulePath "CategorizationModule.psm1") -Force
Import-Module (Join-Path $ModulePath "FileGenerator.psm1") -Force
Import-Module (Join-Path $ModulePath "UpdateModule.psm1") -Force

function Read-GitCommits {
    <#
    .SYNOPSIS
    Reads and parses git commits from the input file
    
    .PARAMETER FilePath
    Path to the git-commits.txt file
    
    .RETURNS
    Array of commit objects with Hash and Message properties
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath
    )
    
    Write-Host "Reading commits from: $FilePath" -ForegroundColor Green
    
    # Check if file exists
    if (-not (Test-Path $FilePath)) {
        throw "Input file not found: $FilePath"
    }
    
    # Read file content
    try {
        $content = Get-Content $FilePath -Encoding UTF8 -ErrorAction Stop
        if (-not $content) {
            throw "Input file is empty: $FilePath"
        }
        
        Write-Host "Found $($content.Count) commits in input file" -ForegroundColor Yellow
        
        # Parse each line
        $commits = @()
        $lineNumber = 0
        
        foreach ($line in $content) {
            $lineNumber++
            
            if ([string]::IsNullOrWhiteSpace($line)) {
                Write-Warning "Skipping empty line at line $lineNumber"
                continue
            }
            
            try {
                $parsedCommit = Parse-CommitLine -CommitLine $line -LineNumber $lineNumber
                if ($parsedCommit) {
                    $commits += $parsedCommit
                }
            }
            catch {
                Write-Warning "Failed to parse line $lineNumber`: $line - $($_.Exception.Message)"
                continue
            }
        }
        
        Write-Host "Successfully parsed $($commits.Count) valid commits" -ForegroundColor Green
        return $commits
    }
    catch {
        throw "Failed to read input file: $($_.Exception.Message)"
    }
}

# Main execution
if ($MyInvocation.InvocationName -ne '.') {
    try {
        Write-Host "Starting Changelog Generation..." -ForegroundColor Cyan
        Write-Host "Input File: $InputFile" -ForegroundColor White
        Write-Host "Output File: $OutputFile" -ForegroundColor White
        
        # Read commits
        $commits = Read-GitCommits -FilePath $InputFile
        
        # Filter out non-essential commits
        $filteredCommits = Filter-CommitsWithConfig -CommitList $commits
        $filteredCount = $commits.Count - $filteredCommits.Count
        Write-Host "Filtered out $filteredCount non-essential commits" -ForegroundColor Yellow
        
        # Ensure we have full paths
        $fullOutputPath = if ([System.IO.Path]::IsPathRooted($OutputFile)) {
            $OutputFile
        } else {
            Join-Path (Get-Location) $OutputFile
        }
        
        Write-Host "Full output path: $fullOutputPath" -ForegroundColor White
        
        if ($Update -and (Test-Path $fullOutputPath)) {
            Write-Host "Update mode: Appending new entries to existing changelog" -ForegroundColor Magenta
            
            # Update existing changelog
            $success = Update-ExistingChangelog -FilePath $fullOutputPath -NewCommits $filteredCommits -CreateBackup
        }
        else {
            Write-Host "Generate mode: Creating new changelog file" -ForegroundColor Magenta
            
            # Categorize commits
            $categorizedCommits = Categorize-Commits -CommitList $filteredCommits
            
            # Generate new changelog file
            $success = Write-ChangelogToFile -FilePath $fullOutputPath -CategorizedCommits $categorizedCommits -CreateBackup -CommitCount $commits.Count -FilteredCount $filteredCount
            
            # Save last processed commit for future incremental updates
            if ($success -and $filteredCommits.Count -gt 0) {
                Save-LastProcessedCommit -CommitHash $filteredCommits[0].Hash
            }
        }
        
        if (-not $success) {
            throw "Changelog operation failed"
        }
        
        Write-Host "`nChangelog generation completed successfully!" -ForegroundColor Green
    }
    catch {
        Write-Error "Failed to generate changelog: $($_.Exception.Message)"
        exit 1
    }
}
