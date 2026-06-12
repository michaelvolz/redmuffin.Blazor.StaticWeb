---
title: "Stale Pre-Compressed HTML Served via Content Negotiation"
date: 2026-06-12
category: docs/solutions/workflow-issues/
module: azure-swa-publish-pipeline
problem_type: workflow_issue
component: tooling
severity: high
applies_when:
  - 'publishing Blazor WASM to Azure Static Web Apps with custom `AfterTargets="Publish"` MSBuild targets that modify `index.html`'
  - "using Brotli or Gzip pre-compression and the modification runs after compression"
  - "any CI step that modifies published files in wwwroot after dotnet publish"
tags:
  - msbuild
  - brotli
  - gzip
  - azure-static-web-apps
  - pre-compressed-files
  - content-negotiation
  - css-fingerprinting
  - publish-pipeline
  - accept-encoding
---

# Stale Pre-Compressed HTML Served via Content Negotiation

## Context

Azure Static Web Apps (SWA) serves pre-compressed files (`.br`, `.gz`)
via HTTP content negotiation when the client sends `Accept-Encoding: br`
or `Accept-Encoding: gzip`. MSBuild generates `index.html.br` and
`index.html.gz` during publish — but generates them BEFORE custom
`AfterTargets="Publish"` MSBuild targets run. Any custom target that
modifies HTML after compression leaves behind stale compressed copies.

Browsers requesting `Accept-Encoding: br` receive the stale
`index.html.br` with original unfingerprinted CSS references, causing
404s invisible to `PowerShell HttpClient` or other tools that omit
`Accept-Encoding`. The discrepancy between clients is the diagnostic
signal: same URL, same deploy, different HTTP clients serving
different content.

## Guidance

Delete or regenerate pre-compressed files after any custom MSBuild
target that modifies published content.

**MSBuild target** (`.csproj`): Use a `RoslynCodeTaskFactory` inline
task to delete stale `.br` and `.gz` files after modification.
`System.IO.Compression.BrotliStream` cannot be used directly —
`RoslynCodeTaskFactory` cannot resolve the assembly by simple name
reference. The SWA platform compresses on-the-fly for local publish
testing.

```xml
<UsingTask TaskName="RegenerateCompressedHtml"
           TaskFactory="RoslynCodeTaskFactory"
           AssemblyFile="$(MSBuildToolsPath)\Microsoft.Build.Tasks.Core.dll">
  <ParameterGroup>
    <InputFile ParameterType="System.String" Required="true" />
  </ParameterGroup>
  <Task>
    <Using Namespace="System.IO" />
    <Code Type="Fragment" Language="cs">
    <![CDATA[
      string brPath = InputFile + ".br";
      string gzPath = InputFile + ".gz";
      if (File.Exists(brPath)) File.Delete(brPath);
      if (File.Exists(gzPath)) File.Delete(gzPath);
    ]]>
    </Code>
  </Task>
</UsingTask>
```

Invocation inside the `AfterTargets="Publish"` target:

```xml
<RegenerateCompressedHtml InputFile="$(IndexHtmlPath)"
                          Condition="'$(CssHash)' != ''" />
```

**CI workflow**: Add `brotli` and `gzip` CLI commands after the `sed`
step that rewrites HTML references. Both tools are pre-installed on
`ubuntu-latest` runners (`brotli 1.1.0-2build2`).

```bash
INDEX="path/to/publish/wwwroot/index.html"
gzip -c -9 "$INDEX" > "${INDEX}.gz"
brotli --best -o "${INDEX}.br" "$INDEX"
```

## Why This Matters

Azure SWA's content negotiation serves pre-compressed files atomically.
There is no way to modify a compressed file after creation without
regenerating it. Any custom MSBuild target that modifies files after
`AfterTargets="Publish"` (CSS fingerprinting, import map rewriting,
asset path transformation) produces correct uncompressed HTML but stale
compressed variants.

The resulting 404s are silent and client-specific: browsers sending
`Accept-Encoding: br` break while headless HTTP clients work fine,
making the bug extremely difficult to detect through conventional
testing. This quirk applies to EVERY custom MSBuild target — not just
CSS fingerprinting.

## When to Apply

- Any `AfterTargets="Publish"` (or later) target that modifies
  `.html`, `.json`, `.js`, `.xml`, or `.svg` files during publish
- CSS fingerprinting or asset cache-busting that rewrites `<link>`
  or `<script>` references
- Import map rewriting (`OverrideHtmlAssetPlaceholders` or custom
  equivalents)
- JavaScript bundle renaming or path transformation
- Any CI step that modifies files in the published `wwwroot` after
  `dotnet publish`
- When observing a discrepancy between browser behavior and
  PowerShell/curl against the same deployed URL

## Examples

**Before (broken):**

```xml
<!-- csproj: CSS fingerprinting modifies index.html, but .br/.gz are stale -->
<Target Name="FingerprintCustomCssAssets" AfterTargets="Publish">
  <ReplaceFileText FilePath="$(IndexHtmlPath)"
                   FindText="css/app.min.css"
                   ReplaceWith="css/app.min.$(CssHash8).css" />
  <!-- No step to update compressed files -->
</Target>
```

Result: `index.html` references `app.min.B2A88184.css` (correct).
`index.html.br` references `app.min.css` (stale). Browsers sending
`Accept-Encoding: br` get the `.br` file → 404 on CSS.

**After (fixed):**

```xml
<Target Name="FingerprintCustomCssAssets" AfterTargets="Publish">
  <ReplaceFileText FilePath="$(IndexHtmlPath)"
                   FindText="css/app.min.css"
                   ReplaceWith="css/app.min.$(CssHash8).css" />
  <RegenerateCompressedHtml InputFile="$(IndexHtmlPath)"
                            Condition="'$(CssHash)' != ''" />
</Target>
```

Result: Stale `.br`/`.gz` deleted. SWA compresses on-the-fly for
local dev. In CI, the workflow regenerates both compressed files from
the modified `index.html` after the `sed` step.

## Related

- `docs/solutions/workflow-issues/brotli-compression-not-reaching-azure-swa-production.md`
