# Locally Testable GitHub Workflow - To Do

## Relevant Files

### PowerShell Scripts
- `Deploy-LocalWorkflow.ps1` - Main PowerShell script that replicates the GitHub workflow functionality locally and in CI/CD.
- `scripts/WorkflowFunctions.psm1` - PowerShell module containing reusable functions for workflow phases.
- `scripts/Test-Prerequisites.ps1` - PowerShell script for validating required tools and environment setup.

### GitHub Workflow
- `.github/workflows/azure-static-web-apps-lively-cliff-0945be603.yml` - Updated workflow file that calls the PowerShell script.

### Configuration
- `workflow-config.json` - Configuration file for workflow settings and file patterns.
- `src/redmuffin.Blazor.StaticWeb.Api/local.settings.json` - Existing local configuration (referenced, not modified).

### Documentation
- `docs/local-workflow-guide.md` - Documentation for using the local workflow script.

### Tests
- `tests/redmuffin.Blazor.StaticWeb.Tests/Scripts/WorkflowFunctionsTests.cs` - TUnit tests for PowerShell workflow functions.
- `tests/redmuffin.Blazor.StaticWeb.Tests/Scripts/ChangeDetectionTests.cs` - TUnit tests for change detection logic.

### Notes

- Tests use TUnit framework with `[Test]` attribute for test methods and `[Arguments]` for data-driven tests.
- Mocking uses LightMock.Generator ONLY (NSubstitute deprecated).
- Use `dotnet clean && dotnet build --no-restore --verbosity quiet` to verify zero build warnings (except IL2111).
- Use `dotnet test` to run all tests or `dotnet test --filter "FullyQualifiedName~[TestClassName]"` for specific test classes.
- **QUALITY CHECK**: After every step verify TestScope pattern, custom mock pattern, ConfigureAwait(false) compliance following HomeTests*.cs files as prime example.
- PowerShell script must work on both Windows PowerShell 5.1+ and PowerShell Core 7+ (pwsh).
- Cross-platform compatibility required for CI/CD execution on Ubuntu runners.
- Configuration hierarchy: local.settings.json → environment variables → script defaults.
- All async operations in supporting C# code must use `ConfigureAwait(false)` and proper error handling.
- Follow StyleCop/Meziantou analyzer rules for any C# code quality.
- PowerShell functions must follow established naming conventions and include proper error handling.

## Tasks

- [ ] 1.0 Setup Prerequisites and Configuration Foundation
  - [ ] 1.1 Create workflow-config.json file with file patterns and workflow settings matching GitHub workflow (lines 47-77)
  - [ ] 1.2 Create prerequisites validation script (Test-Prerequisites.ps1) to check dotnet, node, git, and swa CLI installation
  - [ ] 1.3 Create PowerShell module structure (scripts/WorkflowFunctions.psm1) with proper module manifest
  - [ ] 1.4 Implement centralized configuration loading function (local.settings.json → env vars → defaults hierarchy)
  - [ ] 1.5 Add unified configuration validation to verify all required values are present
- [ ] 2.0 Design Core PowerShell Script Architecture  
  - [ ] 2.1 Create main Deploy-LocalWorkflow.ps1 script with proper PowerShell structure and cmdlet binding
  - [ ] 2.2 Implement environment detection logic to differentiate between local Windows and CI/CD Ubuntu environments
  - [ ] 2.3 Add parameter support for execution modes (full, test-only, build-only, deploy-only, dry-run)
  - [ ] 2.4 Create centralized logging and output functions with colored output support and progress indicators
  - [ ] 2.5 Implement proper error handling with exit codes, cleanup on failure, and troubleshooting hints
- [ ] 3.0 Implement Change Detection Logic
  - [ ] 3.1 Create Git-based change detection function that replicates workflow logic exactly
  - [ ] 3.2 Implement file pattern matching using patterns from workflow-config.json
  - [ ] 3.3 Add support for testing change detection against different Git commits/branches
  - [ ] 3.4 Create function to determine if only documentation files changed (skip deployment logic)
  - [ ] 3.5 Add verbose output showing which files changed and deployment decision reasoning
- [ ] 4.0 Create Testing Framework (TDD Approach)
  - [ ] 4.1 Create TUnit test project structure for PowerShell workflow functions using C# wrapper classes
  - [ ] 4.2 Add change detection unit tests with various Git state scenarios
  - [ ] 4.3 Create configuration management unit tests for different environment scenarios
  - [ ] 4.4 Implement test utilities and mock helpers using LightMock.Generator
  - [ ] 4.5 Add prerequisites validation tests
- [ ] 5.0 Implement Testing Phase Integration
  - [ ] 5.1 Create test execution function that runs TUnit tests with same parameters as workflow
  - [ ] 5.2 Implement environment variable setup for tests using configuration hierarchy
  - [ ] 5.3 Add fail-fast logic that exits with code 1 if tests don't pass
  - [ ] 5.4 Implement parallel test execution support matching TUnit default behavior
  - [ ] 5.5 Add test result reporting with condensed output and error highlighting
- [ ] 6.0 Implement Build Phase Automation
  - [ ] 6.1 Implement WASM workload installation for SIMD optimizations (prerequisite for build)
  - [ ] 6.2 Create .NET dependency restoration function with local caching support
  - [ ] 6.3 Add Blazor WebAssembly build function with Release configuration and exact workflow parameters
  - [ ] 6.4 Create Azure Functions build function matching workflow build parameters (lines 213-229)
  - [ ] 6.5 Implement build verification and output structure validation
- [ ] 7.0 Implement Deployment Phase Integration
  - [ ] 7.1 Create Node.js setup and Azure Static Web Apps CLI installation/verification functions
  - [ ] 7.2 Implement deployment function using same parameters as workflow (swa deploy)
  - [ ] 7.3 Add deployment token management from configuration hierarchy
  - [ ] 7.4 Create deployment health check function with site accessibility verification
  - [ ] 7.5 Implement deployment output reporting with URL and status information
- [ ] 8.0 Implement Dry-Run Mode and Advanced Features
  - [ ] 8.1 Add dry-run mode implementation that shows what would be executed without running
  - [ ] 8.2 Create rollback/cleanup functions for failed deployments
  - [ ] 8.3 Implement selective execution modes (test-only, build-only, deploy-only)
  - [ ] 8.4 Add performance optimization features (parallel execution where possible)
  - [ ] 8.5 Create workflow execution reporting and logging
- [ ] 9.0 Create GitHub Workflow Integration
  - [ ] 9.1 Update existing workflow file to call the PowerShell script instead of individual steps
  - [ ] 9.2 Maintain existing environment variable and secret passing to the script
  - [ ] 9.3 Preserve existing workflow triggers and conditions
  - [ ] 9.4 Add workflow validation to ensure script execution works in CI/CD environment
  - [ ] 9.5 Create transition plan for gradual workflow migration
- [ ] 10.0 Integration Testing and Documentation
  - [ ] 10.1 Create integration tests for complete workflow execution in different scenarios
  - [ ] 10.2 Add end-to-end tests for local vs CI/CD environment differences
  - [ ] 10.3 Write comprehensive documentation for local workflow usage
  - [ ] 10.4 Add troubleshooting guide and common issues resolution
  - [ ] 10.5 Create quick-start guide and examples for different use cases
