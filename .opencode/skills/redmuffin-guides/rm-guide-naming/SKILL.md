---
name: rm-guide-naming
description: "Use when creating, renaming, or reviewing C# names for types, members, namespaces, test doubles, or Blazor components. Also covers CLI subcommand naming, CLI flag naming, config file naming, and directory naming. USE FOR: CLI naming, config naming, directory naming."
---

# rm-guide-naming

## CRITICAL

- Use PascalCase for types, namespaces, methods, properties, events, enums.
- Use `camelCase` for locals and parameters.
- Use `_camelCase` for private fields.
- Prefix interfaces with `I`.
- Name test doubles as `[Class]_[Type]` (`Mock`, `Stub`, `Spy`, `Fake`, `Dummy`).
- **Names are not set in stone.** A name is a snapshot of current understanding.
  When working with the code reveals what the name MUST be, change it.
  A better name makes every future reader instantly understand. It is worth
  every reference update, file rename, and test fix required.

## WHEN TO LOAD

- Creating or renaming C# files, classes, records, enums, interfaces, methods.
- Creating or renaming Blazor components (`.razor` / `.razor.cs`).
- Reviewing names in new tests, test doubles, or components.

## GUIDANCE

- Never use names that obscure the type's purpose.
- Avoid abbreviations unless the domain already uses them.
- Match existing repo names exactly when extending a pattern.

## NEVER

- Do not invent new naming schemes inside a feature.
- Do not use Hungarian notation.
- Do not abbreviate CLI flag names, subcommands, or configuration file names.
- Do not use generic UI furniture names for Blazor components (e.g.,
  `Panel`, `Widget`, `Control`, `Viewer`). Name WHAT the component
  displays, not HOW it's displayed.

## Blazor Component Naming

Names are the hardest part. They must answer "what IS this component?"
instantly — like Lego bricks that show their connection points.

### Naming pattern

```
[Scope][Subject][RenderPurpose]
```

| Element       | Meaning               | Example                               |
| ------------- | --------------------- | ------------------------------------- |
| Scope         | Which part of the app | `PageLoad`, `AppStart`, `UserProfile` |
| Subject       | What entity or metric | `Metrics`, `Timing`, `Breakdown`      |
| RenderPurpose | How it renders        | `View`, `Card`, `Bar`, `Chart`        |

**Examples:** `PageLoadMetricsView`, `TimingBreakdownCard`,
`WasmBootstrapCard`, `MetricProgressBar`

### Anti-patterns

| Anti-pattern                     | Problem                                   | Fix                                                             |
| -------------------------------- | ----------------------------------------- | --------------------------------------------------------------- |
| `PageLoadSpeed` / `LoadSpeed`    | Overlapping, no distinction               | Add scope prefix: `PageLoadMetricsView` / `AppStartMetricsView` |
| `Panel` / `Widget` / `Control`   | UI furniture — says nothing about content | Describe the content: `TimingBreakdownCard`                     |
| Acronyms or abbreviations        | Opaque to readers                         | Spell it out: `WasmBootstrapCard` not `WBootCard`               |
| `Helper` / `Utility` / `Manager` | Vague catch-all                           | Name the specific action                                        |

### Copy-paste detection

If two component files have similar names and lack clear scoping
prefixes (e.g., `PageLoadSpeed` and `LoadSpeed`), they are almost
certainly a copy-paste anti-pattern. The fix is NEVER a shared base
class. The fix is decomposition into smaller single-responsibility
components with different compositions.

- No abbreviations: `architecture` not `arch`, `duplicates` not `dupes`.

## Directory & Namespace Structure

Folder names map 1:1 to namespace segments. A file at
`Features/Raindrop/Services/RaindropAPIFactory.cs` has namespace
`redmuffin.Blazor.StaticWeb.Features.Raindrop.Services`.

### Feature folders (top-level)

Every page, domain, and shared construct lives under `Features/`:

| Pattern                     | Example                          | Contains                                                             |
| --------------------------- | -------------------------------- | -------------------------------------------------------------------- |
| `Features/{FeatureName}/`   | `Features/Raindrop/`             | Domain logic: `Services/`, `Models/`, `Cache/`, `Presentation/`      |
| `Features/{PageName}/`      | `Features/HomePage/`             | Single-page feature: `.razor` + `.razor.cs` + optional `Components/` |
| `Features/{PageName}/`      | `Features/DebugPage/`            | Multi-page feature: sub-pages, `Services/`, `Models/`, `Components/` |
| `Features/Common/`          | `Features/Common/Components/`    | Shared reusable components used by 2+ features                       |
| `Features/Common/{Domain}/` | `Features/Common/PageLoadSpeed/` | Cross-cutting domain: `Services/`, `Models/`, `Components/`          |

