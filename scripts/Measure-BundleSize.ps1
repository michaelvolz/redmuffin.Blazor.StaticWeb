<#
.SYNOPSIS
    Measures Blazor WebAssembly bundle size and generates detailed reports.

.DESCRIPTION
    This script analyzes the published Blazor WASM output to measure bundle sizes,
    track optimization progress, and generate reports for size monitoring.

.PARAMETER Configuration
    Build configuration to analyze (default: Release)

.PARAMETER OutputPath
    Custom output path for the published application

.PARAMETER GenerateReport
    Generate detailed HTML report with size breakdown

.PARAMETER BaselinePath
    Path to baseline measurement file for comparison

.EXAMPLE
    .\Measure-BundleSize.ps1
    Measures the Release build bundle size

.EXAMPLE
    .\Measure-BundleSize.ps1 -GenerateReport -BaselinePath "baseline.json"
    Generates detailed report comparing to baseline
#>

[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$OutputPath = "",
    [switch]$GenerateReport,
    [string]$BaselinePath = ""
)

# Script configuration
$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path $PSScriptRoot -Parent
$ProjectPath = Join-Path $ProjectRoot "src\redmuffin.Blazor.StaticWeb\redmuffin.Blazor.StaticWeb.csproj"
$DefaultOutputPath = Join-Path $ProjectRoot "src\redmuffin.Blazor.StaticWeb\bin\$Configuration\net9.0\publish\wwwroot"
$ReportsPath = Join-Path $ProjectRoot "reports\bundle-size"

function Write-Header {
    param([string]$Title)
    Write-Host ""
    Write-Host "=" * 60 -ForegroundColor Cyan
    Write-Host $Title -ForegroundColor Cyan
    Write-Host "=" * 60 -ForegroundColor Cyan
}

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-Host $Title -ForegroundColor Yellow
    Write-Host "-" * $Title.Length -ForegroundColor Yellow
}

function Format-FileSize {
    param([long]$Bytes)
    
    if ($Bytes -ge 1GB) {
        return "{0:N2} GB" -f ($Bytes / 1GB)
    }
    elseif ($Bytes -ge 1MB) {
        return "{0:N2} MB" -f ($Bytes / 1MB)
    }
    elseif ($Bytes -ge 1KB) {
        return "{0:N2} KB" -f ($Bytes / 1KB)
    }
    else {
        return "$Bytes bytes"
    }
}

function Get-DirectorySize {
    param([string]$Path)
    
    if (-not (Test-Path $Path)) {
        return 0
    }
    
    $size = (Get-ChildItem -Path $Path -Recurse -File | Measure-Object -Property Length -Sum).Sum
    return [long]$size
}

function Get-FileAnalysis {
    param([string]$Path, [string]$Pattern = "*")
    
    if (-not (Test-Path $Path)) {
        return @()
    }
    
    $files = Get-ChildItem -Path $Path -Filter $Pattern -File | Sort-Object Length -Descending
    
    return $files | ForEach-Object {
        [PSCustomObject]@{
            Name = $_.Name
            Size = $_.Length
            SizeFormatted = Format-FileSize $_.Length
            Extension = $_.Extension
            LastModified = $_.LastWriteTime
        }
    }
}

