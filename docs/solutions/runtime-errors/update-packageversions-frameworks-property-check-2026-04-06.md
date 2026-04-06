---
title: Update-PackageVersions.ps1 frameworks property check
date: 2026-04-06
module: scripts/Update-PackageVersions.ps1
tags: [powershell, json-parsing, property-access, runtime-error]
problem_type: runtime-error
---

## Problem

`Update-PackageVersions.ps1` script fails when processing projects without a 'frameworks' property in the JSON output from `dotnet list package --outdated --format json`.

## Symptoms

Script throws an error accessing non-existent property when iterating through project JSON objects that lack the 'frameworks' property.

## Root Cause

The script assumed all project objects in the JSON output would contain a 'frameworks' property, but some projects may not have this property, causing a runtime error when attempting to access `$project.frameworks`.

## What Didn't Work

Assuming all projects have 'frameworks' property - this assumption breaks when processing JSON output from projects that don't define target frameworks in a way that creates the 'frameworks' property.

## Solution

Add a property existence check before accessing the 'frameworks' property:

```powershell
if ($project.PSObject.Properties.Name -contains 'frameworks' -and $project.frameworks) {
    # Process frameworks
}
```

## Why This Works

- `$project.PSObject.Properties.Name -contains 'frameworks'` checks if the 'frameworks' property exists on the object
- Only accesses `$project.frameworks` if the property exists, preventing the runtime error
- The `-and $project.frameworks` ensures the property is not null/empty as well

## Prevention

Always check property existence when parsing dynamic JSON structures. Use `PSObject.Properties.Name -contains 'propertyName'` to safely check for property presence before accessing it.

## Files Modified

- `scripts/Update-PackageVersions.ps1` (line ~100 in Get-ReportItems function)
