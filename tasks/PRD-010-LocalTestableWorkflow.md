# Locally Testable GitHub Workflow

## Introduction/Overview

Transform the existing GitHub Actions workflow (`azure-static-web-apps-lively-cliff-0945be603.yml`) into a locally testable PowerShell script that can be executed both on Windows development machines and in the CI/CD pipeline. This will enable developers to test the complete deployment pipeline locally before pushing changes, reducing CI/CD failures and improving development velocity.

The solution replaces the current YAML-based workflow with a PowerShell script that the workflow calls, ensuring identical behavior between local development and production deployment.

## Goals

- **DEV-001**: Create a PowerShell script that replicates 100% of the GitHub workflow functionality locally
- **DEV-002**: Enable local testing of change detection logic against different Git states
- **DEV-003**: Provide identical build, test, and deployment processes between local and CI/CD environments  
- **DEV-004**: Reduce CI/CD pipeline failures by catching issues locally before push
- **DEV-005**: Maintain existing workflow performance optimizations (caching, parallel execution)
- **DEV-006**: Support both Windows PowerShell (local) and pwsh (CI/CD) execution environments

## User Stories

- **US-001**: As a developer, I want to run the complete deployment pipeline locally so that I can verify changes before pushing to GitHub
- **US-002**: As a developer, I want to test change detection logic locally so that I can verify which files trigger deployments
- **US-003**: As a developer, I want to simulate different deployment scenarios locally so that I can troubleshoot deployment issues without using CI/CD resources
- **US-004**: As a DevOps engineer, I want the CI/CD pipeline to use the same script as local development so that behavior is consistent across environments
- **US-005**: As a developer, I want condensed output with all important information so that I can quickly identify issues without verbose logging
- **US-006**: As a developer, I want to use my existing local.settings.json configuration so that I don't need to manage separate environment variables

## Functional Requirements

### Change Detection
- **FR-001**: The script must replicate the GitHub workflow's change detection logic exactly
- **FR-002**: The script must identify documentation-only changes and skip deployment accordingly
- **FR-003**: The script must support testing change detection against different Git commits/branches
- **FR-004**: The script must use the same file patterns as defined in the workflow (lines 47-77)

### Testing Phase
- **FR-005**: The script must run TUnit tests with the same parameters as the workflow
- **FR-006**: The script must use local.settings.json values for local execution
- **FR-007**: The script must use environment variables for CI/CD execution
- **FR-008**: The script must fail fast if tests don't pass (exit code 1)
- **FR-009**: The script must support parallel test execution like TUnit default behavior

### Build Phase  
- **FR-010**: The script must restore .NET dependencies with caching support locally
- **FR-011**: The script must install WASM workload for SIMD optimizations
- **FR-012**: The script must build both Blazor WebAssembly and Azure Functions projects
- **FR-013**: The script must use Release configuration for production-like builds
- **FR-014**: The script must replicate exact build parameters from workflow (lines 213-229)

### Deployment Phase
- **FR-015**: The script must install and use Azure Static Web Apps CLI (swa)
- **FR-016**: The script must deploy using the same parameters as the workflow
- **FR-017**: The script must support both local testing and actual deployment modes
- **FR-018**: The script must use the same directory structure and output paths
- **FR-019**: The script must read deployment token from local config or environment variable

### Environment Management
- **FR-020**: The script must auto-detect execution environment (local vs. CI/CD)
- **FR-021**: The script must load configuration from local.settings.json when available
- **FR-022**: The script must fall back to environment variables when local.settings.json is not available
- **FR-023**: The script must support both Windows PowerShell and PowerShell Core (pwsh)

### Output and Reporting
- **FR-024**: The script must provide condensed output with all important information
- **FR-025**: The script must use colored output for better readability (when supported)
- **FR-026**: The script must show progress indicators for long-running operations
- **FR-027**: The script must provide clear error messages with troubleshooting hints
- **FR-028**: The script must output deployment URL and health check status

## Non-Goals (Out of Scope)

- **NG-001**: CodeQL security analysis workflow transformation (explicitly excluded)
- **NG-002**: Modifying existing GitHub Actions YAML structure (only calling the script)
- **NG-003**: Creating separate scripts for different environments (single script approach)
- **NG-004**: Supporting package managers other than NuGet and npm
- **NG-005**: Implementing custom caching logic (rely on existing .NET and npm caches)
- **NG-006**: GUI interface for script execution (command-line only)

