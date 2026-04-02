<#
.SYNOPSIS
    Scans the system for indicators of compromise from npm supply chain attacks.

.DESCRIPTION
    Uses Everything (es.exe) for instant system-wide filesystem searches.
    Checks for known malicious packages, artifacts, and configuration issues.

.PARAMETER Quiet
    Suppress non-essential output, return only summary.

.PARAMETER Json
    Output results as JSON for programmatic consumption.

.EXAMPLE
    ./Scan-NpmSupplyChain.ps1

.EXAMPLE
    ./Scan-NpmSupplyChain.ps1 -Json

.NOTATION
    Author: Security Audit Script
    Created: 2026-04-02
#>

param(
    [switch]$Quiet,
    [switch]$Json
)

$ErrorActionPreference = 'Stop'

$script:Timestamp = (Get-Date -Format 'yyyy-MM-ddTHH:mm:ss')
$script:Status = 'PASS'
$script:Checks = [System.Collections.ArrayList]::new()
$script:Findings = [System.Collections.ArrayList]::new()
$script:Recommendations = [System.Collections.ArrayList]::new()

function Add-Check {
    param(
        [string]$Name,
        [string]$Status,
        [string]$Message,
        [string[]]$Paths = @()
    )

    $check = [PSCustomObject]@{
        Name = $Name
        Status = $Status
        Message = $Message
        Paths = $Paths
    }

    [void]$script:Checks.Add($check)

    if ($Status -eq 'FAIL') {
        $script:Status = 'FAIL'
    }
}

function Invoke-EsSearch {
    param([string]$Query)

    try {
        $output = & es.exe $Query 2>$null
        return $output | Where-Object { $_ -and $_.Trim() }
    }
    catch {
        return @()
    }
}

function Test-EsPathExists {
    param([string]$Query)

    $results = Invoke-EsSearch -Query $Query
    return ($results.Count -gt 0)
}

function Get-EsPaths {
    param([string]$Query)

    return Invoke-EsSearch -Query $Query
}

Write-Host "Scanning for npm supply chain attack indicators..." -ForegroundColor Cyan
Write-Host "Using Everything (es.exe) for instant filesystem searches" -ForegroundColor Gray
Write-Host ""

<# IMPORTANT: Keep this list updated with known malicious packages #>
$MaliciousPackages = @{
    'axios@1.14.1' = @{
        Shasum = '2553649f2322049666871cea80a5d0d6adc700ca'
        Description = 'Compromised axios version (March 2026 supply chain attack)'
        AttackDate = '2026-03-30'
    }
    'axios@0.30.4' = @{
        Shasum = 'd6f3f62fd3b9f5432f5782b62d8cfd5247d5ee71'
        Description = 'Compromised axios version (March 2026 supply chain attack)'
        AttackDate = '2026-03-30'
    }
    'plain-crypto-js@4.2.1' = @{
        Shasum = '07d889e2dadce6f3910dcbc253317d28ca61c766'
        Description = 'Malicious dependency used in axios supply chain attack'
        AttackDate = '2026-03-30'
    }
}

$MalwareArtifacts = @{
    '6202033.ps1' = 'Windows PowerShell RAT payload (axios attack)'
    '6202033.vbs' = 'Windows VBScript dropper (axios attack)'
    '6202033' = 'Generic malware artifact pattern'
    'com.apple.act.mond' = 'macOS RAT payload (axios attack)'
    'ld.py' = 'Linux Python RAT (context-dependent, check path)'
}

$PersistencePaths = @{
    'C:\ProgramData\wt.exe' = 'Windows persistence (renamed PowerShell)'
    'C:\ProgramData\*.bat' = 'Potential persistence script'
}

$C2Indicators = @('sfrclak', '142.11.206.73')

Write-Host "[CHECK 1/8] Scanning for malicious npm packages..." -ForegroundColor Yellow

$axiosPackages = Get-EsPaths -Query "axios/package.json"
$suspectAxios = @()

foreach ($pkg in $axiosPackages) {
    if (Test-Path $pkg) {
        $content = Get-Content $pkg -Raw -ErrorAction SilentlyContinue
        if ($content -match '"version":\s*"1\.14\.1"') {
            $suspectAxios += $pkg
        }
        elseif ($content -match '"version":\s*"0\.30\.4"') {
            $suspectAxios += $pkg
        }
    }
}