function Measure-BundleSize {
    param([string]$PublishPath)
    
    Write-Header "Blazor WASM Bundle Size Analysis"
    Write-Host "Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
    Write-Host "Configuration: $Configuration" -ForegroundColor Gray
    Write-Host "Publish Path: $PublishPath" -ForegroundColor Gray
    
    # Verify publish path exists
    if (-not (Test-Path $PublishPath)) {
        throw "Publish path not found: $PublishPath. Please run 'dotnet publish -c $Configuration' first."
    }
    
    $frameworkPath = Join-Path $PublishPath "_framework"
    if (-not (Test-Path $frameworkPath)) {
        throw "_framework directory not found. This doesn't appear to be a published Blazor WASM application."
    }
    
    # Overall size analysis
    Write-Section "Overall Bundle Size"
    $totalSize = Get-DirectorySize $PublishPath
    $frameworkSize = Get-DirectorySize $frameworkPath
    $staticSize = $totalSize - $frameworkSize
    
    Write-Host "Total Bundle Size: $(Format-FileSize $totalSize)" -ForegroundColor Green
    Write-Host "Framework Size:    $(Format-FileSize $frameworkSize)" -ForegroundColor White
    Write-Host "Static Assets:     $(Format-FileSize $staticSize)" -ForegroundColor White
    
    # WASM files analysis
    Write-Section "WebAssembly Files (.wasm)"
    $wasmFiles = Get-FileAnalysis $frameworkPath "*.wasm"
    $wasmTotalSize = ($wasmFiles | Measure-Object -Property Size -Sum).Sum
    
    Write-Host "Total WASM Size: $(Format-FileSize $wasmTotalSize)" -ForegroundColor Green
    Write-Host "WASM File Count: $($wasmFiles.Count)" -ForegroundColor White
    Write-Host ""
    Write-Host "Top 10 Largest WASM Files:" -ForegroundColor Cyan
    $wasmFiles | Select-Object -First 10 | ForEach-Object {
        $percentage = ($_.Size / $wasmTotalSize) * 100
        Write-Host "  $($_.SizeFormatted.PadLeft(10)) ($($percentage.ToString('N1'))%) - $($_.Name)" -ForegroundColor White
    }
    
    # Compressed files analysis
    Write-Section "Compressed Files"
    $brFiles = Get-FileAnalysis $frameworkPath "*.br"
    $gzFiles = Get-FileAnalysis $frameworkPath "*.gz"
    
    if ($brFiles.Count -gt 0) {
        $brTotalSize = ($brFiles | Measure-Object -Property Size -Sum).Sum
        Write-Host "Brotli Compressed: $(Format-FileSize $brTotalSize) ($($brFiles.Count) files)" -ForegroundColor Green
    }
    
    if ($gzFiles.Count -gt 0) {
        $gzTotalSize = ($gzFiles | Measure-Object -Property Size -Sum).Sum
        Write-Host "Gzip Compressed:   $(Format-FileSize $gzTotalSize) ($($gzFiles.Count) files)" -ForegroundColor Green
    }
    
    # JavaScript and other assets
    Write-Section "Other Framework Assets"
    $jsFiles = Get-FileAnalysis $frameworkPath "*.js"
    $jsonFiles = Get-FileAnalysis $frameworkPath "*.json"
    $datFiles = Get-FileAnalysis $frameworkPath "*.dat"
    
    if ($jsFiles.Count -gt 0) {
        $jsTotalSize = ($jsFiles | Measure-Object -Property Size -Sum).Sum
        Write-Host "JavaScript Files: $(Format-FileSize $jsTotalSize) ($($jsFiles.Count) files)" -ForegroundColor White
    }
    
    if ($jsonFiles.Count -gt 0) {
        $jsonTotalSize = ($jsonFiles | Measure-Object -Property Size -Sum).Sum
        Write-Host "JSON Files:       $(Format-FileSize $jsonTotalSize) ($($jsonFiles.Count) files)" -ForegroundColor White
    }
    
    if ($datFiles.Count -gt 0) {
        $datTotalSize = ($datFiles | Measure-Object -Property Size -Sum).Sum
        Write-Host "Data Files:       $(Format-FileSize $datTotalSize) ($($datFiles.Count) files)" -ForegroundColor White
        Write-Host "  (Includes timezone, collation, and other runtime data)" -ForegroundColor Gray
    }
    
    # Create measurement object
    $measurement = [PSCustomObject]@{
        Timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
        Configuration = $Configuration
        TotalSize = $totalSize
        FrameworkSize = $frameworkSize
        StaticSize = $staticSize
        WasmSize = $wasmTotalSize
        WasmFileCount = $wasmFiles.Count
        BrotliSize = if ($brFiles.Count -gt 0) { ($brFiles | Measure-Object -Property Size -Sum).Sum } else { 0 }
        GzipSize = if ($gzFiles.Count -gt 0) { ($gzFiles | Measure-Object -Property Size -Sum).Sum } else { 0 }
        LargestWasmFiles = $wasmFiles | Select-Object -First 5
        AllFiles = @{
            Wasm = $wasmFiles
            JavaScript = $jsFiles
            Json = $jsonFiles
            Data = $datFiles
            Brotli = $brFiles
            Gzip = $gzFiles
        }
    }
    
    return $measurement
}

