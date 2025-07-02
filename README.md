# redmuffin.Blazor.StaticWeb (preview - alpha)

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
- [Local Development: Starting the Project with Azure Static Web Apps CLI](#local-development-starting-the-project-with-azure-static-web-apps-cli)

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
- **SCSS/CSS Asset Pipeline** - Automated compilation and optimization
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
- **Excubo.WebCompiler**  
  Install the Excubo WebCompiler as a global .NET tool using the following command: dotnet tool install -g Excubo.WebCompiler  - Required for compiling SCSS files to CSS
  - The script `scripts/compile-webcompiler.ps1` can be used manually if needed
  - In debug mode, SCSS compilation will automatically run on every .NET compilation during development

- **Azure Static Web Apps CLI**  
  Install the Azure Static Web Apps CLI globally:npm install -g @azure/static-web-apps-cli
### Optional Tools
- **Azure CLI** - For Azure resource management and deployment

---

## Getting Started

1. **Clone the repository:**git clone https://github.com/michaelvolz/redmuffin.Blazor.StaticWeb.git
cd redmuffin.Blazor.StaticWeb
2. **Install prerequisites:**
   - Ensure Visual Studio 2022, .NET 9 SDK, and .NET 8 SDK are installed
   - Install global tools: ```bash
 dotnet tool install -g Excubo.WebCompiler
     npm install -g @azure/static-web-apps-cli
 ```
3. **Open the solution:**
   - Open `redmuffin.Blazor.StaticWeb.sln` in Visual Studio 2022

4. **Restore and build:**dotnet restore
dotnet build
5. **Run tests:**dotnet test
---

## Project Structure

The project follows a [feature folder structure](https://dev.to/smotastic/layer-vs-feature-architecture-3cko) to organize code by feature rather than by technical layer. This approach improves maintainability and scalability by grouping related components, services, and assets together.
redmuffin.Blazor.StaticWeb/
??? .github/
?   ??? Documentation.instructions.md
?   ??? copilot-instructions.md
?   ??? prompts/
??? src/
?   ??? redmuffin.Blazor.StaticWeb/          # Blazor WebAssembly (.NET 9)
?   ?   ??? Features/
?   ?   ?   ??? Pages/
?   ?   ?   ??? Shared/Components/
?   ?   ??? wwwroot/
?   ??? redmuffin.Blazor.StaticWeb.Api/      # Azure Functions (.NET 8)
?   ?   ??? Functions/
?   ??? redmuffin.Blazor.StaticWeb.Common/   # Shared utilities
??? tests/
?   ??? redmuffin.Blazor.StaticWeb.Tests/
?   ??? redmuffin.Blazor.StaticWeb.Api.Tests/
??? scripts/
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
  CSS preprocessor with variables, nesting, and modularization, compiled using Excubo WebCompiler.

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

- **[Excubo WebCompiler](https://github.com/excubo-ag/WebCompiler)**  
  .NET tool for compiling SCSS, LESS, and other preprocessor files into optimized CSS.

- **[LibMan (Library Manager)](https://learn.microsoft.com/en-us/aspnet/core/client-side/libman/)**  
  Lightweight client-side library acquisition tool for managing third-party libraries like Foundation.

- **[Code Analyzers](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview)**  
  Static code analysis tools integrated into the build process for code quality, security, and maintainability.

### Development Tools

- **[GitHub Copilot](https://github.com/features/copilot)**  
  AI-powered code completion tool with MCP server integration via Docker Desktop.

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
- [Excubo WebCompiler](https://github.com/excubo-ag/WebCompiler) for SCSS/LESS compilation
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
- **Application Insights**: Integrated monitoring, logging, and diagnostics
- **Dependency Injection**: Full DI container support with `IHttpClientFactory`, `ILogger`, and custom services

### Example Function (Current Architecture)
[Function("ExchangeRaindropCode")]
public async Task<HttpResponseData> RunAsync(
    [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
    CancellationToken cancellationToken = default)
{
    // Modern .NET 8 isolated worker implementation
    // with dependency injection and structured logging
    LogFunctionProcessed(_logger);
    
    // Implementation with proper error handling,
    // input validation, and secure token exchange
    return response;
}
### Documentation

For more details, refer to the [Azure Functions Documentation](https://learn.microsoft.com/en-us/azure/azure-functions/).

---

## Azure Static Web Apps CLI Dependency

The project requires **Azure Static Web Apps CLI** for local development and testing of Azure Static Web Apps. This tool simulates the production environment locally.

### Installation

To install Azure Static Web Apps CLI, use the following command:npm install -g @azure/static-web-apps-cli
For more details, refer to the [Azure Static Web Apps CLI Documentation](https://learn.microsoft.com/en-us/azure/static-web-apps/cli).

---

## Local Development: Starting the Project with Azure Static Web Apps CLI

To simulate the Azure Static Web Apps environment locally, follow these steps:

### Quick Start

1. **Start the Azure Static Web Apps CLI:**swa start http://localhost:5233 --api-location http://localhost:7184/api   
   This command will:
   - Proxy the frontend at `http://localhost:4280`
   - Proxy API calls to the backend, enabling seamless integration
   - Automatically poll both services until they are online
   - Provide hot reload capabilities during development

2. **Start the Blazor WebAssembly frontend and API backend:**
   - Use the "Start both" profile in Visual Studio to launch both projects simultaneously
   - Alternatively, start each project manually: ```bash
 # Terminal 1 - Start Blazor WebAssembly
 dotnet run --project src/redmuffin.Blazor.StaticWeb/
 
 # Terminal 2 - Start Azure Functions
 dotnet run --project src/redmuffin.Blazor.StaticWeb.Api/
 ```
3. **Access the application:**
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