## Design Considerations

### PowerShell Script Architecture
- **Single Script Approach**: One `Deploy-LocalWorkflow.ps1` script that handles all phases
- **Environment Detection**: Auto-detect local vs. CI/CD environment based on available tools and paths
- **Configuration Hierarchy**: local.settings.json → environment variables → script defaults
- **Modular Functions**: Separate functions for each workflow phase (change detection, test, build, deploy)

### Zurb Foundation Integration
- The script focuses on backend build/deploy processes; UI styling remains handled by existing SCSS build process
- Foundation classes and SCSS compilation handled by existing dotnet build process
- No additional UI components needed for this infrastructure-focused feature

### GitHub Actions Integration
- **Workflow Simplification**: Transform existing multi-job workflow into single script execution
- **Environment Variables**: Maintain existing secret and environment variable structure
- **Artifact Management**: Use same build output paths and artifact locations

## Technical Considerations

### PowerShell Compatibility
- **Cross-Platform**: Script must work on Windows PowerShell 5.1+ and PowerShell Core 7+
- **CI/CD Execution**: Use `pwsh` in Ubuntu GitHub runners for consistent behavior
- **Module Dependencies**: Avoid external PowerShell modules to minimize dependencies

### Configuration Management
- **Local Development**: Read from `src/redmuffin.Blazor.StaticWeb.Api/local.settings.json`
- **CI/CD Environment**: Use environment variables as defined in workflow
- **Validation**: Verify all required configuration values are available before execution

### Error Handling and Logging
- **Exit Codes**: Use appropriate exit codes for different failure scenarios
- **Verbose Logging**: Support `-Verbose` parameter for detailed logging during troubleshooting
- **Progress Reporting**: Show clear progress indicators for each major phase

### Performance Optimizations
- **Dependency Caching**: Leverage existing NuGet and npm cache directories
- **Parallel Execution**: Maintain TUnit's parallel test execution
- **Build Optimization**: Use `--no-restore` and `--no-dependencies` flags where appropriate

## Success Metrics

- **SM-001**: 100% functional parity between local script execution and CI/CD workflow
- **SM-002**: Reduce CI/CD pipeline failures by 50% through local pre-testing
- **SM-003**: Enable developers to identify deployment issues within 5 minutes locally
- **SM-004**: Maintain existing workflow performance (no significant slowdown)
- **SM-005**: Support all team members' local development environments (Windows 10/11)

## Implementation Notes

### Script Structure
- **Main Function**: `Deploy-LocalWorkflow.ps1` with parameter-based execution modes
- **Configuration Functions**: Auto-detect and load configuration from multiple sources
- **Phase Functions**: Separate functions for check-changes, test, build, deploy phases
- **Utility Functions**: Helper functions for environment detection, logging, error handling

### GitHub Workflow Integration
- **Simplified Workflow**: Replace existing jobs with single script execution
- **Environment Setup**: Maintain existing .NET, Node.js, and tool installation steps
- **Secret Management**: Pass secrets as environment variables to script
- **Artifact Handling**: Use same paths and naming conventions

### Local Development Workflow
- **Prerequisites**: Document required tools (dotnet, node, swa CLI)
- **Quick Start**: Provide simple command for full pipeline execution
- **Selective Execution**: Support running specific phases (test-only, build-only, etc.)
- **Configuration Validation**: Verify local setup before attempting execution

### Testing Strategy
- **Unit Tests**: Test individual PowerShell functions using TUnit framework
- **Integration Tests**: Test complete workflow execution in different scenarios
- **Environment Tests**: Verify behavior in both local and CI/CD environments
- **Change Detection Tests**: Test against various Git states and file change patterns

## Open Questions

- **OQ-001**: Should the script support a "dry-run" mode that shows what would be executed without actually running it?
- **OQ-002**: Do we need backward compatibility with the existing workflow during transition period?
- **OQ-003**: Should the script support incremental deployment (only changed components)?
- **OQ-004**: Do we need integration with existing `Start.ps1` development script?
- **OQ-005**: Should the script generate deployment reports or logs for audit purposes?
