---
title: Fix Missing Frameworks Property in Package Update Script
date: 2026-04-06
category: build-errors
module: scripts
problem_type: build_error
component: tooling
severity: medium
symptoms:
  - Script crashes when processing projects without 'frameworks' property in JSON
root_cause: logic_error
resolution_type: code_fix
tags:
  - powershell-script
  - json-parsing
  - dotnet-list-package
---

# Fix Missing Frameworks Property in Package Update Script

## Problem

The `Update-PackageVersions.ps1` script fails when parsing JSON output from `dotnet list package --format json`, specifically when trying to access the `frameworks` property on project objects that do not have this property defined.

## Symptoms

- Script throws a runtime error when iterating through project packages
- Error occurs on projects that lack explicit framework specifications in the package list JSON

## What Didn't Work

- Assuming all project objects in the JSON array would have a `frameworks` property
- Direct property access without existence checks

## Solution

Add a property existence check before accessing the `frameworks` property:

```powershell
if ($project.PSObject.Properties.Name -contains 'frameworks') {
    foreach ($framework in $project.frameworks) {
        # Process frameworks
    }
}
```

## Why This Works

The `dotnet list package --format json` command outputs project objects where the `frameworks` property is optional and only present when the project has multi-targeting or specific framework configurations. By checking for property existence using `PSObject.Properties.Name -contains 'frameworks'`, the script avoids attempting to access undefined properties, preventing the runtime error.

## Prevention

- Always validate property existence when parsing dynamic JSON structures in PowerShell
- Use defensive programming techniques for optional JSON fields
- Consider adding unit tests for script parsing logic

## Related Issues

- Related PowerShell parsing errors in commit scripts (see developer-experience docs)
- Package management and JSON property handling in integration issues