### Core (app infrastructure)

`Core/` holds application-level infrastructure shared across features
but not feature-specific:

| Folder                   | Purpose                                                           |
| ------------------------ | ----------------------------------------------------------------- |
| `Core/Layout/`           | Layout components (`MainLayout`, `NavMenu`)                       |
| `Core/Services/`         | Cross-cutting services (`WarmupService`, `BrowserStorageService`) |
| `Core/ImagePlaceholder/` | Cross-cutting feature: `Abstractions/`, `Models/`, `Services/`    |
| `Core/Abstractions/`     | Truly app-wide interfaces (`IDelayProvider`)                      |

### NEVER

- Do not nest pages under `Features/Pages/` — the `Pages/` level adds zero signal. Flat: `Features/HomePage/`
- Do not create `Services/` at the project root. Services belong in `Core/Services/` or `Features/{Domain}/Services/`
- Do not create generic `Models/` folders at the root or in `Core/`. Models belong with their consumer

### Namespace Syntax

- Use file-scoped namespaces in all C# files: `namespace A.B.C;`
- Never add block-scoped namespaces in new code.
- Keep namespaces predictable. Match the file's responsibility, not its historical origin.

## Role-Based Naming (Purpose over Value)

When naming constants — especially colors, thresholds, or configuration
values — name them by their **role** (what they represent), NOT by their
**literal value** (what they are):

| ❌ Literal-Value Name | ✅ Role-Based Name              |
| --------------------- | ------------------------------- |
| `Green = "#0cce6b"`   | `LighthouseGood = "#0cce6b"`    |
| `Orange = "#ffc107"`  | `LighthouseWarning = "#ffc107"` |
| `Red = "#fa5252"`     | `LighthousePoor = "#fa5252"`    |
| `Blue = "#4a8fd4"`    | `DiagnosticMuted = "#4a8fd4"`   |

**Why:** LighthouseGood can change from `#0cce6b` to `#18a957` in one
place and every component referencing it updates. The name communicates
"this is the color for a good Google Lighthouse score," which survives
any color palette redesign.

**Pattern used in:** `PageLoadColors.cs` — 8 role-based constants for
the performance diagnostics page. Components reference `PageLoadColors.LighthouseGood`,
never a hex value directly.

**Also applies to thresholds and magic numbers:**

```csharp
// ❌ Literal-value names
private const int MaxAssemblyCount = 70;

// ✅ Role-based names
private const int AssemblyCountGreenThreshold = 70;
```

## QualityGates Asset Naming

All QualityGates configuration files and generated artifacts follow a strict
no-abbreviation convention. The filename must describe exactly what it is
without requiring the reader to open it.

### Directory

- `quality-gates/` — all QualityGates configuration lives in this directory,
  placed at the solution root (main: `REPO_ROOT/quality-gates/`, tools:
  `REPO_ROOT/tools/quality-gates/`). Matches the tool name "QualityGates".

### Configuration Files

| File                       | Purpose                                      |
| -------------------------- | -------------------------------------------- |
| `architecture-rules.yml`   | Component dependency rules                   |
| `exclusions.yml`           | (Future) Methods/files excluded from gates   |
| `quality-gates-config.yml` | (Future) Master config: thresholds, defaults |

### Generated Artifacts

Generated files go to `/tmp/` and are never committed:

| File                     | Purpose                                  |
| ------------------------ | ---------------------------------------- |
| `/tmp/coverage-data.xml` | Cobertura coverage XML for CRAP analysis |

### CLI Subcommands

| Subcommand     | Purpose                                            |
| -------------- | -------------------------------------------------- |
| `crap`         | Complexity Risk Analysis (kept — industry acronym) |
| `scrap`        | Structural Code Analysis (kept — industry acronym) |
| `architecture` | Dependency architecture validation                 |
| `mutation`     | Mutation testing                                   |
| `duplicates`   | Duplicate code detection                           |
| `all`          | Run all gates with defaults                        |

### CLI Flags

| Flag                    | Purpose                          |
| ----------------------- | -------------------------------- |
| `--architecture-config` | Path to `architecture-rules.yml` |
| `--mutation-source`     | Source file for mutation testing |
| `--mutation-scan`       | Scan-only mutation mode          |
| `--duplicates`          | Enable duplicate detection gate  |

### Rationale

- No abbreviations: `architecture` not `arch`, `duplicates` not `dupes`.
- Industry-standard acronyms (CRAP, SCRAP) are preserved — they are more
  recognizable than their expanded forms.
- Generated artifact names describe the data, not the tool that produced it
  (`coverage-data.xml` not `quality-gates-coverage.xml`).
- Directory name matches tool name: `quality-gates/` ↔ "QualityGates".
