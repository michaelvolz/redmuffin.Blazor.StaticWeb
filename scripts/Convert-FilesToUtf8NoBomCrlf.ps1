<#
.SYNOPSIS
    Converts text files to UTF-8 WITHOUT BOM and CRLF line endings with enhanced performance and features.

.DESCRIPTION
    This script recursively searches for specified file types and converts them to UTF-8 encoding 
    WITHOUT BOM (Byte Order Mark) and ensures CRLF line endings. Features parallel processing,
    progress reporting, and safety options. If a file has a BOM, it will be removed.

.PARAMETER Path
    The root path to search for files. Defaults to current directory.

.PARAMETER Include
    Array of file patterns to include. Defaults to @("*.md", "*.cs", "*.razor")

.PARAMETER ExcludeDirectories
    Array of directory patterns to exclude. Defaults to @("bin", "debug", "obj", "node_modules", ".git")

.PARAMETER MaxFileSizeMB
    Maximum file size in MB to process. Defaults to 10MB.

.PARAMETER Backup
    Creates .bak backup files before modification.

.PARAMETER Parallel
    Enables parallel processing for better performance.

.PARAMETER MaxParallelJobs
    Maximum number of parallel jobs when using -Parallel. Defaults to 4.

.EXAMPLE
    .\Convert-FilesToUtf8NoBomCrlf.ps1
    Converts all matching files in the current directory with default settings.

.EXAMPLE
    .\Convert-FilesToUtf8NoBomCrlf.ps1 -Path "C:\MyProject" -Include "*.txt","*.xml" -Backup -Parallel
    Converts txt and xml files with backup and parallel processing.

.EXAMPLE
    .\Convert-FilesToUtf8NoBomCrlf.ps1 -WhatIf
    Shows what files would be converted without actually modifying them.

.NOTES
    Author: Optimized PowerShell Script
    Date: 2025-01-16
    Version: 3.0
    Changes: Modified to remove BOM instead of adding it
#>

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter()]
    [ValidateScript({ Test-Path $_ -PathType Container })]
    [string]$Path = ".",
    
    [Parameter()]
    [string[]]$Include = @("*.md", "*.cs", "*.razor"),
    
    [Parameter()]
    [string[]]$ExcludeDirectories = @("bin", "debug", "obj", "node_modules", ".git"),
    
    [Parameter()]
    [ValidateRange(0.1, 100)]
    [double]$MaxFileSizeMB = 10,
    
    [Parameter()]
    [switch]$Backup,
    
    [Parameter()]
    [switch]$Parallel,
    
    [Parameter()]
    [ValidateRange(1, 16)]
    [int]$MaxParallelJobs = 4
)