if ($suspectAxios.Count -eq 0) {
    Add-Check -Name "Axios versions" -Status "PASS" -Message "No compromised axios versions found"
}
else {
    Add-Check -Name "Axios versions" -Status "FAIL" -Message "Found compromised axios versions" -Paths $suspectAxios
    [void]$script:Findings.Add("CRITICAL: Compromised axios versions detected. These may have executed malicious code.")
    [void]$script:Recommendations.Add("Uninstall affected axios packages immediately")
    [void]$script:Recommendations.Add("Run full malware scan")
    [void]$script:Recommendations.Add("Rotate all credentials that may have been exposed")
}

$plainCryptoFound = @()

if (Test-EsPathExists -Query "plain-crypto") {
    $plainCryptoPaths = Get-EsPaths -Query "plain-crypto"
    foreach ($path in $plainCryptoPaths) {
        if ($path -match "plain-crypto-js") {
            $plainCryptoFound += $path
        }
    }
}

if ($plainCryptoFound.Count -eq 0) {
    Add-Check -Name "plain-crypto-js" -Status "PASS" -Message "No malicious plain-crypto-js dependency found"
}
else {
    Add-Check -Name "plain-crypto-js" -Status "FAIL" -Message "Found malicious dependency" -Paths $plainCryptoFound
    [void]$script:Findings.Add("CRITICAL: plain-crypto-js found. This is the malware delivery vehicle.")
    [void]$script:Recommendations.Add("Remove plain-crypto-js immediately")
    [void]$script:Recommendations.Add("Check for malware artifacts (see subsequent checks)")
}

Write-Host "[CHECK 2/8] Scanning for malware artifacts..." -ForegroundColor Yellow

$malwareFound = @()

foreach ($artifact in $MalwareArtifacts.Keys) {
    $results = Get-EsPaths -Query $artifact
    foreach ($result in $results) {
        if ($result -match $artifact) {
            $malwareFound += $result
        }
    }
}

if ($malwareFound.Count -eq 0) {
    Add-Check -Name "Malware artifacts" -Status "PASS" -Message "No malware artifacts found"
}
else {
    Add-Check -Name "Malware artifacts" -Status "FAIL" -Message "Found potential malware artifacts" -Paths $malwareFound
    [void]$script:Findings.Add("CRITICAL: Malware artifacts detected. System may be compromised.")
    [void]$script:Recommendations.Add("Isolate system and perform full incident response")
}

Write-Host "[CHECK 3/8] Scanning for persistence mechanisms..." -ForegroundColor Yellow

$persistenceFound = @()

foreach ($path in $PersistencePaths.Keys) {
    if (Test-Path $path -ErrorAction SilentlyContinue) {
        $persistenceFound += $path
    }
}

if (Test-Path "C:\ProgramData\wt.exe" -ErrorAction SilentlyContinue) {
    $persistenceFound += "C:\ProgramData\wt.exe"
}

if ($persistenceFound.Count -eq 0) {
    Add-Check -Name "Persistence mechanisms" -Status "PASS" -Message "No suspicious persistence mechanisms found"
}
else {
    Add-Check -Name "Persistence mechanisms" -Status "FAIL" -Message "Found suspicious files in persistence locations" -Paths $persistenceFound
    [void]$script:Findings.Add("HIGH: Potential persistence mechanism detected")
    [void]$script:Recommendations.Add("Investigate and remove suspicious files")
}

Write-Host "[CHECK 4/8] Scanning for C2 domain references..." -ForegroundColor Yellow

$c2Found = @()

foreach ($domain in $C2Indicators) {
    $results = Get-EsPaths -Query $domain
    if ($results) {
        $c2Found += $results | Select-Object -First 5
    }
}

if ($c2Found.Count -eq 0) {
    Add-Check -Name "C2 indicators" -Status "PASS" -Message "No C2 domain references found in filesystem"
}
else {
    Add-Check -Name "C2 indicators" -Status "FAIL" -Message "Found potential C2 domain references" -Paths $c2Found
    [void]$script:Findings.Add("CRITICAL: C2 domain references found. Active compromise likely.")
    [void]$script:Recommendations.Add("Block these domains at firewall immediately")
    [void]$script:Recommendations.Add("Investigate network traffic for C2 communication")
}

Write-Host "[CHECK 5/8] Checking npm configuration..." -ForegroundColor Yellow

