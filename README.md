# redmuffin.Blazor.StaticWeb (preview - alpha)

[![Build Status](https://github.com/michaelvolz/redmuffin.Blazor.Static/actions/workflows/azure-static-web-apps-lively-cliff-0945be603.yml/badge.svg)](https://github.com/michaelvolz/redmuffin.Blazor.Static/actions/workflows/azure-static-web-apps-lively-cliff-0945be603.yml)
[![CodeQL](https://github.com/michaelvolz/redmuffin.Blazor.Static/actions/workflows/codeql.yml/badge.svg)](https://github.com/michaelvolz/redmuffin.Blazor.Static/actions/workflows/codeql.yml)
[![Last Commit (master)](https://img.shields.io/github/last-commit/michaelvolz/redmuffin.Blazor.Static/master.svg)](https://github.com/michaelvolz/redmuffin.Blazor.Static/commits/master)

[![License: Unlicense](https://img.shields.io/badge/license-Unlicense-blue.svg)](https://en.wikipedia.org/wiki/Unlicense)
[![Dependabot enabled](https://img.shields.io/badge/Dependabot-enabled-blue.svg)](https://docs.github.com/en/code-security/dependabot/working-with-dependabot)
![GitHub language count](https://img.shields.io/github/languages/count/michaelvolz/OpenGraphTilemakerReborn)
![GitHub code size in bytes](https://img.shields.io/github/languages/code-size/michaelvolz/OpenGraphTilemakerReborn)
[![.NET 9](https://img.shields.io/badge/.NET-9-blueviolet?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

---

## Overview

**redmuffin.Blazor.StaticWeb** is a Blazor WebAssembly project targeting .NET 9, designed for building modern, performant, and maintainable static web applications. The solution leverages [Markdig](https://github.com/xoofx/markdig) for Markdown integration and follows best practices for accessibility, security, and maintainability.

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

- Blazor WebAssembly (.NET 9)
- Modern C# (C# 12/13) features
- Markdown rendering via Markdig
- SCSS/CSS asset pipeline
- Automated builds and code analysis
- Dependabot integration
- Feature folder structure for project organization
- CodeQL-enabled for advanced code security analysis
- EditorConfig for consistent code style and formatting
- Directory.Build.props for centralized project configuration
- Unit testing with TUnit

---

## Prerequisites

- **Visual Studio 2022** (17.8 or later) with the following workloads:
  - ASP.NET and web development
- **.NET 9 SDK**
- **Docker Desktop**  
  [Download from Docker website](https://www.docker.com/products/docker-desktop/)
  - Required for GitHub Copilot MCP server functionality
- **Excubo.WebCompiler**  
  Install the Excubo WebCompiler as a global .NET tool using the following command: 
  `dotnet tool install -g Excubo.WebCompiler`  
  - Required for compiling SCSS files to CSS.
  - The script `scripts/compile-webcompiler.ps1` can be used manually if needed.
  - In debug mode, SCSS compilation will automatically run on every .NET compilation during development.

---

## Getting Started

1. **Clone the repository:** `git clone https://github.com/michaelvolz/redmuffin.Blazor.Static.git`
2. **Install prerequisites:**
   - Ensure Visual Studio 2022 and .NET 9 SDK are installed.
   - Install the Excubo WebCompiler as a global .NET tool using the command:
            
     `dotnet tool install -g Excubo.WebCompiler`

3. **Open the solution:**
   - Open `redmuffin.Blazor.StaticWeb.sln` in Visual Studio 2022.

4. **Restore and build:** Via Visual Studio or the command `dotnet build`
---

## Project Structure

The project follows a [feature folder structure](https://dev.to/smotastic/layer-vs-feature-architecture-3cko) to organize code by feature rather than by technical layer. This approach improves maintainability and scalability by grouping related components, services, and assets together.

---

## Technology Stack

### Core Technologies

- **[C#](https://learn.microsoft.com/en-us/dotnet/csharp/)**  
  A modern, object-oriented programming language developed by Microsoft. The project utilizes the latest C# 12/13 features for concise, robust, and maintainable code.

- **[Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)**  
  A framework for building interactive web UIs using C# instead of JavaScript. This project uses Blazor WebAssembly, enabling client-side execution of .NET code in the browser.

- **[.NET Core / .NET 9](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)**  
  A cross-platform, high-performance framework for building modern cloud, web, and desktop applications. The project targets .NET 9 for the latest features and performance improvements.

### Frontend and Styling

- **[SCSS (Sass)](https://sass-lang.com/documentation/syntax#scss)**  
  A powerful CSS preprocessor that adds variables, nesting, and modularization to CSS. SCSS is compiled to standard CSS using Excubo WebCompiler for maintainable and scalable stylesheets.

- **[Zurb Foundation](https://get.foundation/)**  
  A responsive front-end framework providing a robust grid system, UI components, and accessibility features. Used as the primary CSS framework for consistent and accessible design.

### Markdown and Rendering

- **[Markdig Renderer](https://github.com/xoofx/markdig)**  
  A fast, extensible Markdown processor for .NET. Markdig is used to render Markdown content within the application.

### Build and Analysis Tools

- **[LibMan (Library Manager)](https://learn.microsoft.com/en-us/aspnet/core/client-side/libman/)**  
  A lightweight, client-side library acquisition tool for web projects. LibMan is used to manage third-party client-side libraries such as Foundation.

- **[Code Analyzer](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview)**  
  Static code analysis tools integrated into the build process to enforce code quality, security, and maintainability standards.

- **[Excubo WebCompiler](https://github.com/excubo-ag/WebCompiler)**  
  A .NET tool for compiling SCSS, LESS, and other preprocessor files into CSS. Ensures that styles are always up to date and optimized.

### Testing and Coverage

- **[TUnit](https://tunit.net/)**  
  A free, open-source, community-focused unit testing tool for .NET. Used for testing the application.
- **[NSubstitute](https://nsubstitute.github.io/)**  
  A friendly mocking library for .NET, used for creating test doubles and simplifying unit tests.

### Development Tools

- **[GitHub Copilot](https://github.com/features/copilot)**  
  An AI-powered code completion tool that assists with writing code, documentation, and tests, improving productivity and code quality.
  - Requires Docker Desktop for MCP server functionality

- **[EditorConfig](https://editorconfig.org/)**  
  A file-based format for defining and maintaining consistent coding styles between different editors and IDEs.

- **[Directory.Build.props](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-your-build?view=vs-2022#directorybuildprops-and-directorybuildtargets)**  
  Centralizes common MSBuild properties for all projects in the repository, ensuring consistent build configuration.

---

## Build and Deployment

### Build

- Use the following commands to build the project: `dotnet build`
### Deployment

- The project is configured for deployment to Azure Static Web Apps. Follow these steps:
  1. Create an Azure Static Web App resource.
  2. Link the GitHub repository to the Azure resource.
  3. Push changes to the `main` branch to trigger deployment.

---

## License

This project is licensed under the [Unlicense](https://unlicense.org/).

---

## Acknowledgements

- [Markdig](https://github.com/xoofx/markdig) for Markdown processing
- [Excubo WebCompiler](https://github.com/excubo-ag/WebCompiler) for SCSS/LESS compilation
- [Zurb Foundation](https://get.foundation/) for the CSS framework
- [GitHub Copilot](https://github.com/features/copilot) for AI code assistance
- [Visual Studio](https://visualstudio.microsoft.com/) for development environment

---

## Azure Functions Integration

The API project leverages **Azure Functions** to provide serverless compute capabilities. This integration enables scalable and event-driven backend functionality for the Blazor WebAssembly application.

### Key Features

- **Azure Functions Worker SDK**: Provides the runtime for executing Azure Functions.
- **HTTP Trigger**: Enables RESTful API endpoints for client-server communication.
- **Application Insights**: Integrated for monitoring and diagnostics.

### Example Function
[Function("HelloWorld")]
public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
{
    _logger.LogInformation("C# HTTP trigger function processed a request.");
    return new OkObjectResult("Welcome to Azure Functions!");
}
### Documentation

For more details, refer to the [Azure Functions Documentation](https://learn.microsoft.com/en-us/azure/azure-functions/).

---

## Azure Static Web Apps CLI Dependency

The project requires **Azure Static Web Apps CLI** for local development and testing of Azure Static Web Apps. Ensure that the CLI is installed globally on your system.

### Installation

To install Azure Static Web Apps CLI, use the following command:
npm install -g @azure/static-web-apps-cli
For more details, refer to the [Azure Static Web Apps CLI Documentation](https://learn.microsoft.com/en-us/azure/static-web-apps/cli).

---

## Local Development: Starting the Project with Azure Static Web Apps CLI

To simulate the Azure Static Web Apps environment locally, follow these steps:

1. **Start the Azure Static Web Apps CLI:**
   - Use the following command to start the CLI and connect the frontend and API:
     
     `swa start http://localhost:5233 --api-location http://localhost:7184/api`

   - This command will:
     - Proxy the frontend at `http://localhost:4280`.
     - Proxy API calls to the backend, enabling seamless integration.
   - The CLI will automatically poll the frontend and API until they are online, making development easier. It can run continuously, even if you restart the frontend or API during development.

2. **Start the Blazor WebAssembly frontend and API backend:**
   - Use the "Start both" profile in Visual Studio to launch the frontend and API simultaneously.
   - The browser will automatically redirect to `http://localhost:4280` if necessary.

3. **Access the application:**
   - Open `http://localhost:4280` in your browser manually to view the application if needed.
   - All API calls will be routed through the same port as the web app.

### Notes
- The CLI simulates the Azure Static Web Apps environment, making it ideal for development and testing.
- The "Start both" profile simplifies launching both projects together.
