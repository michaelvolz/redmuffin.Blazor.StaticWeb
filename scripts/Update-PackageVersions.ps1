#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Update-PackageVersions.ps1 — report CPM-safe package updates.

.DESCRIPTION
    Reads the root global.json, lists outdated packages with the .NET CLI, and
    prints a minimal update report for Directory.Packages.props.

.PARAMETER Json
    Emit JSON instead of human-readable output.

.PARAMETER PackageId
    Optional package ID to filter the report.

.PARAMETER PackageListJson
    Optional JSON payload for tests or offline use.
#>

[CmdletBinding()]
param(
    [switch]$Json,
    [string]$PackageId,
    [string]$RepositoryRoot,
    [string]$TargetPath,
    [string]$PackageListJson
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Get-RepoRoot {
    param([string]$OverrideRoot)

    if ($OverrideRoot) {
        return (Resolve-Path $OverrideRoot).Path
    }

    return (Get-Item $PSScriptRoot).Parent.FullName
}

function Read-RootSdkVersion {
    param([string]$RepoRoot)

    $json = Get-Content (Join-Path $RepoRoot 'global.json') -Raw | ConvertFrom-Json
    return [string]$json.sdk.version
}

function Get-MajorVersion {
    param([string]$Version)

    $major = 0
    if ($Version -match '^(\d+)') {
        [void][int]::TryParse($Matches[1], [ref]$major)
    }

    return $major
}

function Read-PackageList {
    param(
        [string]$Target,
        [string]$JsonInput
    )

    if ($JsonInput) {
        return $JsonInput | ConvertFrom-Json
    }

    $args = @('list')
    if ($Target) { $args += $Target }
    $args += @('package', '--outdated', '--highest-minor', '--format', 'json')

    $raw = & dotnet @args
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to list outdated packages.'
    }

    return $raw | ConvertFrom-Json
}

function Get-ReportItems {
    param($PackageList, [string]$PackageFilter)

    $packages = @{}

    foreach ($project in $PackageList.projects) {
        foreach ($framework in $project.frameworks) {
            foreach ($package in $framework.topLevelPackages) {
                if ($package.latestVersion -eq 'Not found at the sources') { continue }
                if ($PackageFilter -and $package.id -ne $PackageFilter) { continue }

                if ($packages.ContainsKey($package.id)) {
                    $current = $packages[$package.id]
                    if ($current.LatestVersion -ne $package.latestVersion) {
                        throw "Package '$($package.id)' has conflicting latest versions: '$($current.LatestVersion)' and '$($package.latestVersion)'."
                    }
                    continue
                }

                $packages[$package.id] = [pscustomobject]@{
                    Id = $package.id
                    RequestedVersion = $package.requestedVersion
                    ResolvedVersion = $package.resolvedVersion
                    LatestVersion = $package.latestVersion
                    File = $project.path
                    Framework = $framework.framework
                }
            }
        }
    }

    return @($packages.Values | Sort-Object Id)
}

$repoRoot = Get-RepoRoot -OverrideRoot $RepositoryRoot
$sdkVersion = Read-RootSdkVersion -RepoRoot $repoRoot
$sdkMajor = Get-MajorVersion -Version $sdkVersion

if (-not $sdkVersion.StartsWith('9.')) {
    throw "Expected root global.json to pin .NET 9, found '$sdkVersion'."
}

$packageList = Read-PackageList -Target $TargetPath -JsonInput $PackageListJson
$items = Get-ReportItems -PackageList $packageList -PackageFilter $PackageId

if (-not $items -or $items.Count -eq 0) {
    Write-Output 'No outdated packages found.'
    exit 0
}

if ($Json) {
    $items | ConvertTo-Json -Depth 5 | Write-Output
    exit 0
}

Write-Output 'Outdated packages:'
foreach ($item in $items) {
    if ((Get-MajorVersion -Version $item.LatestVersion) -gt $sdkMajor) {
        continue
    }

    Write-Output "$($item.Id) $($item.ResolvedVersion) -> $($item.LatestVersion)"
}
