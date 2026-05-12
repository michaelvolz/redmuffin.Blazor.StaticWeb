---
name: rm-guide-naming
description: "Shortcut: rm:guide-naming. Use when creating, renaming, or reviewing C# names for types, members, namespaces, and test doubles."
---

# rm-guide-naming

## CRITICAL

- Use PascalCase for types, namespaces, methods, properties, events, enums.
- Use `camelCase` for locals and parameters.
- Use `_camelCase` for private fields.
- Prefix interfaces with `I`.
- Name test doubles as `[Class]_[Type]` (`Mock`, `Stub`, `Spy`, `Fake`, `Dummy`).

## WHEN TO LOAD

- Creating or renaming C# files, classes, records, enums, interfaces, methods.
- Reviewing names in new tests or test doubles.

## GUIDANCE

- Prefer explicit, intention-revealing names.
- Avoid abbreviations unless the domain already uses them.
- Match existing repo names exactly when extending a pattern.

## NEVER

- Do not invent new naming schemes inside a feature.
- Do not use Hungarian notation.
- Do not abbreviate CLI flag names, subcommands, or configuration file names.

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
