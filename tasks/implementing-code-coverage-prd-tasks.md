## Relevant Files

### Configuration Files
- `Directory.Build.props` - Global MSBuild properties for code coverage configuration.
- `coverageconfig.json` - Coverage configuration file for exclusions and settings.
- `.coverletrc` - Coverlet-specific configuration file.

### PowerShell Scripts
- `scripts/Generate-CoverageReport.ps1` - PowerShell script to generate coverage reports locally.
- `scripts/View-CoverageReport.ps1` - PowerShell script to open coverage reports in browser.

### Project Files
- `tests/redmuffin.Blazor.StaticWeb.Tests/redmuffin.Blazor.StaticWeb.Tests.csproj` - Main Blazor test project with coverage configuration.
- `tests/redmuffin.Blazor.StaticWeb.Api.Tests/redmuffin.Blazor.StaticWeb.Api.Tests.csproj` - API test project with coverage configuration.
- `src/redmuffin.Blazor.StaticWeb/redmuffin.Blazor.StaticWeb.csproj` - Main Blazor project with coverage instrumentation.
- `src/redmuffin.Blazor.StaticWeb.Api/redmuffin.Blazor.StaticWeb.Api.csproj` - API project with coverage instrumentation.

### Coverage Output
- `coverage/` - Directory for coverage output files (HTML reports, XML/JSON data).
- `coverage/index.html` - Main HTML coverage report entry point.
- `coverage/coverage.xml` - XML coverage report for tooling integration.
- `coverage/coverage.json` - JSON coverage report for programmatic access.

### Documentation
- `docs/CodeCoverage.md` - Documentation for developers on using code coverage.
- `README.md` - Updated with coverage information and usage instructions.

### Notes

- Tests use TUnit framework with `[Test]` attribute for test methods and `[Arguments]` for data-driven tests.
- Use `dotnet test` with coverage flags to run tests and generate coverage reports.
- Blazor components follow feature-based organization under `src/redmuffin.Blazor.StaticWeb/Features/`.
- Azure Functions use isolated worker model with dependency injection.
- Use Coverlet for coverage collection and ReportGenerator for HTML report generation.
- Coverage reports exclude third-party code and generated files through configuration.
- Local development workflow supports both command-line and PowerShell script execution.

## Tasks

- [x] 1.0 Setup Code Coverage Infrastructure
  - [x] 1.1 Install Coverlet.MSBuild NuGet package to test projects
  - [x] 1.2 Install ReportGenerator global tool for HTML report generation
  - [x] 1.3 Create coverage output directory structure
  - [x] 1.4 Verify TUnit compatibility with Coverlet
- [x] 2.0 Configure Coverage Collection
  - [x] 2.1 Add Coverlet configuration to test project files
  - [x] 2.2 Configure coverage data collection formats (opencover, cobertura)
  - [x] 2.3 Set up coverage thresholds and fail conditions
  - [x] 2.4 Test coverage collection with existing TUnit tests
- [x] 3.0 Implement Coverage Exclusions
  - [x] 3.1 Create global exclusion configuration in Directory.Build.props
  - [x] 3.2 Configure exclusions for third-party libraries and NuGet packages
  - [x] 3.3 Exclude generated code files and auto-generated Blazor artifacts
  - [x] 3.4 Set up assembly-level and namespace-level exclusions
- [x] 4.0 Create Coverage Report Generation
  - [x] 4.1 Configure ReportGenerator to produce HTML reports
  - [x] 4.2 Set up XML and JSON report generation
  - [x] 4.3 Create unified coverage report combining all test projects
  - [x] 4.4 Configure report styling and branding
- [x] 5.0 Integrate with Local Development Workflow
  - [x] 5.1 Create PowerShell script for generating coverage reports
  - [x] 5.2 Create PowerShell script for viewing coverage reports
  - [x] 5.3 Add coverage commands to project documentation
  - [x] 5.4 Create developer guide for interpreting coverage results
  - [x] 5.5 Set up coverage trend tracking mechanism
