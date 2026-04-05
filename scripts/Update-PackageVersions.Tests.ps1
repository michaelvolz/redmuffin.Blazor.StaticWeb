Describe 'Update-PackageVersions.ps1' {
    BeforeEach {
        $script:ScriptPath = Join-Path $PSScriptRoot 'Update-PackageVersions.ps1'
        $script:RepoRoot = Join-Path $TestDrive 'repo'
        New-Item -ItemType Directory -Path $script:RepoRoot -Force | Out-Null

        @'
{
  "sdk": {
    "version": "9.0.305",
    "rollForward": "latestMinor"
  }
}
'@ | Set-Content -Path (Join-Path $script:RepoRoot 'global.json') -Encoding UTF8

        @'
<?xml version="1.0" encoding="utf-8"?>
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <PropertyGroup Label="SharedVersions">
    <MicrosoftExtensionsVersion>9.0.14</MicrosoftExtensionsVersion>
    <TUnitVersion>1.28.0</TUnitVersion>
  </PropertyGroup>
  <ItemGroup Label="MicrosoftExtensions">
    <PackageVersion Include="Microsoft.Extensions.Logging" Version="$(MicrosoftExtensionsVersion)" />
  </ItemGroup>
  <ItemGroup Label="Testing">
    <PackageVersion Include="TUnit" Version="$(TUnitVersion)" />
    <PackageVersion Include="LightMock.Generator" Version="1.2.3" />
  </ItemGroup>
  <ItemGroup Label="Analyzers">
    <PackageVersion Include="Microsoft.AspNetCore.Components.Analyzers" Version="$(MicrosoftExtensionsVersion)" />
    <PackageVersion Update="Microsoft.AspNetCore.Components.Analyzers" Version="$(MicrosoftExtensionsVersion)" />
  </ItemGroup>
</Project>
'@ | Set-Content -Path (Join-Path $script:RepoRoot 'Directory.Packages.props') -Encoding UTF8
    }

    function New-PackageListJson {
        @'
{
  "version": 1,
  "parameters": "--outdated",
  "sources": ["https://api.nuget.org/v3/index.json"],
  "projects": [
    {
      "path": "src/app/app.csproj",
      "frameworks": [
            {
              "framework": "net9.0",
              "topLevelPackages": [
                { "id": "Microsoft.Extensions.Logging", "requestedVersion": "9.0.14", "resolvedVersion": "9.0.14", "latestVersion": "9.0.15" },
                { "id": "TUnit", "requestedVersion": "1.22.3", "resolvedVersion": "1.22.3", "latestVersion": "1.28.0" },
                { "id": "LightMock.Generator", "requestedVersion": "1.2.3", "resolvedVersion": "1.2.3", "latestVersion": "1.2.4" },
                { "id": "Microsoft.AspNetCore.Components.Analyzers", "requestedVersion": "9.0.14", "resolvedVersion": "9.0.14", "latestVersion": "10.0.5" }
              ]
            }
          ]
        },
    {
      "path": "tests/app.tests/app.tests.csproj",
      "frameworks": [
        {
          "framework": "net9.0",
          "topLevelPackages": [
            { "id": "Microsoft.Extensions.Logging", "requestedVersion": "9.0.14", "resolvedVersion": "9.0.14", "latestVersion": "9.0.15" }
          ]
        }
      ]
    }
  ]
}
'@
    }

    It 'lists deduped outdated packages' {
        $output = & $script:ScriptPath -RepositoryRoot $script:RepoRoot -Json:$false -PackageListJson (New-PackageListJson) 2>&1
        $text = $output -join "`n"

        if ($text -notmatch 'Outdated packages:') { throw 'Expected the report header.' }
        if ($text -notmatch 'Microsoft\.Extensions\.Logging 9\.0\.14 -> 9\.0\.15') { throw 'Expected Microsoft.Extensions.Logging to be reported.' }
        if ($text -notmatch 'TUnit 1\.22\.3 -> 1\.28\.0') { throw 'Expected TUnit to be reported.' }
        if ($text -match 'Microsoft\.AspNetCore\.Components\.Analyzers') { throw 'Expected .NET 10 drift to be excluded.' }
    }

    It 'returns json when requested' {
        $output = & $script:ScriptPath -RepositoryRoot $script:RepoRoot -Json -PackageListJson (New-PackageListJson) 2>&1
        $joined = $output -join "`n"

        if ($joined -notmatch 'Microsoft\.Extensions\.Logging') { throw 'Expected Microsoft.Extensions.Logging in JSON output.' }
        if ($joined -notmatch 'TUnit') { throw 'Expected TUnit in JSON output.' }
    }

    It 'filters by package id' {
        $output = & $script:ScriptPath -RepositoryRoot $script:RepoRoot -PackageId 'TUnit' -PackageListJson (New-PackageListJson) 2>&1
        $text = $output -join "`n"

        if ($text -notmatch 'TUnit 1\.22\.3 -> 1\.28\.0') { throw 'Expected TUnit only.' }
        if ($text -match 'Microsoft\.Extensions\.Logging') { throw 'Expected Microsoft.Extensions.Logging to be filtered out.' }
    }

    It 'applies package updates when requested' {
        $before = Get-Content -Raw (Join-Path $script:RepoRoot 'Directory.Packages.props')

        $output = & $script:ScriptPath -RepositoryRoot $script:RepoRoot -Apply -PackageListJson (New-PackageListJson) 2>&1
        $text = $output -join "`n"
        $after = Get-Content -Raw (Join-Path $script:RepoRoot 'Directory.Packages.props')

        if ($text -notmatch 'Applied package updates to Directory.Packages.props\.') { throw 'Expected apply confirmation.' }
        if ($after -notmatch '<MicrosoftExtensionsVersion>9\.0\.15</MicrosoftExtensionsVersion>') { throw 'Expected the shared property to update.' }
        if ($after -notmatch '<PackageVersion Include="LightMock\.Generator" Version="1\.2\.4" />') { throw 'Expected the literal package version to update.' }
        if ($after -notmatch '<PackageVersion Update="Microsoft\.AspNetCore\.Components\.Analyzers" Version="\$\(MicrosoftExtensionsVersion\)" />') { throw 'Expected the override entry to remain intact.' }
        if ($before -eq $after) { throw 'Expected the file to change.' }
    }

    It 'leaves the props file unchanged in report mode' {
        $before = Get-Content -Raw (Join-Path $script:RepoRoot 'Directory.Packages.props')

        $output = & $script:ScriptPath -RepositoryRoot $script:RepoRoot -PackageListJson (New-PackageListJson) 2>&1
        $text = $output -join "`n"
        $after = Get-Content -Raw (Join-Path $script:RepoRoot 'Directory.Packages.props')

        if ($text -match 'Applied package updates') { throw 'Expected report mode to avoid apply output.' }
        if ($before -ne $after) { throw 'Expected report mode to leave the file unchanged.' }
    }

    It 'rejects apply and json together' {
        $threw = $false
        try {
            & $script:ScriptPath -RepositoryRoot $script:RepoRoot -Apply -Json -PackageListJson (New-PackageListJson) | Out-Null
        }
        catch {
            $threw = $true
        }

        if (-not $threw) { throw 'Expected -Apply and -Json together to fail.' }
    }

    It 'fails cleanly when the root SDK is not 9' {
        @'
{
  "sdk": {
    "version": "8.0.100",
    "rollForward": "latestMinor"
  }
}
'@ | Set-Content -Path (Join-Path $script:RepoRoot 'global.json') -Encoding UTF8

        $threw = $false
        try {
            & $script:ScriptPath -RepositoryRoot $script:RepoRoot -PackageListJson (New-PackageListJson) | Out-Null
        }
        catch {
            $threw = $true
        }

        if (-not $threw) { throw 'Expected the script to fail for a non-9 root SDK.' }
    }

    It 'fails when the package list contains conflicting latest versions' {
        $conflict = @'
{
  "version": 1,
  "projects": [
    {
      "path": "src/app/app.csproj",
      "frameworks": [
        {
          "framework": "net9.0",
          "topLevelPackages": [
            { "id": "TUnit", "requestedVersion": "1.22.3", "resolvedVersion": "1.22.3", "latestVersion": "1.28.0" }
          ]
        }
      ]
    },
    {
      "path": "tests/app.tests/app.tests.csproj",
      "frameworks": [
        {
          "framework": "net9.0",
          "topLevelPackages": [
            { "id": "TUnit", "requestedVersion": "1.22.3", "resolvedVersion": "1.22.3", "latestVersion": "1.29.0" }
          ]
        }
      ]
    }
  ]
}
'@

        $threw = $false
        try {
            & $script:ScriptPath -RepositoryRoot $script:RepoRoot -PackageListJson $conflict | Out-Null
        }
        catch {
            $threw = $true
        }

        if (-not $threw) { throw 'Expected conflicting latest versions to fail.' }
    }
}
