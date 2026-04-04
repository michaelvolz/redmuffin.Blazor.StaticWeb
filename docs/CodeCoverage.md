---
title: Code Coverage Guide
date: 2025-07-12
---

This guide explains how to use code coverage in the redmuffin.Blazor.StaticWeb project using Coverlet and ReportGenerator.

## Quick Start

### Generate Coverage Reports

```powershell
# Run tests and generate coverage reports
.\scripts\Generate-CoverageReport.ps1

# View the unified coverage report
.\scripts\View-CoverageReport.ps1
```

## Coverage Commands

### PowerShell Scripts

#### Generate Coverage Reports

```powershell
# Generate all coverage reports
.\scripts\Generate-CoverageReport.ps1

# Generate without rebuilding projects
.\scripts\Generate-CoverageReport.ps1 -NoBuild
```

#### View Coverage Reports

```powershell
# View unified coverage report (default)
.\scripts\View-CoverageReport.ps1

# View branded coverage report with history
.\scripts\View-CoverageReport.ps1 -ReportType Branded

# View basic HTML coverage report
.\scripts\View-CoverageReport.ps1 -ReportType Html
```

### Manual Commands

#### Running Tests with Coverage

```bash
# Blazor tests
dotnet test tests/redmuffin.Blazor.StaticWeb.Tests

# API tests
dotnet test tests/redmuffin.Blazor.StaticWeb.Api.Tests

# All tests
dotnet test
```

#### Generate HTML Reports Manually

```bash
# Generate unified HTML report
reportgenerator -reports:"coverage/*.opencover.xml" -targetdir:"coverage/unified" -reporttypes:"Html"

# Generate branded report with history
reportgenerator -reports:"coverage/*.opencover.xml" -targetdir:"coverage/branded" -reporttypes:"Html" -title:"redmuffin.Blazor.StaticWeb - Code Coverage Report" -tag:"v1.0.0" -historydir:"coverage/history"

# Generate XML summary
reportgenerator -reports:"coverage/*.opencover.xml" -targetdir:"coverage" -reporttypes:"Xml,JsonSummary"
```

## Coverage Configuration

### Project Configuration

Coverage is configured in the test project files:

- `tests/redmuffin.Blazor.StaticWeb.Tests/redmuffin.Blazor.StaticWeb.Tests.csproj`
- `tests/redmuffin.Blazor.StaticWeb.Api.Tests/redmuffin.Blazor.StaticWeb.Api.Tests.csproj`

### Global Exclusions

Global exclusions are configured in:

- `Directory.Build.props` - MSBuild-level exclusions
- `.coverletrc` - Coverlet-specific exclusions

### Coverage Thresholds

- **Blazor Tests**: 1% minimum (line, branch, method)
- **API Tests**: 40% line, 35% branch, 50% method

## Report Locations

### HTML Reports

- **Unified Report**: `coverage/unified/index.html`
- **Branded Report**: `coverage/branded/index.html`
- **Basic Report**: `coverage/html/index.html`

### Data Files

- **OpenCover XML**: `coverage/blazor-.opencover.xml`, `coverage/api-.opencover.xml`
- **Cobertura XML**: `coverage/blazor-.cobertura.xml`, `coverage/api-.cobertura.xml`
- **JSON Data**: `coverage/blazor-.json`, `coverage/api-.json`
- **Summary Files**: `coverage/Summary.xml`, `coverage/Summary.json`

## Coverage Exclusions

The following are automatically excluded from coverage:

- Third-party libraries (System._, Microsoft._, etc.)
- Test projects (_Tests_)
- Generated code files (_.g.cs, _.designer.cs)
- Build artifacts (obj/, bin/)
- Program and Startup classes

## Tools Required

### Global Tools

```bash
# Install ReportGenerator globally
dotnet tool install --global dotnet-reportgenerator-globaltool
```

### Project Dependencies

- **coverlet.msbuild**: Integrated into test projects
- **TUnit**: Testing framework with coverage compatibility

## Troubleshooting

### Common Issues

1. **Coverage files not generated**
   - Ensure Coverlet.MSBuild is installed in test projects
   - Check that tests are running successfully
   - Verify exclude patterns aren't too broad

2. **Reports not opening**
   - Check if coverage reports exist in expected locations
   - Ensure default browser is configured
   - Run `.\scripts\Generate-CoverageReport.ps1` first

3. **Low coverage percentages**
   - Review exclusion patterns to ensure they're not excluding your code
   - Check that tests are actually testing your code
   - Use the HTML reports to identify untested areas

### Debugging Coverage

```bash
# Run with verbose output
dotnet test tests/redmuffin.Blazor.StaticWeb.Tests --verbosity normal

# Check coverage configuration
dotnet test tests/redmuffin.Blazor.StaticWeb.Tests --collect:"XPlat Code Coverage" --verbosity diagnostic
```

## Integration with Development Workflow

### Local Development

1. Write or modify code
2. Write or update tests
3. Run `.\scripts\Generate-CoverageReport.ps1`
4. Review coverage reports
5. Add tests for uncovered areas
6. Repeat until satisfied with coverage

### Code Review Process

- Include coverage reports in pull request reviews
- Aim for high coverage on new code
- Use coverage trends to track improvement over time

## Coverage Metrics Interpretation

### Line Coverage

Percentage of executable code lines that are covered by tests.

### Branch Coverage

Percentage of decision branches (if/else, switch) that are tested.

### Method Coverage

Percentage of methods that have at least one test execution.

### Target Coverage Goals

- **New Code**: Aim for 90%+ coverage
- **Existing Code**: Gradual improvement, tracking trends
- **Critical Paths**: 100% coverage for essential business logic

## History and Trends

Coverage history is stored in `coverage/history/` to track trends over time. Use the branded report to visualize coverage evolution.
