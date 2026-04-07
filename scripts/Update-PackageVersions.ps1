#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Update-PackageVersions.ps1 — report or apply CPM-safe package updates.

.DESCRIPTION
    Reads the root global.json, lists outdated packages with the .NET CLI, and
    can apply the version updates directly to Directory.Packages.props.

.PARAMETER Apply
    Apply the selected package version changes to Directory.Packages.props.

.PARAMETER Json
    Emit JSON instead of human-readable output.

.PARAMETER PackageId
    Optional package ID to filter the report.

.PARAMETER PackageListJson
    Optional JSON payload for tests or offline use.
#>

[CmdletBinding()]
param(
    [switch]$Apply,
    [switch]$Json,
    [string]$PackageId,
    [string]$RepositoryRoot,
    [string]$TargetPath,
    [string]$PackageListJson
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ($Apply -and $Json) {
    throw 'Apply and Json cannot be combined.'
}

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
    $args += @('package', '--outdated', '--format', 'json')

    # Use explicit process to ensure UTF-8 encoding is preserved
    $processInfo = New-Object System.Diagnostics.ProcessStartInfo
    $processInfo.FileName = 'dotnet'
    $processInfo.Arguments = $args -join ' '
    $processInfo.RedirectStandardOutput = $true
    $processInfo.RedirectStandardError = $true
    $processInfo.UseShellExecute = $false
    $processInfo.CreateNoWindow = $true
    $processInfo.StandardOutputEncoding = [System.Text.Encoding]::UTF8

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $processInfo
    $process.Start() | Out-Null
    $stdout = $process.StandardOutput.ReadToEnd()
    $process.WaitForExit()

    if ($process.ExitCode -ne 0) {
        throw 'Failed to list outdated packages.'
    }

    return $stdout | ConvertFrom-Json
}

# Known package incompatibilities - packages that should not be upgraded past certain versions
# due to breaking changes or known incompatibilities with other dependencies
function Get-IncompatiblePackageConstraints {
    return @{
        'Microsoft.ApplicationInsights.WorkerService' = @{
            MaxVersion = '2.23.0'
            Reason = 'Version 3.x+ is incompatible with Microsoft.Azure.Functions.Worker.ApplicationInsights 2.x (ITelemetryInitializer breaking change)'
        }
    }
}

function Test-PackageCompatibility {
    param(
        [string]$PackageId,
        [string]$LatestVersion,
        [string]$CurrentVersion
    )

    $constraints = Get-IncompatiblePackageConstraints

    if (-not $constraints.ContainsKey($PackageId)) {
        return @{
            IsCompatible = $true
            Skip = $false
            Reason = $null
        }
    }

    $constraint = $constraints[$PackageId]
    $maxVersion = $constraint.MaxVersion

    # Parse versions for comparison
    $latestParts = $LatestVersion -split '\.'
    $maxParts = $maxVersion -split '\.'

    $isNewerThanMax = $false
    for ($i = 0; $i -lt [Math]::Min($latestParts.Count, $maxParts.Count); $i++) {
        $latestPart = [int]$latestParts[$i]
        $maxPart = [int]$maxParts[$i]
        if ($latestPart -gt $maxPart) {
            $isNewerThanMax = $true
            break
        }
        if ($latestPart -lt $maxPart) {
            break
        }
    }

    if ($isNewerThanMax) {
        return @{
            IsCompatible = $false
            Skip = $true
            Reason = "SKIPPED: $LatestVersion exceeds max compatible version $maxVersion - $($constraint.Reason)"
        }
    }

    return @{
        IsCompatible = $true
        Skip = $false
        Reason = $null
    }
}