begin {
    Write-Verbose "Starting optimized file conversion process (UTF-8 without BOM)"
    
    # Define UTF-8 encoding WITHOUT BOM
    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    
    # Counters for statistics
    $script:processedCount = 0
    $script:skippedCount = 0
    $script:errorCount = 0
    $script:backedUpCount = 0
    $script:bomRemovedCount = 0
    
    # BOM bytes for UTF-8
    $utf8BomBytes = [byte[]]@(0xEF, 0xBB, 0xBF)
    
    # Build exclude regex pattern
    $excludePattern = ($ExcludeDirectories | ForEach-Object { [regex]::Escape($_) }) -join '|'
    $excludeRegex = "\\($excludePattern)\\"
    
    # Convert max file size to bytes
    $maxFileSizeBytes = $MaxFileSizeMB * 1MB
    
    # Function to process a single file
    function Process-SingleFile {
        param([System.IO.FileInfo]$File)
        
        try {
            # Check file size
            if ($File.Length -gt $maxFileSizeBytes) {
                Write-Warning "Skipping large file: $($File.FullName) ($([Math]::Round($File.Length / 1MB, 2)) MB)"
                if ($Parallel) { [System.Threading.Interlocked]::Increment([ref]$script:skippedCount.Value) }
                else { $script:skippedCount++ }
                return
            }
            
            # Read file efficiently
            $fileBytes = [System.IO.File]::ReadAllBytes($File.FullName)
            
            # Check if file is likely binary
            if (Test-BinaryFile -Bytes $fileBytes) {
                Write-Warning "Skipping binary file: $($File.FullName)"
                if ($Parallel) { [System.Threading.Interlocked]::Increment([ref]$script:skippedCount.Value) }
                else { $script:skippedCount++ }
                return
            }
            
            # Check for UTF-8 BOM
            $hasBom = $fileBytes.Length -ge 3 -and 
                      $fileBytes[0] -eq $utf8BomBytes[0] -and 
                      $fileBytes[1] -eq $utf8BomBytes[1] -and 
                      $fileBytes[2] -eq $utf8BomBytes[2]
            
            # Get content as string, handling BOM if present
            if ($hasBom) {
                # Skip BOM bytes when reading
                $encoding = [System.Text.UTF8Encoding]::new($false)
                $content = $encoding.GetString($fileBytes, 3, $fileBytes.Length - 3)
            } else {
                $encoding = [System.Text.UTF8Encoding]::new($false)
                $content = $encoding.GetString($fileBytes)
            }
            
            # Normalize line endings efficiently
            $normalizedContent = $content -replace '\r?\n', "`r`n"
            
            # Check if conversion is needed (has BOM or needs line ending normalization)
            $needsConversion = $hasBom -or ($content -ne $normalizedContent)
            
            if ($needsConversion) {
                if ($PSCmdlet.ShouldProcess($File.FullName, "Convert to UTF-8 WITHOUT BOM and CRLF")) {
                    # Create backup if requested
                    if ($Backup) {
                        $backupPath = "$($File.FullName).bak"
                        [System.IO.File]::Copy($File.FullName, $backupPath, $true)
                        Write-Verbose "Created backup: $backupPath"
                        if ($Parallel) { [System.Threading.Interlocked]::Increment([ref]$script:backedUpCount.Value) }
                        else { $script:backedUpCount++ }
                    }
                    
                    # Write file with UTF-8 WITHOUT BOM
                    [System.IO.File]::WriteAllText($File.FullName, $normalizedContent, $utf8NoBom)
                    
                    # Preserve original timestamps if possible
                    try {
                        $File.LastWriteTime = $File.LastWriteTime
                        $File.LastAccessTime = $File.LastAccessTime
                    } catch {
                        Write-Verbose "Could not preserve timestamps for: $($File.FullName)"
                    }
                    
                    if ($hasBom) {
                        Write-Host "✓ Converted: $($File.Name) (BOM removed)" -ForegroundColor Green
                        if ($Parallel) { [System.Threading.Interlocked]::Increment([ref]$script:bomRemovedCount.Value) }
                        else { $script:bomRemovedCount++ }
                    } else {
                        Write-Host "✓ Converted: $($File.Name)" -ForegroundColor Green
                    }
                    
                    if ($Parallel) { [System.Threading.Interlocked]::Increment([ref]$script:processedCount.Value) }
                    else { $script:processedCount++ }
                }
            }
            else {
                Write-Verbose "File already in correct format: $($File.FullName)"
                if ($Parallel) { [System.Threading.Interlocked]::Increment([ref]$script:skippedCount.Value) }
                else { $script:skippedCount++ }
            }
        }
        catch {
            Write-Error "Failed to process file '$($File.FullName)': $_"
            if ($Parallel) { [System.Threading.Interlocked]::Increment([ref]$script:errorCount.Value) }
            else { $script:errorCount++ }
        }
    }
    
    # Function to check if file is likely binary
    function Test-BinaryFile {
        param([byte[]]$Bytes)
        
        # Check first 8KB for null bytes or high concentration of non-text characters
        $checkLength = [Math]::Min($Bytes.Length, 8192)
        $nonTextCount = 0
        
        for ($i = 0; $i -lt $checkLength; $i++) {
            $byte = $Bytes[$i]
            
            # Null byte is a strong indicator of binary file
            if ($byte -eq 0) {
                return $true
            }
            
            # Count non-printable characters (excluding common whitespace)
            if ($byte -lt 32 -and $byte -ne 9 -and $byte -ne 10 -and $byte -ne 13) {
                $nonTextCount++
            }
        }
        
        # If more than 30% non-text characters, likely binary
        $nonTextRatio = $nonTextCount / $checkLength
        return $nonTextRatio -gt 0.3
    }
}