function Compare-WithBaseline {
    param(
        [PSCustomObject]$Current,
        [PSCustomObject]$Baseline
    )
    
    Write-Section "Comparison with Baseline"
    Write-Host "Baseline: $($Baseline.Timestamp)" -ForegroundColor Gray
    Write-Host "Current:  $($Current.Timestamp)" -ForegroundColor Gray
    Write-Host ""
    
    $sizeDiff = $Current.TotalSize - $Baseline.TotalSize
    $sizePercent = if ($Baseline.TotalSize -gt 0) { ($sizeDiff / $Baseline.TotalSize) * 100 } else { 0 }
    
    $color = if ($sizeDiff -lt 0) { "Green" } else { "Red" }
    $symbol = if ($sizeDiff -lt 0) { "[DOWN]" } else { "[UP]" }
    
    $absSizeDiff = [Math]::Abs($sizeDiff)
    Write-Host "Total Size Change: $symbol $(Format-FileSize $absSizeDiff) ($([Math]::Abs($sizePercent).ToString('N1'))%)" -ForegroundColor $color
    
    # Framework size comparison
    $frameworkDiff = $Current.FrameworkSize - $Baseline.FrameworkSize
    $frameworkPercent = if ($Baseline.FrameworkSize -gt 0) { ($frameworkDiff / $Baseline.FrameworkSize) * 100 } else { 0 }
    
    $frameworkColor = if ($frameworkDiff -lt 0) { "Green" } else { "Red" }
    $frameworkSymbol = if ($frameworkDiff -lt 0) { "[DOWN]" } else { "[UP]" }
    
    $absFrameworkDiff = [Math]::Abs($frameworkDiff)
    Write-Host "Framework Change:  $frameworkSymbol $(Format-FileSize $absFrameworkDiff) ($([Math]::Abs($frameworkPercent).ToString('N1'))%)" -ForegroundColor $frameworkColor
    
    # WASM size comparison
    $wasmDiff = $Current.WasmSize - $Baseline.WasmSize
    $wasmPercent = if ($Baseline.WasmSize -gt 0) { ($wasmDiff / $Baseline.WasmSize) * 100 } else { 0 }
    
    $wasmColor = if ($wasmDiff -lt 0) { "Green" } else { "Red" }
    $wasmSymbol = if ($wasmDiff -lt 0) { "[DOWN]" } else { "[UP]" }
    
    $absWasmDiff = [Math]::Abs($wasmDiff)
    Write-Host "WASM Change:       $wasmSymbol $(Format-FileSize $absWasmDiff) ($([Math]::Abs($wasmPercent).ToString('N1'))%)" -ForegroundColor $wasmColor
}

function Save-Measurement {
    param(
        [PSCustomObject]$Measurement,
        [string]$OutputFile
    )
    
    # Ensure reports directory exists
    $reportsDir = Split-Path $OutputFile -Parent
    if (-not (Test-Path $reportsDir)) {
        New-Item -ItemType Directory -Path $reportsDir -Force | Out-Null
    }
    
    # Save as JSON
    $Measurement | ConvertTo-Json -Depth 10 | Set-Content -Path $OutputFile -Encoding UTF8
    Write-Host "Measurement saved to: $OutputFile" -ForegroundColor Green
}