try {
    $npmConfig = npm config list 2>$null

    $ignoreScripts = npm config get ignore-scripts 2>$null
    $minReleaseAge = npm config get min-release-age 2>$null

    $configIssues = @()

    if ($ignoreScripts -ne 'true') {
        $configIssues += "ignore-scripts is NOT set (scripts can auto-execute)"
    }

    if (-not $minReleaseAge -or $minReleaseAge -eq 'undefined' -or $minReleaseAge -eq 'null') {
        $configIssues += "min-release-age is NOT set (no quarantine protection)"
    }
    elseif ([int]$minReleaseAge -lt 10080) {
        $configIssues += "min-release-age is $minReleaseAge minutes (< 7 days recommended)"
    }

    if ($configIssues.Count -eq 0) {
        Add-Check -Name "npm config" -Status "PASS" -Message "npm security settings configured correctly"
    }
    else {
        Add-Check -Name "npm config" -Status "WARN" -Message "npm security settings need attention: $($configIssues -join '; ')"
        [void]$script:Recommendations.Add("Run: npm config set ignore-scripts true")
        [void]$script:Recommendations.Add("Run: npm config set min-release-age 10080")
    }
}
catch {
    Add-Check -Name "npm config" -Status "WARN" -Message "Could not read npm config"
}

Write-Host "[CHECK 6/8] Checking Windows Run keys..." -ForegroundColor Yellow

$runKeyHKCU = Get-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -ErrorAction SilentlyContinue
$runKeyHKLM = Get-ItemProperty -Path "HKLM:\Software\Microsoft\Windows\CurrentVersion\Run" -ErrorAction SilentlyContinue

$suspiciousRunEntries = @()

$knownGood = @(
    'OneDrive', 'Signal', 'Speech', 'WingetUI', 'MicrosoftEdge', 'Docker',
    'Flow.Launcher', 'SecurityHealth', 'Everything', 'RtkAudUService', 'RTHDVCPL',
    'Realtek', 'Windows', 'Microsoft', 'NVIDIA', 'Intel', 'AMD'
)

if ($runKeyHKCU) {
    $runKeyHKCU.PSObject.Properties | Where-Object { $_.Name -notin @('PSPath', 'PSParentPath', 'PSChildName', 'PSDrive', 'PSProvider') } | ForEach-Object {
        $name = $_.Name
        $value = $_.Value
        $isKnown = $knownGood | Where-Object { $name -match $_ }
        if (-not $isKnown -and $value -match '\.ps1|\.bat|\.cmd|\.vbs|powershell.*-e|curl|wget|bitsadmin') {
            $suspiciousRunEntries += "HKCU\$name : $value"
        }
    }
}

if ($runKeyHKLM) {
    $runKeyHKLM.PSObject.Properties | Where-Object { $_.Name -notin @('PSPath', 'PSParentPath', 'PSChildName', 'PSDrive', 'PSProvider') } | ForEach-Object {
        $name = $_.Name
        $value = $_.Value
        $isKnown = $knownGood | Where-Object { $name -match $_ }
        if (-not $isKnown -and $value -match '\.ps1|\.bat|\.cmd|\.vbs|powershell.*-e|curl|wget|bitsadmin') {
            $suspiciousRunEntries += "HKLM\$name : $value"
        }
    }
}

if ($suspiciousRunEntries.Count -eq 0) {
    Add-Check -Name "Run keys" -Status "PASS" -Message "No suspicious Run key entries found"
}
else {
    Add-Check -Name "Run keys" -Status "WARN" -Message "Suspicious Run key entries detected" -Paths $suspiciousRunEntries
    [void]$script:Findings.Add("MEDIUM: Suspicious Run key entries - investigate manually")
}

Write-Host "[CHECK 7/8] Checking for installed axios versions..." -ForegroundColor Yellow

$allAxiosVersions = @()

foreach ($pkg in $axiosPackages) {
    if (Test-Path $pkg) {
        $content = Get-Content $pkg -Raw -ErrorAction SilentlyContinue
        if ($content -match '"version":\s*"([^"]+)"') {
            $version = $Matches[1]
            $dir = Split-Path $pkg -Parent
            $allAxiosVersions += [PSCustomObject]@{
                Path = $dir
                Version = $version
                Status = if ($version -match '^(1\.14\.1|0\.30\.4)$') { 'COMPROMISED' } elseif ($version -match '^1\.14') { 'UNSAFE' } else { 'OK' }
            }
        }
    }
}

