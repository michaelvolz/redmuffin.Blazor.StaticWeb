<!-- 
This README is intended for human developers. 
AI assistants should refer to .github/copilot-instructions.md for technical guidelines.
-->

# redmuffin.Blazor.StaticWeb (preview - alpha)

[![Build Status](https://github.com/michaelvolz/redmuffin.Blazor.StaticWeb/actions/workflows/azure-static-web-apps-lively-cliff-0945be603.yml/badge.svg)](https://github.com/michaelvolz/redmuffin.Blazor.StaticWeb/actions/workflows/azure-static-web-apps-lively-cliff-0945be603.yml)
[![CodeQL](https://github.com/michaelvolz/redmuffin.Blazor.StaticWeb/actions/workflows/codeql.yml/badge.svg)](https://github.com/michaelvolz/redmuffin.Blazor.StaticWeb/actions/workflows/codeql.yml)
[![Last Commit (master)](https://img.shields.io/github/last-commit/michaelvolz/redmuffin.Blazor.StaticWeb/master.svg)](https://github.com/michaelvolz/redmuffin.Blazor.StaticWeb/commits/master)

[![License: Unlicense](https://img.shields.io/badge/license-Unlicense-blue.svg)](https://en.wikipedia.org/wiki/Unlicense)
[![Dependabot enabled](https://img.shields.io/badge/Dependabot-enabled-blue.svg)](https://docs.github.com/en/code-security/dependabot/working-with-dependabot)
![GitHub language count](https://img.shields.io/github/languages/count/michaelvolz/redmuffin.Blazor.StaticWeb)
![GitHub code size in bytes](https://img.shields.io/github/languages/code-size/michaelvolz/redmuffin.Blazor.StaticWeb)

[![.NET 9](https://img.shields.io/badge/.NET-9-512BD4)](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
[![Blazor WebAssembly](https://img.shields.io/badge/Blazor-WebAssembly-512BD4?logo=blazor&logoColor=white)](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor)
[![Azure Functions](https://img.shields.io/badge/Azure-Functions-0078D4?logo=microsoftazure&logoColor=white)](https://azure.microsoft.com/en-us/products/functions)
[![C# 13](https://img.shields.io/badge/C%23-13-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![TUnit](https://img.shields.io/badge/Testing-TUnit-green?logo=.net&logoColor=white)](https://github.com/thomhurst/TUnit)
[![Foundation](https://img.shields.io/badge/UI-Foundation-29A2DD?logo=foundation&logoColor=white)](https://get.foundation/)

[![Maintenance](https://img.shields.io/badge/Maintained-yes-green.svg)](https://github.com/michaelvolz/redmuffin.Blazor.StaticWeb/graphs/commit-activity)
[![GitHub commit activity](https://img.shields.io/github/commit-activity/m/michaelvolz/redmuffin.Blazor.StaticWeb)](https://github.com/michaelvolz/redmuffin.Blazor.StaticWeb/graphs/commit-activity)


## Project Status

**Alpha/Preview Release** - This project is in early development. While functional, expect:
- Breaking changes between versions
- Incomplete documentation
- Active development with frequent updates
- Limited production readiness

Suitable for experimentation, learning, and development environments.

## Overview

**redmuffin.Blazor.StaticWeb** is a modern full-stack web application built with Blazor WebAssembly (.NET 9) and Azure Functions (.NET 8). The solution provides a performant, maintainable static web application with serverless backend capabilities, featuring OAuth integration and comprehensive testing infrastructure.

## Table of Contents

- [Project Status](#project-status)
- [Overview](#overview)
- [Features](#features)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Development Workflow](#development-workflow)
  - [Trunk-Based Development](#trunk-based-development)
- [Usage](#usage)
- [Project Structure](#project-structure)
- [Technology Stack](#technology-stack)
- [Development Tools](#development-tools)
  - [Local Development](#local-development-visual-studio-multi-project-startup)
  - [Azure Functions Integration](#azure-functions-integration)
  - [MCP Server Integration](#mcp-server-integration)
- [Build and Deployment](#build-and-deployment)
- [License](#license)
- [Acknowledgements](#acknowledgements)

---

## Features

### Core Functionality
- **Blazor WebAssembly (.NET 9)** - Client-side execution with modern C# features
- **Azure Functions (.NET 8)** - Serverless backend with HTTP triggers
- **Raindrop.io OAuth Integration** - External API integration with secure authentication
- **Markdown Content Rendering** - Advanced Markdown processing with Markdig

### Development & Quality
- **Modern C# (C# 12/13) features** - Primary constructors, collection expressions, ref readonly parameters
- **Comprehensive Testing** - TUnit framework with NSubstitute mocking
- **Code Coverage** - Automated coverage reports with Coverlet and ReportGenerator
- **PowerShell Automation** - Scripts for coverage report generation and viewing
- **SCSS Styling Only** - All styling should be done using SCSS files in the `wwwroot/scss/` directory.
- **CSS Files are Auto-Generated** - Direct modifications to CSS files are not allowed; they are automatically generated from SCSS.
- **SCSS Partials** - All SCSS partial files must start with an underscore (_) and be included in `app.scss` for automatic compilation.
- **Feature Folder Structure** - Organized by feature for better maintainability
- **Code Quality & Security** - CodeQL analysis, automated builds, Dependabot integration
- **Accessibility Compliance** - WCAG 2.1 AA standards with semantic HTML and ARIA support

### Infrastructure & Tools
- **EditorConfig** - Consistent code style and formatting
- **Directory.Build.props** - Centralized project configuration
- **Docker Integration** - Configures MCP servers for AI assistance (GitHub, Fetch, Time, Brave Search, Sequential Thinking servers)
- **Azure Static Web Apps** - Deployment and hosting platform

---

## Prerequisites

### Required Software
- **Visual Studio 2022** (17.8 or later) with the following workloads:
  - ASP.NET and web development
- **.NET 9 SDK** - For Blazor WebAssembly project
- **.NET 8 SDK** - For Azure Functions project
- **Node.js** (Latest LTS) - Required for Azure Static Web Apps CLI

### Global Tools

#### Node.js Tools (npm)
- **Azure Static Web Apps CLI**  
  Required for local development and testing of Azure Static Web Apps:
  ```bash
  npm install -g @azure/static-web-apps-cli
  ```

#### .NET Tools
- **Project-Local Tools** (automatically managed)  
  The project includes pre-configured .NET tools in `.config/dotnet-tools.json`:
  ```bash
  # Restore all project-local tools
  dotnet tool restore
  ```
  
  **Included Tools:**
  - `Microsoft.Web.LibraryManager.Cli` (LibMan) - Client-side library management

- **Additional Global Tools** (installed automatically by scripts)  
  These tools are installed on-demand by the project scripts:
  - `dotnet-reportgenerator-globaltool` - Code coverage report generation
  
  **Manual Installation (if needed):**
  ```bash
  # Install ReportGenerator globally
  dotnet tool install --global dotnet-reportgenerator-globaltool
  ```

### Optional Tools
- **Azure CLI** - For Azure resource management and deployment
- **Docker Desktop**
  - Required for optional MCP server integration (enhances AI assistant capabilities)
  - [Download from Docker website](https://www.docker.com/products/docker-desktop)

---

## Getting Started

### Quick Start (Experienced Developers)

```bash
# Clone and setup
git clone https://github.com/michaelvolz/redmuffin.Blazor.StaticWeb.git
cd redmuffin.Blazor.StaticWeb
npm install -g @azure/static-web-apps-cli

# Build and run
dotnet restore
dotnet build
dotnet test

# Start development environment
# Open redmuffin.Blazor.StaticWeb.sln in Visual Studio
# Use "Start both" profile or press F5
# Navigate to http://localhost:4280
```

### Detailed Setup

1. **Clone the repository:**

   ```bash
   git clone https://github.com/michaelvolz/redmuffin.Blazor.StaticWeb.git
   cd redmuffin.Blazor.StaticWeb
   ```

2. **Verify prerequisites:**
   - Check .NET versions: `dotnet --list-sdks`
   - Ensure you have .NET 8 and .NET 9 SDKs installed
   - Verify Node.js: `node --version`
   - (Optional) Check Docker: `docker --version` - only needed for MCP server integration

3. **Install global tools:**

   ```bash
   npm install -g @azure/static-web-apps-cli
   ```

4. **Restore dependencies:**

   ```bash
   dotnet restore
   dotnet tool restore
   ```

5. **Build the solution:**

   ```bash
   dotnet build
   ```

6. **Run tests to verify setup:**

   ```bash
   dotnet test
   ```

7. **Start the development environment:**
   - Open `redmuffin.Blazor.StaticWeb.sln` in Visual Studio 2022
   - Use the "Start both" profile or press F5
   - The application will start on `http://localhost:4280`

### Validation Steps

After setup, verify everything is working:

1. **Build succeeds without errors**
2. **All tests pass**
3. **Application loads at `http://localhost:4280`**
4. **API endpoints are accessible** (check browser dev tools Network tab)
5. **Hot reload works** (modify a `.razor` file and see changes)

### Known Build Warnings

**IL2111 Warnings (Expected and Safe to Ignore):**

During development and building, you may encounter IL2111 warnings like:

```
warning IL2111: Method 'Microsoft.AspNetCore.Components.LayoutView.Layout.set' with parameters or return value with `DynamicallyAccessedMembersAttribute` is accessed via reflection. Trimmer can't guarantee availability of the requirements of the method.
```

**These warnings are expected and safe to ignore** because:
- They occur in generated Razor files (`App_razor.g.cs`) during Blazor WebAssembly compilation
- They are related to Blazor's internal layout handling mechanism
- They do not affect application functionality or performance
- They are part of the normal Blazor compilation process and ASP.NET Core Components trimming optimization
- They are in generated code that is not under developer control

**Action required:** None - these warnings can be safely ignored during development and deployment.

### Next Steps

- See [Usage](#usage) for common development tasks
- Check [Local Development](#local-development-visual-studio-multi-project-startup) for development workflow
- Review [MCP Server Integration](#mcp-server-integration) for AI-enhanced development

---

## Development Workflow

### Trunk-Based Development

This project follows **[Trunk-Based Development](https://trunkbaseddevelopment.com/)** - a source-control branching model where developers collaborate on code in a single branch called 'trunk' (or 'main'/'master'), avoiding long-lived feature branches.

#### Key Principles

- **Single Main Branch**: All development happens on the `master` branch
- **Frequent Integration**: Developers commit/push to trunk **at least once every 24 hours**
- **Short-Lived Feature Branches**: When used, feature branches are small, short-lived (hours to 1-2 days), and created from a single developer workstation
- **Continuous Integration**: Every commit triggers automated builds and tests
- **No Merge Hell**: Avoid the complexity of long-lived branches and large merges

#### Benefits for This Project

- **Continuous Integration Ready**: Enables true CI/CD with frequent integration
- **Faster Feedback**: Issues are discovered and resolved quickly
- **Simplified Workflow**: No complex branching strategies to manage
- **Better Collaboration**: All developers work with the latest code
- **Reduced Risk**: Smaller, more frequent changes are easier to review and safer to deploy

#### Workflow Guidelines

1. **Daily Commits**: Commit to `master` at least once per day
2. **Small Changes**: Break work into small, incremental commits
3. **Pre-Integration Checks**: Run full build and tests before pushing
4. **Feature Flags**: Use feature flags for incomplete features rather than branches
5. **Pull Requests**: Use short-lived PRs for code review (merge within 24 hours)

#### Best Practices

- **Keep the Build Green**: Never break the build on `master`
- **Test Locally First**: Run `dotnet build` and `dotnet test` before pushing
- **Use Feature Flags**: Hide incomplete features behind flags instead of long-lived branches
- **Quick Code Reviews**: Review and merge PRs promptly to avoid drift
- **Rollback Ready**: Maintain ability to rollback any commit if needed

#### Tools and Automation

- **GitHub Actions**: Automated CI/CD pipeline on every push
- **Automated Testing**: TUnit tests run on every commit
- **Code Quality Checks**: CodeQL security scanning and analysis
- **Deployment Pipeline**: Automatic deployment to Azure Static Web Apps

#### Anti-Patterns to Avoid

- ❌ Long-lived feature branches (more than 1-2 days)
- ❌ Delaying integration until "feature complete"
- ❌ Large batch commits
- ❌ Breaking the build on `master`
- ❌ Avoiding commits due to "incomplete" work

#### Resources

- **[Official Trunk-Based Development Site](https://trunkbaseddevelopment.com/)**
- **[Atlassian Guide](https://www.atlassian.com/continuous-delivery/continuous-integration/trunk-based-development)**
- **[Martin Fowler's Feature Flags](https://martinfowler.com/articles/feature-toggles.html)**
- **[Continuous Integration](https://www.atlassian.com/continuous-delivery/continuous-integration)**

---

## Usage

### Common Development Tasks

#### Running the Application

```bash
# Start the full development environment
# Open redmuffin.Blazor.StaticWeb.sln in Visual Studio
# Press F5 or use "Start both" profile
# Application will be available at http://localhost:4280
```

#### Building and Testing

```bash
# Build the entire solution
dotnet build

# Run all tests
dotnet test

# Generate code coverage report
.\scripts\Generate-CoverageReport.ps1

# View coverage report
.\scripts\View-CoverageReport.ps1
```

#### Working with Features

```bash
# Create a new feature (example structure)
# Add files under src/redmuffin.Blazor.StaticWeb/Features/YourFeature/
# - YourFeature.razor           # Main component
# - YourFeature.razor.cs        # Code-behind
# - YourFeature.razor.css       # Component styles
# - Components/                 # Child components
```

#### API Development

```bash
# Add new Azure Function
# Add files under src/redmuffin.Blazor.StaticWeb.Api/Functions/
# Functions are automatically discovered by the runtime

# Test API endpoints
# Use browser dev tools or tools like Postman
# Base URL: http://localhost:4280/api/
```

#### Debugging

- **Frontend**: Set breakpoints in `.razor.cs` files
- **Backend**: Set breakpoints in Azure Functions
- **Network**: Use browser dev tools to inspect API calls

### Example Workflows

#### Adding a New Page

1. Create `src/redmuffin.Blazor.StaticWeb/Features/Pages/NewPage.razor`
2. Add `@page "/newpage"` directive
3. Implement component logic in `NewPage.razor.cs`
4. Add styles in `NewPage.razor.css`
5. Test locally and add unit tests

#### Creating an API Endpoint

1. Create `src/redmuffin.Blazor.StaticWeb.Api/Functions/NewFunction.cs`
2. Add `[Function("FunctionName")]` attribute
3. Implement HTTP trigger logic
4. Add corresponding tests in test project
5. Test with frontend integration

---

## Project Structure

The project follows a [feature folder structure](https://dev.to/smotastic/layer-vs-feature-architecture-3cko) to organize code by feature rather than by technical layer. This approach improves maintainability and scalability by grouping related components, services, and assets together.

```
redmuffin.Blazor.StaticWeb/
├── .github/
│   ├── instructions/                        # AI coding guidelines
│   ├── workflows/                           # GitHub Actions
│   └── prompts/                             # AI prompts
├── src/
│   ├── redmuffin.Blazor.StaticWeb/          # Blazor WebAssembly (.NET 9)
│   │   ├── Features/
│   │   │   ├── Pages/
│   │   │   └── Shared/
│   │   ├── Core/
│   │   ├── wwwroot/
│   │   └── Properties/
│   ├── redmuffin.Blazor.StaticWeb.Api/      # Azure Functions (.NET 8)
│   │   ├── Functions/
│   │   └── Core/
│   ├── redmuffin.Blazor.StaticWeb.Common/   # Shared utilities
│   └── SwaLauncher/                         # SWA CLI launcher (.NET 9)
├── tests/
│   ├── redmuffin.Blazor.StaticWeb.Tests/
│   └── redmuffin.Blazor.StaticWeb.Api.Tests/
├── scripts/                                 # Build & deployment scripts
├── TestResults/                             # Test output
```

---

## Technology Stack

### Frontend Technologies

- **[Blazor WebAssembly](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)**  
  Framework for building interactive web UIs using C# instead of JavaScript. Enables client-side execution of .NET code in the browser.

- **[C# 12/13](https://learn.microsoft.com/en-us/dotnet/csharp/)**  
  Modern, object-oriented programming language with latest features including primary constructors, collection expressions, and ref readonly parameters.

- **[.NET 9](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)**  
  Cross-platform, high-performance framework for building modern applications with the latest features and performance improvements.

### Backend Technologies

- **[Azure Functions](https://azure.microsoft.com/en-us/products/functions)**  
  Serverless compute platform running on .NET 8 with HTTP triggers for RESTful API endpoints.

- **[Raindrop.io API](https://developer.raindrop.io/)**  
  External API integration with OAuth 2.0 authentication for bookmark management functionality.

### UI Framework & Styling

- **[Zurb Foundation](https://get.foundation/)**  
  Responsive front-end framework providing robust grid system, UI components, and accessibility features.

- **[SCSS (Sass)](https://sass-lang.com/documentation/syntax#scss)**  
  CSS preprocessor with variables, nesting, and modularization for modern styling capabilities.
  
  **Important:** All styling must be done through SCSS files located in `wwwroot/scss/`. CSS files in `wwwroot/css/` are automatically generated and should never be edited directly. Component-specific styles should use `.razor.css` files for scoped styling.
  
  **SCSS Partials:** All SCSS partial files must start with an underscore (_) and be imported into `app.scss` for automatic compilation. This ensures proper dependency management and build optimization.

- **[Blazored.LocalStorage](https://github.com/Blazored/LocalStorage)**  
  Blazor library for browser local storage access via JavaScript interop.

### Content & Utilities

- **[Markdig](https://github.com/xoofx/markdig)**  
  Fast, extensible Markdown processor for .NET with advanced extensions support.

- **[Microsoft.AspNetCore.WebUtilities](https://www.nuget.org/packages/Microsoft.AspNetCore.WebUtilities/)**  
  Utilities for web applications including query string parsing and URL manipulation.

### Testing Framework

- **[TUnit](https://github.com/thomhurst/TUnit)**  
  Modern, fast, and flexible .NET testing framework with parallel execution and comprehensive assertion library.

- **[NSubstitute](https://nsubstitute.github.io/)**  
  Friendly mocking library for .NET, used for creating test doubles and simplifying unit tests.

### Build and Analysis Tools

- **[LibMan (Library Manager)](https://learn.microsoft.com/en-us/aspnet/core/client-side/libman/)**  
  Lightweight client-side library acquisition tool for managing third-party libraries like Foundation.

- **[Roslynator Analyzers](https://github.com/JosefPihrt/Roslynator)**  
  Provides refactorings, analyzers, and fixes for improving code quality and maintainability.

- **[StyleCop Analyzers](https://github.com/DotNetAnalyzers/StyleCopAnalyzers)**  
  Enforces a set of style and consistency rules for C# code, ensuring adherence to coding standards.

- **[Meziantou Analyzers](https://github.com/meziantou/Meziantou.Analyzer)**  
  Offers additional code quality checks focused on performance, security, and best practices.

- **[VSThreading Analyzers](https://github.com/microsoft/vs-threading/tree/main/doc/analyzers)**  
  Ensures threading best practices are followed, especially for asynchronous programming.

- **[Coverlet](https://github.com/coverlet-coverage/coverlet)**  
  Cross-platform code coverage library for .NET, enabling comprehensive test coverage analysis with multiple output formats.

- **[ReportGenerator](https://github.com/danielpalme/ReportGenerator)**  
  Powerful tool for generating readable reports from code coverage data, supporting HTML, XML, and various other formats with historical tracking.

### Development Tools

- **[GitHub Copilot](https://github.com/features/copilot)**  
  AI-powered code completion tool with MCP server integration.

- **[EditorConfig](https://editorconfig.org/)**
  Consistent coding style definitions across different editors and IDEs.

- **[Directory.Build.props](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-your-build)**  
  Centralized MSBuild properties for consistent build configuration across all projects.

---

## Development Tools

The project includes comprehensive development tools and integrations to enhance productivity and code quality.

### Prerequisites System Requirements

- **Minimum RAM**: 8GB (16GB recommended)
- **Disk Space**: 10GB free space
- **OS**: Windows 10/11, macOS 10.15+, or Linux (Ubuntu 20.04+)
- **CPU**: x64 processor with SSE2 instruction set support

### Docker Integration

**Docker Desktop** is required for MCP server functionality:

- **Purpose**: Enables AI assistants to access external resources and documentation
- **Configured Servers**: GitHub, Fetch, Time, Brave Search, and Sequential Thinking servers (via Docker)
- **Installation**: [Download Docker Desktop](https://www.docker.com/products/docker-desktop/)
- **Configuration**: MCP servers are pre-configured in `.mcp.json`
- **Note**: Context7 uses HTTP endpoint, not Docker

---

## Build and Deployment

### Build Commands

**Build the entire solution:**
```bash
dotnet build
```

**Build specific projects:**
```bash
dotnet build src/redmuffin.Blazor.StaticWeb/
dotnet build src/redmuffin.Blazor.StaticWeb.Api/
```

**Run tests:**
```bash
dotnet test
```

### Code Coverage

The project includes comprehensive code coverage analysis using Coverlet and ReportGenerator:

**Generate Coverage Reports:**
```powershell
.\scripts\Generate-CoverageReport.ps1
```

**View Coverage Reports:**
```powershell
# View unified coverage report (default)
.\scripts\View-CoverageReport.ps1

# View branded coverage report with history
.\scripts\View-CoverageReport.ps1 -ReportType Branded

# View basic HTML coverage report
.\scripts\View-CoverageReport.ps1 -ReportType Html
```

**Coverage Features:**
- **Multiple Output Formats**: HTML, XML, JSON, and Cobertura formats
- **Unified Reports**: Combined coverage from both Blazor and API test projects
- **Historical Tracking**: Coverage trends over time with the branded report
- **Automated Exclusions**: Generated files, vendor libraries, and test projects automatically excluded
- **Threshold Configuration**: Configurable coverage thresholds for quality gates
- **Tool Integration**: Automatic installation of required tools (ReportGenerator)

**Coverage Configuration:**
- Coverage settings are configured in test project files (.csproj)
- Global exclusions are defined in Directory.Build.props
- Additional exclusions can be configured in .coverletrc
- Reports are generated in the `coverage/` directory

### Deployment

The project is configured for deployment to Azure Static Web Apps:

1. **Create an Azure Static Web App resource**
2. **Link the GitHub repository** to the Azure resource
3. **Configure build settings:**
   - App location: `src/redmuffin.Blazor.StaticWeb`
   - API location: `src/redmuffin.Blazor.StaticWeb.Api`
   - Output location: `wwwroot`
4. **Push changes** to the `master` branch to trigger automatic deployment

---

## License

This project is licensed under the [Unlicense](https://unlicense.org/).

---

## Acknowledgements

- [Markdig](https://github.com/xoofx/markdig) for Markdown processing
- [Zurb Foundation](https://get.foundation/) for the CSS framework
- [TUnit](https://github.com/thomhurst/TUnit) for the modern testing framework
- [NSubstitute](https://nsubstitute.github.io/) for mocking capabilities
- [Blazored.LocalStorage](https://github.com/Blazored/LocalStorage) for browser storage integration
- [GitHub Copilot](https://github.com/features/copilot) for AI code assistance
- [Visual Studio](https://visualstudio.microsoft.com/) for development environment

---

## Azure Functions Integration

The API project leverages **Azure Functions with .NET 8 Isolated Worker** to provide serverless compute capabilities. This integration enables scalable and event-driven backend functionality for the Blazor WebAssembly application.

### Key Features

- **Azure Functions Worker SDK (.NET 8)**: Isolated worker process for better performance and control
- **HTTP Triggers**: RESTful API endpoints with strong typing and dependency injection
- **OAuth Integration**: Secure token exchange for external API authentication
- **Dependency Injection**: Full DI container support with `IHttpClientFactory`, `ILogger`, and custom services

For more details, refer to the [Azure Functions Documentation](https://learn.microsoft.com/en-us/azure/azure-functions/).

---

## MCP Server Integration

The development environment includes several **Model Context Protocol (MCP) servers** that enhance AI-powered code assistance capabilities. These servers enable AI assistants (like GitHub Copilot) to access external resources, search capabilities, and up-to-date documentation.

### Available MCP Servers

#### Configured Servers
- **[GitHub MCP Server](https://github.com/github/github-mcp-server)**: Provides access to GitHub repositories and issues (requires Personal Access Token)
- **[Fetch MCP Server](https://github.com/modelcontextprotocol/servers/tree/main/src/fetch)**: Retrieves web content from URLs, automatically converting HTML to markdown for easier AI consumption
- **[Time MCP Server](https://github.com/modelcontextprotocol/servers/tree/main/src/time)**: Provides current time and date information
- **[Context7 MCP Server](https://github.com/upstash/context7)**: Fetches up-to-date documentation for libraries and frameworks via HTTP endpoint
- **[Brave Search MCP Server](https://github.com/modelcontextprotocol/servers/tree/main/src/brave-search)**: Provides real-time web search and local business search capabilities (requires API key)
- **[Sequential Thinking MCP Server](https://github.com/modelcontextprotocol/servers/tree/main/src/sequentialthinking)**: Enables structured, multi-step reasoning and problem-solving through dynamic thought processes

### Key Benefits

- **Real-Time Information**: Access current documentation, search results, and web content
- **Enhanced Problem Solving**: Structured reasoning and comprehensive resource access
- **Development Efficiency**: Reduce context switching by accessing external resources directly through AI
- **Up-to-Date Content**: Documentation that's more recent than AI training data cutoffs
- **Privacy-Focused**: Brave Search integration respects user privacy

### Configuration

MCP servers are pre-configured in the project's `.mcp.json` file and integrate seamlessly with GitHub Copilot when Docker Desktop is available. Some servers may require API keys (like Brave Search) for full functionality.

### Usage Examples

Once configured, you can ask your AI assistant to:

- "Fetch the latest Blazor WebAssembly documentation"
- "Search for recent .NET 9 performance improvements"
- "Get current Azure Functions examples using Context7"
- "Help me think through this architecture problem step by step"

### Security Considerations

- Fetch MCP Server can access local/internal IP addresses - ensure proper network security policies in corporate environments
- All servers respect standard web protocols (robots.txt, user-agent settings)
- Configuration is managed through your AI assistant's settings

---

## Local Development: Visual Studio Multi-Project Startup

The project is configured for seamless development using Visual Studio's multi-project startup feature, which automatically launches all required components.

### Quick Start

1. **Start the development environment:**
   - Open `redmuffin.Blazor.StaticWeb.sln` in Visual Studio 2022
   - Use the "Start both" profile (or similar multi-project startup configuration)
   - Visual Studio will automatically start:
     - Blazor WebAssembly frontend
     - Azure Functions API backend
     - SwaLauncher (which starts the Azure Static Web Apps emulator)

2. **Access the application:**
   - Open `http://localhost:4280` in your browser
   - All API calls will be routed through the same port as the web app
   - OAuth redirects and authentication flows will work correctly

### Development Features

- **Hot Reload**: Changes to Blazor components and API functions are automatically reflected
- **Unified Routing**: Single port for both frontend and API eliminates CORS issues
- **Production Simulation**: Mimics the exact Azure Static Web Apps runtime environment
- **Debugging Support**: Full debugging capabilities for both frontend and backend code

### Notes

- The CLI simulates the Azure Static Web Apps environment, making it ideal for development and testing
- The "Start both" profile in Visual Studio simplifies launching both projects together
- OAuth flows and API integration work seamlessly in this local development setup