process {
    try {
        # Get all matching files
        Write-Host "Searching for files in: $Path" -ForegroundColor Cyan
        Write-Host "Include patterns: $($Include -join ', ')" -ForegroundColor Cyan
        Write-Host "Exclude directories: $($ExcludeDirectories -join ', ')" -ForegroundColor Cyan
        Write-Host "Converting to: UTF-8 WITHOUT BOM, CRLF line endings" -ForegroundColor Yellow
        
        $files = Get-ChildItem -Path $Path -Recurse -Include $Include -File |
            Where-Object { 
                $_.FullName -notmatch $excludeRegex
            }
        
        $totalFiles = @($files).Count
        Write-Host "Found $totalFiles files to process" -ForegroundColor Cyan
        
        if ($totalFiles -eq 0) {
            Write-Warning "No files found matching the criteria"
            return
        }
        
        # Process files
        if ($Parallel -and $PSVersionTable.PSVersion.Major -ge 7) {
            Write-Host "Processing files in parallel (max $MaxParallelJobs jobs)..." -ForegroundColor Yellow
            
            # Create script block for parallel execution
            $parallelBlock = {
                param($File, $ProcessFunction, $TestFunction, $Utf8NoBom, $Utf8BomBytes, $MaxFileSizeBytes, $Backup, $WhatIf)
                
                # Define functions in parallel scope
                $ProcessSingleFile = [ScriptBlock]::Create($ProcessFunction)
                $TestBinaryFile = [ScriptBlock]::Create($TestFunction)
                
                # Process the file
                & $ProcessSingleFile -File $File
            }
            
            $files | ForEach-Object -Parallel $parallelBlock -ArgumentList $_, 
                ${function:Process-SingleFile}.ToString(), 
                ${function:Test-BinaryFile}.ToString(), 
                $using:utf8NoBom, 
                $using:utf8BomBytes, 
                $using:maxFileSizeBytes, 
                $using:Backup, 
                $using:WhatIf -ThrottleLimit $MaxParallelJobs
        }
        else {
            if ($Parallel -and $PSVersionTable.PSVersion.Major -lt 7) {
                Write-Warning "Parallel processing requires PowerShell 7+. Processing sequentially..."
            }
            
            # Sequential processing with progress
            $i = 0
            foreach ($file in $files) {
                $i++
                $percentComplete = ($i / $totalFiles) * 100
                Write-Progress -Activity "Converting files" -Status "$i of $totalFiles" -PercentComplete $percentComplete
                
                Process-SingleFile -File $file
            }
            Write-Progress -Activity "Converting files" -Completed
        }
    }
    catch {
        Write-Error "An error occurred during file discovery: $_"
        throw
    }
}

end {
    # Get final counts (handle both parallel and sequential)
    $finalProcessed = if ($Parallel) { $script:processedCount.Value } else { $script:processedCount }
    $finalSkipped = if ($Parallel) { $script:skippedCount.Value } else { $script:skippedCount }
    $finalErrors = if ($Parallel) { $script:errorCount.Value } else { $script:errorCount }
    $finalBackedUp = if ($Parallel) { $script:backedUpCount.Value } else { $script:backedUpCount }
    $finalBomRemoved = if ($Parallel) { $script:bomRemovedCount.Value } else { $script:bomRemovedCount }
    
    Write-Host "`nConversion Summary:" -ForegroundColor Yellow
    Write-Host "  Files converted: $finalProcessed" -ForegroundColor Green
    if ($finalBomRemoved -gt 0) {
        Write-Host "  Files with BOM removed: $finalBomRemoved" -ForegroundColor Magenta
    }
    Write-Host "  Files skipped (already correct): $finalSkipped" -ForegroundColor Cyan
    
    if ($Backup -and $finalBackedUp -gt 0) {
        Write-Host "  Files backed up: $finalBackedUp" -ForegroundColor Blue
    }
    
    if ($finalErrors -gt 0) {
        Write-Host "  Files with errors: $finalErrors" -ForegroundColor Red
    }
    
    Write-Verbose "File conversion process completed"
}