$compromisedCount = ($allAxiosVersions | Where-Object { $_.Status -eq 'COMPROMISED' }).Count
$unsafeCount = ($allAxiosVersions | Where-Object { $_.Status -eq 'UNSAFE' }).Count
$safeCount = ($allAxiosVersions | Where-Object { $_.Status -eq 'OK' }).Count

if ($compromisedCount -gt 0) {
    Add-Check -Name "Axios inventory" -Status "FAIL" -Message "Found $compromisedCount compromised version(s), $unsafeCount potentially unsafe, $safeCount safe"
}
elseif ($unsafeCount -gt 0) {
    Add-Check -Name "Axios inventory" -Status "WARN" -Message "Found $unsafeCount axios versions from the attack branch (recommend downgrade), $safeCount safe"
}
else {
    Add-Check -Name "Axios inventory" -Status "PASS" -Message "All $safeCount axios installations are safe versions"
}

Write-Host "[CHECK 8/8] Checking npm cache for compromised packages..." -ForegroundColor Yellow

$npmCachePath = "$env:LOCALAPPDATA\npm-cache\_cacache"
$cacheClean = $true

if (Test-Path $npmCachePath) {
    $cacheItems = Get-ChildItem -Path $npmCachePath -Recurse -File -ErrorAction SilentlyContinue
    if ($cacheItems.Count -eq 0) {
        Add-Check -Name "npm cache" -Status "PASS" -Message "npm cache is empty (clean)"
    }
    else {
        Add-Check -Name "npm cache" -Status "INFO" -Message "npm cache contains $($cacheItems.Count) items (run 'npm cache clean --force' to purge)"
    }
}
else {
    Add-Check -Name "npm cache" -Status "PASS" -Message "npm cache directory not found"
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SCAN COMPLETE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if ($Json) {
    $output = [PSCustomObject]@{
        Timestamp = $script:Timestamp
        Status = $script:Status
        Checks = $script:Checks
        Findings = $script:Findings
        Recommendations = $script:Recommendations
    }
    $output | ConvertTo-Json -Depth 5
}
else {
    $statusColor = if ($script:Status -eq 'PASS') { 'Green' } elseif ($script:Status -eq 'WARN') { 'Yellow' } else { 'Red' }
    Write-Host "OVERALL STATUS: " -NoNewline
    Write-Host $script:Status -ForegroundColor $statusColor
    Write-Host ""

    Write-Host "CHECKS:" -ForegroundColor Yellow
    foreach ($check in $script:Checks) {
        $color = switch ($check.Status) {
            'PASS' { 'Green' }
            'FAIL' { 'Red' }
            'WARN' { 'Yellow' }
            'INFO' { 'Gray' }
            default { 'White' }
        }
        Write-Host "  [$($check.Status)] " -NoNewline -ForegroundColor $color
        Write-Host "$($check.Name): $($check.Message)"
        if ($check.Paths -and $check.Paths.Count -gt 0) {
            foreach ($path in $check.Paths | Select-Object -First 5) {
                Write-Host "    -> $path" -ForegroundColor Gray
            }
            if ($check.Paths.Count -gt 5) {
                Write-Host "    ... and $($check.Paths.Count - 5) more" -ForegroundColor Gray
            }
        }
    }

    if ($script:Findings.Count -gt 0) {
        Write-Host ""
        Write-Host "FINDINGS:" -ForegroundColor Red
        foreach ($finding in $script:Findings) {
            Write-Host "  - $finding"
        }
    }

    if ($script:Recommendations.Count -gt 0) {
        Write-Host ""
        Write-Host "RECOMMENDATIONS:" -ForegroundColor Yellow
        foreach ($rec in $script:Recommendations) {
            Write-Host "  - $rec"
        }
    }

    Write-Host ""
    Write-Host "Axios installations found: $($allAxiosVersions.Count)" -ForegroundColor Gray
    if ($allAxiosVersions.Count -gt 0) {
        $allAxiosVersions | Format-Table -AutoSize | Out-String | Write-Host
    }
}

exit $(if ($script:Status -eq 'PASS') { 0 } elseif ($script:Status -eq 'WARN') { 1 } else { 2 })