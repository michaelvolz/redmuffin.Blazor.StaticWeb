<#
.SYNOPSIS
Verifies that the deployed Blazor WASM boot manifest (inlined in dotnet.js)
matches the served boot files.

.DESCRIPTION
Run this after deploying to production (or against a local SWA serve). The check
covers the files a cold visitor downloads before the app starts: the JS module
runtime, the native runtime, the core assemblies, and the eager assemblies.
Lazy assemblies are skipped, because they load after boot and cannot block
startup.

The script:
  1. Downloads /_framework/dotnet.js and extracts the embedded /*json-start*/
     boot config, which carries a SHA-256 digest for every resource.
  2. Collects the boot resources and fetches them in parallel.
  3. Compares each served file to its manifest digest.
  4. Fails (exit 1) on a missing file or a hash mismatch, which is the
     condition behind "Failed to find a valid digest in the 'integrity'
     attribute".

.EXAMPLE
scripts/verify-boot-digests.ps1 -BaseUrl https://redmuffin.net
#>
param(
    [string]$BaseUrl = 'https://redmuffin.net',
    [int]$Parallelism = 16
)

$ErrorActionPreference = 'Stop'
$ua = 'OpenAI File Downloader, XaiImageApiFetch/1.0'
# Windows pwsh resolves 'curl' to the Invoke-WebRequest alias; curl.exe is the real binary.
# Linux ships only the binary. Resolve once, use everywhere.
$curl = if (Get-Command curl.exe -ErrorAction SilentlyContinue) { 'curl.exe' } else { 'curl' }

# The manifest separates boot resources from lazy ones. Boot resources decide
# whether the site starts, so they are the checked set.
$bootSections = @('jsModuleNative', 'jsModuleRuntime', 'wasmNative', 'coreAssembly', 'assembly')

# A fresh directory per run keeps a leftover file from an earlier run from
# standing in for a download that failed.
$tmp = Join-Path ([System.IO.Path]::GetTempPath()) ("boot-digests-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $tmp | Out-Null
$watch = [System.Diagnostics.Stopwatch]::StartNew()

try {
    # 1. Load the boot config inlined in dotnet.js.
    $dotnetJsFile = Join-Path $tmp 'dotnet.js'
    $code = & $curl -sS --connect-timeout 5 --max-time 15 --retry 2 --retry-delay 2 -A $ua -o $dotnetJsFile -w '%{http_code}' "$BaseUrl/_framework/dotnet.js"
    if ($code -ne '200') { Write-Host "FAIL: dotnet.js returned HTTP $code"; exit 1 }

    $text = Get-Content $dotnetJsFile -Raw
    $start = $text.IndexOf('/*json-start*/')
    if ($start -lt 0) { Write-Host 'FAIL: dotnet.js carries no /*json-start*/ boot config (wrong .NET version?)'; exit 1 }
    $start += '/*json-start*/'.Length
    $depth = 0
    $end = -1
    for ($i = $start; $i -lt $text.Length; $i++) {
        if ($text[$i] -eq '{') { $depth++ }
        elseif ($text[$i] -eq '}') {
            $depth--
            if ($depth -eq 0) { $end = $i; break }
        }
    }
    if ($end -lt 0) { Write-Host 'FAIL: boot config braces never close'; exit 1 }
    $bootConfig = $text.Substring($start, $end - $start + 1) | ConvertFrom-Json

    # 2. Collect the boot resources.
    $entries = @()
    foreach ($sectionName in $bootSections) {
        $section = $bootConfig.resources.PSObject.Properties[$sectionName]
        if ($null -eq $section) { Write-Host "FAIL: boot config carries no '$sectionName' section"; exit 1 }
        foreach ($item in @($section.Value)) {
            if (-not $item.PSObject.Properties['name'] -or -not $item.PSObject.Properties['hash']) { continue }
            if ($item.name -match '[\\/]' -or $item.name -in @('.', '..')) { Write-Host "FAIL: resource name '$($item.name)' is not a plain file name"; exit 1 }
            $entries += [pscustomobject]@{ Name = $item.name; Hash = ($item.hash -replace '^sha256-', '') }
        }
    }
    if ($entries.Count -lt 5) { Write-Host "FAIL: $($entries.Count) boot resources found, expected the runtime and the eager assemblies"; exit 1 }

    Write-Host "Boot resources: $($entries.Count) files, $Parallelism fetched at a time."

    # 3. Fetch every boot file in one parallel curl call.
    $curlArgs = @('--parallel', '--parallel-max', $Parallelism, '--fail', '--connect-timeout', '5', '--max-time', '15', '--retry', '2', '--retry-delay', '2', '-sS', '-A', $ua)
    foreach ($entry in $entries) {
        $curlArgs += @('-o', (Join-Path $tmp $entry.Name), "$BaseUrl/_framework/$($entry.Name)")
    }
    & $curl @curlArgs | Out-Null

    # 4. Compare each served file to its manifest digest.
    $failures = 0
    foreach ($entry in $entries) {
        $file = Join-Path $tmp $entry.Name
        if (-not (Test-Path -LiteralPath $file)) { Write-Host "FAIL: $($entry.Name) was not downloaded"; $failures++; continue }
        $bytes = [System.IO.File]::ReadAllBytes($file)
        $actual = [Convert]::ToBase64String([System.Security.Cryptography.SHA256]::HashData($bytes))
        if ($actual -ne $entry.Hash) {
            Write-Host "FAIL: $($entry.Name)"
            Write-Host "  manifest: sha256-$($entry.Hash)"
            Write-Host "  served  : sha256-$actual"
            $failures++
        }
    }

    if ($failures -gt 0) {
        Write-Host "RESULT: FAIL - $failures of $($entries.Count) boot resources mismatched."
        Write-Host 'A visitor cannot boot from a missing or mismatched boot file.'
        exit 1
    }
    Write-Host "RESULT: PASS - all $($entries.Count) boot resources match the served files in $([math]::Round($watch.Elapsed.TotalSeconds, 1))s."
    exit 0
}
finally {
    Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue
}
