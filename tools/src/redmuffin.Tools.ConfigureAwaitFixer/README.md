# redmuffin.Tools.ConfigureAwaitFixer

A development-only MSBuild-integrated tool that automatically adds
`.ConfigureAwait(false)` to `await` expressions before compilation.
Eliminates the most common Roslyn analyzer error (CA2007) from the
edit-build-fix loop.

## What it does

Runs before the compiler inspects source files (`BeforeTargets="CoreCompile"`).
For every `await` expression not already followed by `.ConfigureAwait(false)`,
the fixer adds it. Skips TUnit `Assert.*` chains to avoid corrupting test code.

## Requirements

- .NET 10 SDK
- `Microsoft.CodeAnalysis.CSharp` (resolved via Central Package Management)

## Installation

This package is distributed from a local NuGet feed (`tools/nupkgs/`). Add it
as a `PackageReference` in your `Directory.Build.props`:

```xml
<PackageReference Include="redmuffin.Tools.ConfigureAwaitFixer"
                  Version="1.0.1" />
```

The package is excluded from CI via environment variable guard:

```xml
<Condition="'$(CI)' != 'true' AND '$(GITHUB_ACTIONS)' != 'true'" />
```

## Configuration

No configuration needed. The fixer applies to every `await` expression in every
`.cs` file. To exclude a specific project, add the property
`<IsConfigureAwaitFixerProject>true</IsConfigureAwaitFixerProject>` to its
`.csproj` — the root `Directory.Build.props` skips any project with this flag.