function Generate-HtmlReport {
    param(
        [PSCustomObject]$Measurement,
        [PSCustomObject]$Baseline = $null
    )
    
    $reportFile = Join-Path $ReportsPath "bundle-size-report-$(Get-Date -Format 'yyyyMMdd-HHmmss').html"
    
    $html = @"
<!DOCTYPE html>
<html>
<head>
    <title>Blazor WASM Bundle Size Report</title>
    <style>
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 20px; background-color: #f5f5f5; }
        .container { max-width: 1200px; margin: 0 auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        h1 { color: #2c3e50; border-bottom: 3px solid #3498db; padding-bottom: 10px; }
        h2 { color: #34495e; margin-top: 30px; }
        .metric { display: inline-block; margin: 10px; padding: 15px; background: #ecf0f1; border-radius: 5px; min-width: 200px; }
        .metric-value { font-size: 24px; font-weight: bold; color: #2980b9; }
        .metric-label { font-size: 14px; color: #7f8c8d; }
        .file-list { max-height: 400px; overflow-y: auto; border: 1px solid #bdc3c7; border-radius: 5px; }
        .file-item { padding: 8px; border-bottom: 1px solid #ecf0f1; display: flex; justify-content: space-between; }
        .file-item:nth-child(even) { background-color: #f8f9fa; }
        .size-bar { height: 20px; background: linear-gradient(90deg, #3498db, #2980b9); border-radius: 3px; margin: 2px 0; }
        .comparison { padding: 15px; margin: 10px 0; border-radius: 5px; }
        .improvement { background-color: #d5f4e6; border-left: 4px solid #27ae60; }
        .regression { background-color: #fadbd8; border-left: 4px solid #e74c3c; }
        .timestamp { color: #7f8c8d; font-size: 12px; }
    </style>
</head>
<body>
    <div class="container">
        <h1>Blazor WebAssembly Bundle Size Report</h1>
        <p class="timestamp">Generated: $($Measurement.Timestamp)</p>
        <p class="timestamp">Configuration: $($Measurement.Configuration)</p>
        
        <h2>Overall Metrics</h2>
        <div class="metric">
            <div class="metric-value">$(Format-FileSize $Measurement.TotalSize)</div>
            <div class="metric-label">Total Bundle Size</div>
        </div>
        <div class="metric">
            <div class="metric-value">$(Format-FileSize $Measurement.FrameworkSize)</div>
            <div class="metric-label">Framework Size</div>
        </div>
        <div class="metric">
            <div class="metric-value">$(Format-FileSize $Measurement.WasmSize)</div>
            <div class="metric-label">WebAssembly Size</div>
        </div>
        <div class="metric">
            <div class="metric-value">$($Measurement.WasmFileCount)</div>
            <div class="metric-label">WASM Files</div>
        </div>
"@

    if ($Baseline) {
        $sizeDiff = $Measurement.TotalSize - $Baseline.TotalSize
        $sizePercent = if ($Baseline.TotalSize -gt 0) { ($sizeDiff / $Baseline.TotalSize) * 100 } else { 0 }
        $comparisonClass = if ($sizeDiff -lt 0) { "improvement" } else { "regression" }
        $symbol = if ($sizeDiff -lt 0) { "[DOWN]" } else { "[UP]" }
        
        $absSizeDiffHtml = [Math]::Abs($sizeDiff)
        $html += @"
        
        <h2>Comparison with Baseline</h2>
        <div class="comparison $comparisonClass">
            <strong>Size Change:</strong> $symbol $(Format-FileSize $absSizeDiffHtml) ($([Math]::Abs($sizePercent).ToString('N1'))%)<br>
            <strong>Baseline:</strong> $($Baseline.Timestamp) - $(Format-FileSize $Baseline.TotalSize)<br>
            <strong>Current:</strong> $($Measurement.Timestamp) - $(Format-FileSize $Measurement.TotalSize)
        </div>
"@
    }

    $html += @"
        
        <h2>Largest WebAssembly Files</h2>
        <div class="file-list">
"@

    $maxSize = ($Measurement.LargestWasmFiles | Measure-Object -Property Size -Maximum).Maximum
    foreach ($file in $Measurement.LargestWasmFiles) {
        $percentage = ($file.Size / $Measurement.WasmSize) * 100
        $barWidth = ($file.Size / $maxSize) * 100
        
        $html += @"
            <div class="file-item">
                <div>
                    <strong>$($file.Name)</strong><br>
                    <div class="size-bar" style="width: $($barWidth)%;"></div>
                </div>
                <div>
                    $($file.SizeFormatted)<br>
                    <small>{0:N1}% of WASM</small>
                </div>
            </div>
"@ -f $percentage.ToString('N1')
    }

    $html += @"
        </div>
        
        <h2>File Type Breakdown</h2>
        <div class="metric">
            <div class="metric-value">$($Measurement.AllFiles.Wasm.Count)</div>
            <div class="metric-label">WASM Files</div>
        </div>
        <div class="metric">
            <div class="metric-value">$($Measurement.AllFiles.JavaScript.Count)</div>
            <div class="metric-label">JavaScript Files</div>
        </div>
        <div class="metric">
            <div class="metric-value">$($Measurement.AllFiles.Json.Count)</div>
            <div class="metric-label">JSON Files</div>
        </div>
        <div class="metric">
            <div class="metric-value">$($Measurement.AllFiles.Data.Count)</div>
            <div class="metric-label">Data Files</div>
        </div>
    </div>
</body>
</html>
"@

    # Ensure reports directory exists
    if (-not (Test-Path $ReportsPath)) {
        New-Item -ItemType Directory -Path $ReportsPath -Force | Out-Null
    }
    
    $html | Set-Content -Path $reportFile -Encoding UTF8
    Write-Host "HTML report generated: $reportFile" -ForegroundColor Green
    
    return $reportFile
}

# Main execution
try {
    # Determine output path
    $publishPath = if ($OutputPath) { $OutputPath } else { $DefaultOutputPath }
    
    # Measure bundle size
    $measurement = Measure-BundleSize -PublishPath $publishPath
    
    # Load baseline if provided
    $baseline = $null
    if ($BaselinePath -and (Test-Path $BaselinePath)) {
        try {
            $baseline = Get-Content -Path $BaselinePath -Raw | ConvertFrom-Json
            Compare-WithBaseline -Current $measurement -Baseline $baseline
        }
        catch {
            Write-Warning "Failed to load baseline from $BaselinePath : $($_.Exception.Message)"
        }
    }
    
    # Save current measurement
    $measurementFile = Join-Path $ReportsPath "measurement-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    Save-Measurement -Measurement $measurement -OutputFile $measurementFile
    
    # Generate HTML report if requested
    if ($GenerateReport) {
        $reportFile = Generate-HtmlReport -Measurement $measurement -Baseline $baseline
        
        # Try to open the report
        if (Get-Command "Start-Process" -ErrorAction SilentlyContinue) {
            try {
                Start-Process $reportFile
            }
            catch {
                Write-Host "Report saved but could not open automatically: $reportFile" -ForegroundColor Yellow
            }
        }
    }
    
    Write-Section "Summary"
    Write-Host "Total Bundle Size: $(Format-FileSize $measurement.TotalSize)" -ForegroundColor Green
    Write-Host "Framework Size:    $(Format-FileSize $measurement.FrameworkSize)" -ForegroundColor White
    Write-Host "WASM Size:         $(Format-FileSize $measurement.WasmSize)" -ForegroundColor White
    
    if ($baseline) {
        $sizeDiff = $measurement.TotalSize - $baseline.TotalSize
        $sizePercent = if ($baseline.TotalSize -gt 0) { ($sizeDiff / $baseline.TotalSize) * 100 } else { 0 }
        $color = if ($sizeDiff -lt 0) { "Green" } else { "Red" }
        $symbol = if ($sizeDiff -lt 0) { "[DOWN]" } else { "[UP]" }
        
        $absSizeDiffSummary = [Math]::Abs($sizeDiff)
        Write-Host "Change from baseline: $symbol $(Format-FileSize $absSizeDiffSummary) ($([Math]::Abs($sizePercent).ToString('N1'))%)" -ForegroundColor $color
    }
    
    Write-Host ""
    Write-Host "Measurement saved to: $measurementFile" -ForegroundColor Gray
    
    # Return measurement object for potential pipeline use
    return $measurement
}
catch {
    Write-Error "Bundle size measurement failed: $($_.Exception.Message)"
    exit 1
}