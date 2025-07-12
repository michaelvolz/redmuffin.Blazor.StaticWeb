# redmuffin.Blazor.StaticWeb (preview - alpha)

**FOR HUMAN DEVELOPERS ONLY** - If you are an AI code assistant, please refer to `.github/copilot-instructions.md` instead of this file for technical guidelines and project information.

[![Build Status](https://github.com/michaelvolz/redmuffin.Blazor.StaticWeb/actions/workflows/azure-static-web-apps-lively-cliff-0945be603.yml/badge.svg)](https://github.com/michaelvolz/redmuffin.Blazor.StaticWeb/actions/workflows/azure-static-web-apps-lively-cliff-0945be603.yml)
[![CodeQL](https://github.com/michaelvolz/redmuffin.Blazor.StaticWeb/actions/workflows/codeql.yml/badge.svg)](https://github.com/michaelvolz/redmuffin.Blazor.StaticWeb/actions/workflows/codeql.yml)
[![Last Commit (master)](https://img.shields.io/github/last-commit/michaelvolz/redmuffin.Blazor.StaticWeb/master.svg)](https://github.com/michaelvolz/redmuffin.Blazor.StaticWeb/commits/master)
[![License: Unlicense](https://img.shields.io/badge/license-Unlicense-blue.svg)](https://en.wikipedia.org/wiki/Unlicense)
[![Dependabot enabled](https://img.shields.io/badge/Dependabot-enabled-blue.svg)](https://docs.github.com/en/code-security/dependabot/working-with-dependabot)
![GitHub language count](https://img.shields.io/github/languages/count/michaelvolz/redmuffin.Blazor.StaticWeb)
![GitHub code size in bytes](https://img.shields.io/github/languages/code-size/michaelvolz/redmuffin.Blazor.StaticWeb)
[![.NET 9](https://img.shields.io/badge/.NET-9-blueviolet?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

---

## Overview

**redmuffin.Blazor.StaticWeb** is a modern full-stack web application built with Blazor WebAssembly (.NET 9) and Azure Functions (.NET 8). The solution provides a performant, maintainable static web application with serverless backend capabilities, featuring OAuth integration, real-time performance monitoring, and comprehensive testing infrastructure.

---

## Table of Contents

- [Features](#features)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Usage](#usage)
- [Project Structure](#project-structure)
- [Technology Stack](#technology-stack)
- [Contributing](#contributing)
- [Build and Deployment](#build-and-deployment)
- [License](#license)
- [Acknowledgements](#acknowledgements)
- [Azure Functions Integration](#azure-functions-integration)
- [Azure CLI Dependency](#azure-cli-dependency)
- [Azure Static Web Apps CLI Dependency](#azure-static-web-apps-cli-dependency)
- [Fetch MCP Server Integration](#fetch-mcp-server-integration)
- [Brave Search MCP Server Integration](#brave-search-mcp-server-integration)
- [Context7 MCP Server Integration](#context7-mcp-server-integration)
- [Local Development: Visual Studio Multi-Project Startup](#local-development-visual-studio-multi-project-startup)

---

## Features

### Core Functionality
- **Blazor WebAssembly (.NET 9)** - Client-side execution with modern C# features
- **Azure Functions (.NET 8)** - Serverless backend with HTTP triggers
- **Raindrop.io OAuth Integration** - External API integration with secure authentication
- **Real-time Performance Monitoring** - Page load speed tracking and display
- **Markdown Content Rendering** - Advanced Markdown processing with Markdig

### Development & Quality
- **Modern C# (C# 12/13) features** - Primary constructors, collection expressions, ref readonly parameters
- **Comprehensive Testing** - TUnit framework with NSubstitute mocking
- **Code Coverage** - Automated coverage reports with Coverlet and ReportGenerator
- **PowerShell Automation** - Scripts for coverage report generation and viewing
- **SCSS/CSS Styling** - Modern styling with Zurb Foundation framework
- **Feature Folder Structure** - Organized by feature for better maintainability
- **Code Quality & Security** - CodeQL analysis, automated builds, Dependabot integration
- **Accessibility Compliance** - WCAG 2.1 AA standards with semantic HTML and ARIA support

### Infrastructure & Tools
- **EditorConfig** - Consistent code style and formatting
- **Directory.Build.props** - Centralized project configuration
- **Docker Integration** - Required for GitHub Copilot MCP server functionality
- **Azure Static Web Apps** - Deployment and hosting platform

---

## Prerequisites

### Required Software
- **Visual Studio 2022** (17.8 or later) with the following workloads:
  - ASP.NET and web development
- **.NET 9 SDK** - For Blazor WebAssembly project
- **.NET 8 SDK** - For Azure Functions project
- **Node.js** (Latest LTS) - Required for Azure Static Web Apps CLI
- **Docker Desktop**  
  [Download from Docker website](https://www.docker.com/products/docker-desktop/)
  - Required for GitHub Copilot MCP server functionality

### Global Tools
- **Azure Static Web Apps CLI**  
  Install the Azure Static Web Apps CLI globally: 

  `npm install -g @azure/static-web-apps-cli`

### Optional Tools
- **Azure CLI** - For Azure resource management and deployment

---

## Getting Started

1. **Clone the repository:** 

   `git clone https://github.com/michaelvolz/redmuffin.Blazor.StaticWeb.git`
  
   `cd redmuffin.Blazor.StaticWeb`

2. **Install prerequisites:**
   - Ensure Visual Studio 2022, .NET 9 SDK, and .NET 8 SDK are installed
   - Install global tools: 

    `npm install -g @azure/static-web-apps-cli`
 
3. **Open the solution:**
   - Open `redmuffin.Blazor.StaticWeb.sln` in Visual Studio 2022

4. **Restore and build:** 

   `dotnet restore`

   `dotnet build`

5. **Run tests:** `dotnet test`

---

## Project Structure

The project follows a [feature folder structure](https://dev.to/smotastic/layer-vs-feature-architecture-3cko) to organize code by feature rather than by technical layer. This approach improves maintainability and scalability by grouping related components, services, and assets together.

<pre>
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
</pre>

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

- **[Code Analyzers](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview)**  
  Static code analysis tools integrated into the build process for code quality, security, and maintainability.

- **[Coverlet](https://github.com/coverlet-coverage/coverlet)**  
  Cross-platform code coverage library for .NET, enabling comprehensive test coverage analysis with multiple output formats.

- **[ReportGenerator](https://github.com/danielpalme/ReportGenerator)**  
  Powerful tool for generating readable reports from code coverage data, supporting HTML, XML, and various other formats with historical tracking.

### Development Tools

- **[GitHub Copilot](https://github.com/features/copilot)**  
  AI-powered code completion tool with MCP server integration via Docker Desktop.

- **[Fetch MCP Server](https://github.com/modelcontextprotocol/servers/tree/main/src/fetch)**  
  Model Context Protocol server that enables AI assistants to fetch and process web content from URLs, automatically converting HTML to markdown for easier consumption.

- **[Brave Search MCP Server](https://github.com/modelcontextprotocol/servers/tree/main/src/brave-search)**  
  Model Context Protocol server that provides AI assistants with real-time web search and local business search capabilities through Brave's privacy-focused search API.

- **[Context7 MCP Server](https://github.com/upstash/context7)**
  Provides up-to-date documentation for libraries and frameworks, allowing AI assistants to fetch relevant code examples and documentation directly.

- **[Sequential Thinking MCP Server](https://github.com/modelcontextprotocol/servers/tree/main/src/sequentialthinking)**
  Enables structured step-by-step reasoning and problem-solving through a dynamic thinking process that can adapt and evolve as understanding deepens.

- **[EditorConfig](https://editorconfig.org/)**  
  Consistent coding style definitions across different editors and IDEs.

- **[Directory.Build.props](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-your-build)**  
  Centralized MSBuild properties for consistent build configuration across all projects.

---

## Build and Deployment

### Build Commands

Build the entire solution:dotnet build
Build specific projects:dotnet build src/redmuffin.Blazor.StaticWeb/
dotnet build src/redmuffin.Blazor.StaticWeb.Api/
Run tests:dotnet test

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
4. **Push changes** to the `main` branch to trigger automatic deployment

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

## Azure Static Web Apps CLI Dependency

The project requires **Azure Static Web Apps CLI** for local development and testing of Azure Static Web Apps. This tool simulates the production environment locally.

### Installation

To install Azure Static Web Apps CLI, use the following command:npm install -g @azure/static-web-apps-cli
For more details, refer to the [Azure Static Web Apps CLI Documentation](https://learn.microsoft.com/en-us/azure/static-web-apps/cli).

---

## Fetch MCP Server Integration

The development environment includes **Fetch MCP Server** integration to enhance AI-powered code assistance capabilities. This Model Context Protocol server enables AI assistants (like GitHub Copilot) to fetch and process web content from URLs, providing real-time access to documentation, APIs, and web resources.

### Key Capabilities

- **Web Content Fetching**: Retrieve content from any publicly accessible URL
- **Format Conversion**: Automatically converts HTML to markdown for easier AI consumption
- **Multiple Content Types**: Supports HTML, JSON, Markdown, and plain text formats
- **Content Processing**: Handles content truncation and pagination for large documents
- **Automatic Detection**: Intelligently detects content types and applies appropriate processing

### Configuration

The Fetch MCP Server is pre-configured in the project's `.mcp.json` file and ready for use with compatible AI assistants.

### Integration with GitHub Copilot

The Fetch MCP Server integrates seamlessly with GitHub Copilot in Visual Studio Code, enabling the AI assistant to:

- Fetch API documentation from official sources
- Retrieve code examples and tutorials from web resources
- Access real-time information from documentation websites
- Pull content from GitHub repositories and wikis
- Process web-based configuration files and specifications

### Usage Examples

Once configured, you can ask your AI assistant to:

- "Fetch the latest documentation for Blazor WebAssembly routing"
- "Get the current API specification from the Azure Functions documentation"
- "Retrieve the setup instructions from the TUnit GitHub repository"
- "Fetch examples of SCSS usage with Foundation framework"

### Configuration

The Fetch MCP Server configuration is typically managed through your AI assistant's settings. For GitHub Copilot integration, the server runs automatically when Docker Desktop is available and properly configured.

### Security Considerations

- The server can access local/internal IP addresses and may represent a security risk in some environments
- Ensure proper network security policies are in place when using in corporate environments
- The server respects robots.txt files and includes configurable user-agent settings

For more details, refer to the [Fetch MCP Server Documentation](https://github.com/modelcontextprotocol/servers/tree/main/src/fetch).

---

## Brave Search MCP Server Integration

The development environment includes **Brave Search MCP Server** integration to provide AI assistants with real-time web search capabilities. This Model Context Protocol server enables AI assistants (like GitHub Copilot) to perform web searches and local business searches using Brave's privacy-focused search API.

### Key Capabilities

- **Web Search**: General web searches for current information, news, and documentation
- **Local Business Search**: Find local businesses, services, and points of interest
- **Privacy-Focused**: Uses Brave's privacy-respecting search engine
- **Real-Time Results**: Access to current information and recent developments
- **Smart Fallbacks**: Automatically falls back to web search if local results are unavailable
- **Pagination Support**: Handle large result sets with built-in pagination

### Configuration

The Brave Search MCP Server configuration can be added to the project's `.mcp.json` file. This server requires a Brave Search API key for operation. The free tier provides 2,000 queries per month, perfect for development and testing.

### Integration with GitHub Copilot

The Brave Search MCP Server integrates seamlessly with GitHub Copilot in Visual Studio Code, enabling the AI assistant to:

- Search for the latest technology updates and releases
- Find current best practices and coding patterns
- Access real-time information about frameworks and libraries
- Discover recent blog posts and tutorials
- Look up API changes and breaking changes
- Find local development resources and services

### Usage Examples

Once configured, you can ask your AI assistant to:

- "Search for the latest .NET 9 performance improvements"
- "Find current Blazor WebAssembly best practices"
- "Look up recent Azure Functions updates"
- "Search for TUnit testing framework examples"
- "Find local developer meetups in my area"

### Configuration

The Brave Search MCP Server configuration is typically managed through your AI assistant's settings. For GitHub Copilot integration, configure the server with your API key when Docker Desktop is available.

### Search Tools Available

- **brave_web_search**: General web search with customizable result count (max 20) and pagination
- **brave_local_search**: Local business and service search with automatic web search fallback

### Benefits for Development

- **Stay Current**: Access to the latest information about technologies and frameworks
- **Research Efficiency**: Quickly find relevant documentation and examples
- **Problem Solving**: Search for solutions to current issues and error messages
- **Learning**: Discover new techniques and best practices
- **Privacy-Focused**: Search without tracking or data collection concerns

For more details, refer to the [Brave Search MCP Server Documentation](https://github.com/modelcontextprotocol/servers/tree/main/src/brave-search).

---

## Context7 MCP Server Integration

The development environment includes **Context7 MCP Server** integration to provide AI assistants with access to up-to-date documentation for libraries and frameworks. This Model Context Protocol server enables AI assistants (like GitHub Copilot) to fetch current documentation, code examples, and API references directly from authoritative sources.

### Key Capabilities

- **Up-to-Date Documentation**: Fetches current documentation instead of relying on outdated training data
- **Version-Specific Content**: Access documentation for specific versions of libraries and frameworks
- **Code Examples**: Retrieve relevant code examples and usage patterns
- **Library Resolution**: Automatically resolves common library names to Context7-compatible IDs
- **Multi-Language Support**: Supports documentation for various programming languages and frameworks
- **Smart Ranking**: Intelligent project ranking and customizable token limits

### Configuration

The Context7 MCP Server is pre-configured in the project's `.mcp.json` file as an HTTP-based service and ready for use with compatible AI assistants.

### Integration with GitHub Copilot

The Context7 MCP Server integrates seamlessly with GitHub Copilot in Visual Studio Code, enabling the AI assistant to:

- Access current documentation for .NET, Blazor, and Azure technologies
- Fetch up-to-date API references and code examples
- Retrieve version-specific documentation for frameworks and libraries
- Get current best practices and implementation patterns
- Access documentation that's more recent than training data cutoffs

### Usage Examples

Once configured, you can ask your AI assistant to:

- "Get the latest Blazor WebAssembly documentation using Context7"
- "Fetch current .NET 9 API documentation"
- "Use Context7 to get TUnit testing framework examples"
- "Get Azure Functions documentation for .NET 8"
- "Fetch Foundation CSS framework documentation"

### Available Tools

- **resolve-library-id**: Resolves a general library name into a Context7-compatible library ID
- **get-library-docs**: Fetches documentation for a library using a Context7-compatible library ID

### Configuration

The Context7 MCP Server configuration is typically managed through your AI assistant's settings. For GitHub Copilot integration, the server runs automatically when Docker Desktop is available and properly configured.

### Workflow

1. **Library Detection**: Context7 automatically detects mentioned libraries and frameworks
2. **ID Resolution**: Uses `resolve-library-id` to find the correct Context7 library identifier
3. **Documentation Fetch**: Retrieves current documentation using `get-library-docs`
4. **Context Injection**: Injects relevant documentation directly into the AI prompt

### Benefits for Development

- **Current Information**: Always access the latest documentation and examples
- **Reduced Research Time**: Get documentation without leaving your development environment
- **Better Code Quality**: Access to current best practices and implementation patterns
- **Version Accuracy**: Get documentation that matches your project's dependency versions
- **Multi-Framework Support**: Works with .NET, JavaScript, Python, and many other ecosystems

For more details, refer to the [Context7 MCP Server Documentation](https://github.com/upstash/context7).

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
