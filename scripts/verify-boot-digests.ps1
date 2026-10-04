<#
.SYNOPSIS
Verifies that the deployed Blazor WASM boot manifest (inlined in dotnet.js)
matches the actually served .wasm files.

.DESCRIPTION
Run this after deploying to production (or a local SWA serve). The script:
  1. Downloads /_framework/dotnet.js and extracts the embedded /*json-start*/
     boot config, which carries a SHA-256 digest for every assembly.
  2. Downloads every file that carries a digest and computes its SHA-256.
  3. Fails (exit 1) on any mismatch, which is the condition behind
     "Failed to find a valid digest in the 'integrity' attribute".

.EXAMPLE
scripts/verify-boot-digests.ps1 -BaseUrl https://redmuffin.net
#>
param(
    [string]$BaseUrl = 'https://redmuffin.net'
)

$ErrorActionPreference = 'Stop'
$ua = 'OpenAI File Downloader, XaiImageApiFetch/1.0'
# Windows pwsh resolves 'curl' to the Invoke-WebRequest alias; curl.exe is the real binary.
# Linux ships only the binary. Resolve once, use everywhere.
$curl = if (Get-Command curl.exe -ErrorAction SilentlyContinue) { 'curl.exe' } else { 'curl' }
$tmp = Join-Path ([System.IO.Path]::GetTempPath()) "boot-digests-probe"
New-Item -ItemType Directory -Force -Path $tmp | Out-Null

function Get-Url([string]$Url, [string]$File) {
    & $curl -sS --max-time 60 -A $ua -o $File -w '%{http_code}' $Url
}

$dotnetJsUrl = "$BaseUrl/_framework/dotnet.js"
$dotnetJsFile = Join-Path $tmp 'dotnet.js'
$code = Get-Url $dotnetJsUrl $dotnetJsFile
if ($code -ne '200') { Write-Host "FAIL: $dotnetJsUrl returned HTTP $code"; exit 1 }

# Extract the embedded boot config: locate /*json-start*/ and brace-match to its close.
$text = Get-Content $dotnetJsFile -Raw
$start = $text.IndexOf('/*json-start*/')
if ($start -lt 0) { Write-Host 'FAIL: dotnet.js carries no /*json-start*/ boot config (wrong .NET version?)'; exit 1 }
$start += '/*json-start*/'.Length
$depth = 0
$end = -1
for ($i = $start; $i -lt $text.Length; $i++) {
    switch ($text[$i]) {
        '{' { $depth++ }
        '}' { $depth--; if ($depth -eq 0) { $end = $i; break } }
    }
    if ($end -ge 0) { break }
}
if ($end -lt 0) { Write-Host 'FAIL: boot config braces never close'; exit 1 }
$bootConfig = $text.Substring($start, $end - $start + 1) | ConvertFrom-Json

# Collect every resource entry that declares a hash.
$entries = @()
foreach ($section in $bootConfig.resources.PSObject.Properties) {
    $value = $section.Value
    if ($value -is [System.Array]) {
        foreach ($item in $value) {
            if ($item.PSObject.Properties['name'] -and $item.PSObject.Properties['hash']) {
                $entries += [pscustomobject]@{ Name = $item.name; Hash = $item.hash }
            }
        }
    }
}
if ($entries.Count -eq 0) { Write-Host 'FAIL: no hashed resources found in the boot config'; exit 1 }

Write-Host "Boot config: $($entries.Count) hashed resources. Downloading and hashing each..."

$failures = 0
$successes = 0
foreach ($entry in $entries) {
    $url = "$BaseUrl/_framework/$($entry.Name)"
    $file = Join-Path $tmp $entry.Name
    $code = Get-Url $url $file
    $expected = $entry.Hash -replace '^sha256-', ''
    if ($code -ne '200') {
        Write-Host "FAIL: $url returned HTTP $code"
        $failures++
        continue
    }
    $bytes = [System.IO.File]::ReadAllBytes($file)
    $actual = [Convert]::ToBase64String([System.Security.Cryptography.SHA256]::HashData($bytes))
    if ($actual -eq $expected) {
        $successes++
    }
    else {
        Write-Host "FAIL: $($entry.Name)"
        Write-Host "  manifest: sha256-$expected"
        Write-Host "  served  : sha256-$actual"
        $failures++
    }
}

Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue

if ($failures -gt 0) {
    Write-Host "RESULT: FAIL - $failures of $($entries.Count) resources mismatched."
    Write-Host 'A visitor with a cached older boot manifest (or a partial deploy) will hit SRI blocks.'
    exit 1
}
Write-Host "RESULT: PASS - all $($entries.Count) hashed resources match the served files."
exit 0
