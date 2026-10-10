---
title: "Extract TUnit durations from the gzip-encoded HTML report"
date: 2026-10-10
category: tooling-decisions
module: testing
problem_type: tooling_decision
component: tooling
severity: low
applies_when:
  - "Reading test durations or statuses out of a TUnit HTML report"
  - "A script that parses TUnit report data starts returning nothing or stale rows"
  - "A TUnit upgrade changes the report's data script tag or payload encoding"
retire_when: "TUnit changes the report embedding again. Generate a report (`dotnet run --project tests/<project> -- --report-html`), open the HTML, and check whether the script tag id and payload encoding still match this document. Re-verify before reusing the parse snippet."
tags:
  - tunit
  - testing
  - tooling
  - reporting
---

# Extract TUnit durations from the gzip-encoded HTML report

## Context

`scripts/Extract-TestDurations.ps1` read the TUnit HTML report and produced an
empty or stale table after the TUnit upgrade this session ran. The script
assumed the old report layout: plain JSON inside `<script id="test-data"
type="application/json">`. Current TUnit reports (verified on TUnit 1.53.0)
carry a different tag and a different payload.

## Guidance

Current TUnit HTML reports embed the result data as a gzip-compressed,
base64-encoded JSON string inside this tag:

```html
<script id="report-data" type="application/octet-stream">
```

The placeholder output shows only the tag above and the note: the JSON no
longer sits in plain text in the report. The JSON payload is a gzip stream
that the script base64-decompresses before parsing. Two changes matter:

1. The script tag id is `report-data` with type `application/octet-stream`.
   The older tag used id `test-data` with plain embedded JSON.
2. The decoder must gunzip the base64 payload before `ConvertFrom-Json`.

The decoder that worked this session:

```powershell
$b64 = ($html -split '<script id="report-data" type="application/octet-stream">')[1] -split '</script>'
$bytes = [Convert]::FromBase64String($b64[0].Trim())
$gzip = [IO.Compression.GzipStream]::new([IO.MemoryStream]::new($bytes), [IO.Compression.CompressionMode]::Decompress)
$json = ([IO.StreamReader]::new($gzip)).ReadToEnd()
$data = $json | ConvertFrom-Json
```

The JSON shape is a flat top-level array under the `tests` key. Each entry
carries `id`, `name`, `cls`, `ns`, `status`, `start`, `duration`, `worker`,
`categories`, and `source`. A duration table maps `"$($test.ns).$($test.cls)"`
to the class name and `$test.duration` to milliseconds.

Two report-location facts drive the script's input path:

- The report file name embeds the target framework:
  `redmuffin.Blazor.StaticWeb.Tests-windows-net10.0-report.html`.
- TUnit writes the report next to the built test assembly
  (`tests/<project>/bin/Debug/net10.0/TestResults/`) by default;
  `--results-directory` overrides the location.

Any hardcoded report path in the script can therefore drift out of sync with
the file name or with a results-directory override, and the parse then reads a
stale or missing file.

## Why This Matters

A duration script that silently parses an old layout produces no error: it
returns a table of zero rows or rows from a stale report. Test-duration work
performed on that table points at tests that have since been renamed or
deleted. The gzip step is not visible in the HTML on casual reading, because
the tag hides a base64 blob, so an editor who greps the report for JSON never
finds the payload and concludes the data is gone.

## When to Apply

- Writing or repairing any script that consumes TUnit HTML report data
- A TUnit upgrade leaves a report-parsing script returning nothing
- Building Duration, CRAP, or other gates on top of test-timing data

## Examples

**Before — reads a plain-JSON tag and a guessed file name:**

```powershell
$html = Get-Content "$repo\TestResults\report.html" -Raw
$data = ($html | ConvertFrom-Json) # old layout: id "test-data", plain JSON
```

**After — locate the `report-data` tag, base64-decode, gunzip, parse:**

```powershell
$html = Get-Content <current-report-path> -Raw
$b64 = ($html -split '<script id="report-data" type="application/octet-stream">')[1] -split '</script>'
$bytes = [Convert]::FromBase64String($b64[0].Trim())
$gzip = [IO.Compression.GzipStream]::new([IO.MemoryStream]::new($bytes), [IO.Compression.CompressionMode]::Decompress)
$data = (([IO.StreamReader]::new($gzip)).ReadToEnd() | ConvertFrom-Json).tests
```

Regenerate the CSV after any test rename or deletion, so the duration table
names only tests that still exist.

## Related

- `scripts/Extract-TestDurations.ps1` — the repaired extractor
- `scripts/test-durations.csv` — regenerated output table
