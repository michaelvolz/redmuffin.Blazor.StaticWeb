# CommitParser.psm1
# Module for parsing git commit lines

function Parse-CommitLine {
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    <#
    .SYNOPSIS
    Parses a single commit line from git log --oneline format
    
    .PARAMETER CommitLine
    The commit line in format "hash commit message"
    
    .PARAMETER LineNumber
    Line number for error reporting
    
    .RETURNS
    PSCustomObject with Hash and Message properties
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$CommitLine,
        
        [Parameter(Mandatory = $false)]
        [int]$LineNumber = 0
    )
    
    # Trim whitespace
    $CommitLine = $CommitLine.Trim()
    
    if ([string]::IsNullOrWhiteSpace($CommitLine)) {
        throw "Empty commit line"
    }
    
    # Git log --oneline format: "hash commit message"
    # Hash is typically 7+ characters, followed by a space
    if ($CommitLine -match '^([a-f0-9]{7,40})\s+(.+)$') {
        $hash = $matches[1]
        $message = $matches[2].Trim()
        
        # Validate hash
        if ($hash.Length -lt 7) {
            throw "Invalid commit hash length: $hash"
        }
        
        # Validate message
        if ([string]::IsNullOrWhiteSpace($message)) {
            throw "Empty commit message for hash: $hash"
        }
        
        return [PSCustomObject]@{
            Hash = $hash
            Message = $message
            OriginalLine = $CommitLine
            LineNumber = $LineNumber
        }
    }
    else {
        throw "Invalid commit line format. Expected 'hash message', got: '$CommitLine'"
    }
}

function Test-CommitFormat {
    <#
    .SYNOPSIS
    Tests if a commit line is in valid git log --oneline format
    
    .PARAMETER CommitLine
    The commit line to test
    
    .RETURNS
    Boolean indicating if the format is valid
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$CommitLine
    )
    
    try {
        $null = Parse-CommitLine -CommitLine $CommitLine
        return $true
    }
    catch {
        return $false
    }
}

function Get-CommitHash {
    <#
    .SYNOPSIS
    Extracts just the commit hash from a commit line
    
    .PARAMETER CommitLine
    The commit line in git log --oneline format
    
    .RETURNS
    The commit hash string
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$CommitLine
    )
    
    if ($CommitLine -match '^([a-f0-9]{7,40})\s+') {
        return $matches[1]
    }
    else {
        throw "Cannot extract hash from: $CommitLine"
    }
}

function Get-CommitMessage {
    <#
    .SYNOPSIS
    Extracts just the commit message from a commit line
    
    .PARAMETER CommitLine
    The commit line in git log --oneline format
    
    .RETURNS
    The commit message string
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$CommitLine
    )
    
    if ($CommitLine -match '^[a-f0-9]{7,40}\s+(.+)$') {
        return $matches[1].Trim()
    }
    else {
        throw "Cannot extract message from: $CommitLine"
    }
}

# Export functions
Export-ModuleMember -Function Parse-CommitLine, Test-CommitFormat, Get-CommitHash, Get-CommitMessage