function Get-ReportItems {
    param(
        $PackageList,
        [string]$PackageFilter,
        [int]$SdkMajor
    )

    $packages = @{}

    foreach ($project in $PackageList.projects) {
        if ($project.PSObject.Properties.Name -contains 'frameworks' -and $project.frameworks) {
            foreach ($framework in $project.frameworks) {
                foreach ($package in $framework.topLevelPackages) {
                    if ($package.latestVersion -eq 'Not found at the sources') { continue }
                    if ($PackageFilter -and $package.id -ne $PackageFilter) { continue }
                    if ((Get-MajorVersion -Version $package.latestVersion) -gt $SdkMajor) { continue }

                    # Check for known package incompatibilities
                    $compatibility = Test-PackageCompatibility -PackageId $package.id -LatestVersion $package.latestVersion -CurrentVersion $package.resolvedVersion
                    if ($compatibility.Skip) {
                        Write-Host "  [SKIP] $($package.id) $($package.latestVersion) - blocked by compatibility constraint"
                        continue
                    }

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
    }

    return @($packages.Values | Sort-Object Id)
}

function Load-XmlDocument {
    param([string]$Path)

    $document = [System.Xml.XmlDocument]::new()
    $document.PreserveWhitespace = $true
    $document.Load($Path)

    return $document
}

function Get-PackageVersionNodes {
    param(
        [System.Xml.XmlDocument]$Document,
        [string]$PackageId
    )

    return @($Document.SelectNodes("//PackageVersion[@Include='$PackageId' or @Update='$PackageId']"))
}

function Resolve-PackageUpdateTarget {
    param(
        [System.Xml.XmlDocument]$Document,
        $Item
    )

    $nodes = Get-PackageVersionNodes -Document $Document -PackageId $Item.Id
    if (-not $nodes -or $nodes.Count -eq 0) {
        throw "Package '$($Item.Id)' was reported as outdated, but no CPM entry was found in Directory.Packages.props."
    }

    $includeNodes = @($nodes | Where-Object { $_.HasAttribute('Include') })
    $candidateNodes = if ($includeNodes.Count -gt 0) { $includeNodes } else { $nodes }

    $propertyNames = @()
    foreach ($node in $candidateNodes) {
        $version = [string]$node.GetAttribute('Version')
        if ($version -match '^\$\(([^)]+)\)$') {
            $propertyNames += $Matches[1]
        }
    }

    $uniqueProperties = @($propertyNames | Sort-Object -Unique)
    if ($uniqueProperties.Count -gt 1) {
        throw "Package '$($Item.Id)' maps to multiple shared properties: $($uniqueProperties -join ', ')."
    }

    if ($uniqueProperties.Count -eq 1) {
        return [pscustomobject]@{
            Kind = 'Property'
            Name = $uniqueProperties[0]
            PackageId = $Item.Id
            LatestVersion = $Item.LatestVersion
        }
    }

    $selectedNode = $includeNodes | Select-Object -First 1
    if (-not $selectedNode) {
        $selectedNode = @($nodes | Where-Object { $_.HasAttribute('Update') }) | Select-Object -First 1
    }

    if (-not $selectedNode) {
        throw "Package '$($Item.Id)' could not be mapped to a CPM entry."
    }

    return [pscustomobject]@{
        Kind = 'Inline'
        Node = $selectedNode
        PackageId = $Item.Id
        LatestVersion = $Item.LatestVersion
    }
}

function Get-PackageUpdateTargets {
    param(
        [System.Xml.XmlDocument]$Document,
        [object[]]$Items
    )

    $targets = @{}

    foreach ($item in $Items) {
        $target = Resolve-PackageUpdateTarget -Document $Document -Item $item
        $key = if ($target.Kind -eq 'Property') { "Property::$($target.Name)" } else { "Package::$($target.PackageId)" }

        if ($targets.ContainsKey($key)) {
            if ($targets[$key].LatestVersion -ne $target.LatestVersion) {
                throw "Package target '$key' has conflicting latest versions: '$($targets[$key].LatestVersion)' and '$($target.LatestVersion)'."
            }

            continue
        }

        $targets[$key] = $target
    }

    return @($targets.Values)
}

function Save-XmlDocumentAtomically {
    param(
        [System.Xml.XmlDocument]$Document,
        [string]$Path
    )

    $directory = Split-Path $Path -Parent
    $tempPath = Join-Path $directory ([System.IO.Path]::GetRandomFileName() + '.tmp')
    $settings = [System.Xml.XmlWriterSettings]::new()
    $settings.Encoding = [System.Text.UTF8Encoding]::new($true)
    $settings.Indent = $false
    $settings.NewLineChars = "`r`n"
    $settings.NewLineHandling = [System.Xml.NewLineHandling]::Replace
    $settings.OmitXmlDeclaration = $false

    $writer = $null
    try {
        $writer = [System.Xml.XmlWriter]::Create($tempPath, $settings)
        $Document.Save($writer)
        $writer.Flush()
    }
    finally {
        if ($writer) {
            $writer.Dispose()
        }
    }

    Move-Item -Path $tempPath -Destination $Path -Force
}

function Apply-PackageUpdates {
    param(
        [string]$Path,
        [System.Xml.XmlDocument]$Document,
        [object[]]$Items
    )

    $targets = Get-PackageUpdateTargets -Document $Document -Items $Items
    if (-not $targets -or $targets.Count -eq 0) {
        return $false
    }

    $changed = $false

    foreach ($target in $targets) {
        if ($target.Kind -eq 'Property') {
            $propertyNodes = @($Document.SelectNodes("//PropertyGroup/$($target.Name)"))
            if (-not $propertyNodes -or $propertyNodes.Count -eq 0) {
                throw "Shared property '$($target.Name)' was not found in Directory.Packages.props."
            }

            foreach ($propertyNode in $propertyNodes) {
                if ([string]$propertyNode.InnerText -ne $target.LatestVersion) {
                    $propertyNode.InnerText = $target.LatestVersion
                    $changed = $true
                }
            }

            continue
        }

        $currentVersion = [string]$target.Node.GetAttribute('Version')
        if ($currentVersion -ne $target.LatestVersion) {
            $target.Node.SetAttribute('Version', $target.LatestVersion)
            $changed = $true
        }
    }

    if ($changed) {
        Save-XmlDocumentAtomically -Document $Document -Path $Path
    }

    return $changed
}

$repoRoot = Get-RepoRoot -OverrideRoot $RepositoryRoot
$sdkVersion = Read-RootSdkVersion -RepoRoot $repoRoot
$sdkMajor = Get-MajorVersion -Version $sdkVersion

if (-not $sdkVersion.StartsWith('9.')) {
    throw "Expected root global.json to pin .NET 9, found '$sdkVersion'."
}

$packageList = Read-PackageList -Target $TargetPath -JsonInput $PackageListJson
$items = Get-ReportItems -PackageList $packageList -PackageFilter $PackageId -SdkMajor $sdkMajor

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
    Write-Output "$($item.Id) $($item.ResolvedVersion) -> $($item.LatestVersion)"
}

if ($Apply) {
    $propsPath = Join-Path $repoRoot 'Directory.Packages.props'
    $propsDocument = Load-XmlDocument -Path $propsPath
    if (Apply-PackageUpdates -Path $propsPath -Document $propsDocument -Items $items) {
        Write-Output 'Applied package updates to Directory.Packages.props.'
    }
}
