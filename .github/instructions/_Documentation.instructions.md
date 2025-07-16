# Documentation Resources for redmuffin.Blazor.StaticWeb

This document provides a comprehensive guide to documentation resources for the technologies used in this project, both available through Context7 and official sources.

## Overview

This project is built using:
- **Frontend**: Blazor WebAssembly (.NET 9)
- **Backend**: Azure Functions (.NET 8) 
- **Testing**: TUnit framework with NSubstitute mocking
- **Styling**: Zurb Foundation CSS framework
- **External API**: Raindrop.io OAuth integration
- **Build Tools**: SCSS compilation, EditorConfig
- **Development**: Visual Studio 2022, Docker Desktop

## Context7 Available Documentation

Based on exploration of Context7 MCP Server, the following documentation libraries are available:

### ? Available in Context7

| Technology | Library ID | Code Snippets | Trust Score | Notes |
|------------|------------|---------------|-------------|--------|
| **TUnit Testing** | `/thomhurst/tunit` | 2,065 | 9.7 | Modern .NET testing framework |
| **NSubstitute** | `/nsubstitute/nsubstitute` | 132 | N/A | .NET mocking library |
| **OAuth2** | `/panva/oauth4webapi` | 58 | 9.3 | Low-level OAuth2 client API |
| **OAuth2** | `/context7/pkg_go_dev-golang.org-x-oauth2` | 3,263 | 10.0 | Go OAuth2 implementation (reference) |
| **OAuth2** | `/context7/docs_goauthentik_io-docs` | 13,607 | 8.0 | authentik OAuth/SSO documentation |
| **Azure Functions** | `/azure/azure-functions-host` | 11 | 9.6 | Azure Functions host/runtime |
| **Blazor Components** | `/dotnet/blazor-samples` | 202 | 8.3 | Official Blazor samples |
| **Blazorise** | `/megabit/blazorise` | 1,434 | 6.7 | Blazor component library |
| **MudBlazor** | `/mudblazor/mudblazor` | 35 | 7.8 | Material Design Blazor components |
| **Microsoft Fluent UI Blazor** | `/microsoft/fluentui-blazor` | 113 | 9.9 | Microsoft's official Blazor UI |
| **Telerik UI for Blazor** | `/telerik/blazor-docs` | 3,967 | 9.2 | Commercial Blazor components |
| **Syncfusion Blazor** | `/context7/blazor_syncfusion_com-documentation-introduction` | 2,943 | 9.0 | Enterprise Blazor components |
| **.NET Documentation** | `/dotnet/docs` | 35,504 | 8.3 | Comprehensive .NET docs |
| **.NET Extensions** | `/dotnet/extensions` | 361 | 8.3 | Production libraries suite |
| **.NET MAUI** | `/dotnet/maui` | 5,241 | 8.3 | Multi-platform development |
| **Foundation CSS** | `/foundation/foundation-sites` | 885 | 9.1 | Responsive front-end framework |
| **Foundation Docs** | `/foundation/foundation-docs` | 28 | 9.1 | Foundation documentation tools |

### ? Context7 Limitations Found

During exploration, several documentation libraries were indexed but returned "Documentation not found or not finalized" errors:
- Most specific library documentation requests failed to return content
- This appears to be a current limitation of the Context7 system
- Libraries are indexed but content may not be fully accessible

## Official Documentation Resources

Since Context7 access is limited, here are the primary official documentation sources:

### Core Technologies

#### Blazor WebAssembly (.NET 9)
- **Official Docs**: https://docs.microsoft.com/en-us/aspnet/core/blazor/
- **WebAssembly Specific**: https://docs.microsoft.com/en-us/aspnet/core/blazor/hosting-models#blazor-webassembly
- **JavaScript Interop**: https://docs.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/
- **Performance**: https://docs.microsoft.com/en-us/aspnet/core/blazor/performance
- **API**: https://docs.microsoft.com/en-us/dotnet/api/?view=aspnetcore-9.0

#### Azure Functions (.NET 8)
- **Official Docs**: https://docs.microsoft.com/en-us/azure/azure-functions/
- **HTTP Triggers**: https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook
- **Testing**: https://docs.microsoft.com/en-us/azure/azure-functions/functions-test-a-function
- **Best Practices**: https://docs.microsoft.com/en-us/azure/azure-functions/functions-best-practices
- **Local Development**: https://docs.microsoft.com/en-us/azure/azure-functions/functions-develop-local

### Testing Framework

#### TUnit
- **GitHub**: https://github.com/thomhurst/TUnit
- **Wiki**: https://github.com/thomhurst/TUnit/wiki
- **Getting Started**: https://github.com/thomhurst/TUnit/wiki/Getting-Started
- **Attributes**: https://github.com/thomhurst/TUnit/wiki/Test-Attributes
- **Assertions**: https://github.com/thomhurst/TUnit/wiki/Assertions

#### NSubstitute
- **Official Site**: https://nsubstitute.github.io/
- **Getting Started**: https://nsubstitute.github.io/help/getting-started/
- **Creating Substitutes**: https://nsubstitute.github.io/help/creating-a-substitute/
- **Setting Return Values**: https://nsubstitute.github.io/help/return-values/
- **Checking Calls**: https://nsubstitute.github.io/help/received-calls/

### UI Framework

#### Zurb Foundation
- **Official Docs**: https://get.foundation/sites/docs/
- **Grid System**: https://get.foundation/sites/docs/grid.html
- **Components**: https://get.foundation/sites/docs/kitchen-sink.html
- **SASS**: https://get.foundation/sites/docs/sass.html
- **Responsive**: https://get.foundation/sites/docs/media-queries.html

### External APIs

#### Raindrop.io
- **API Docs**: https://developer.raindrop.io/
- **OAuth**: https://developer.raindrop.io/#oauth
- **Authentication**: https://developer.raindrop.io/#authentication
- **Collections**: https://developer.raindrop.io/#collections
- **Raindrops**: https://developer.raindrop.io/#raindrops

#### OAuth 2.0 Specification
- **RFC 6749**: https://tools.ietf.org/html/rfc6749
- **Auth Code Flow**: https://tools.ietf.org/html/rfc6749#section-4.1
- **Token Endpoint**: https://tools.ietf.org/html/rfc6749#section-3.2

### Development Tools

#### .NET 9 / C# 12/13
- **.NET 9 Docs**: https://docs.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9
- **C# 12 Features**: https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12
- **C# 13 Features**: https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-13

#### Visual Studio 2022
- **Docs**: https://docs.microsoft.com/en-us/visualstudio/
- **Blazor Support**: https://docs.microsoft.com/en-us/visualstudio/javascript/tutorial-asp-net-core-with-blazor

## AI Coding Assistant Guidelines

### Using Context7 Documentation

When using Context7 for documentation lookup:

1. **Primary Commands**:
   ```
   resolve-library-id "library-name"    # Find exact library ID
   get-library-docs "/org/project"      # Get documentation
   ```

2. **Best Practices**:
   - Always use `resolve-library-id` first unless you have exact Context7 ID
   - Specify `tokens` parameter (recommended: 3000-5000 for comprehensive docs)
   - Use `topic` parameter to focus on specific areas
   - Prioritize libraries with high Trust Scores (8.0+) and Code Snippet counts

3. **Fallback Strategy**:
   - If Context7 fails, reference official documentation URLs above
   - Cross-reference multiple sources for accuracy
   - Verify code examples against current API versions

### Technology-Specific Guidance

#### For Blazor Development
- **Context7**: Use `/dotnet/blazor-samples` and `/microsoft/fluentui-blazor`
- **Priorities**: Component lifecycle, JavaScript interop, performance optimization
- **Official**: Always verify against Microsoft's official Blazor documentation

#### For Azure Functions
- **Context7**: Use `/azure/azure-functions-host` 
- **Topics**: HTTP triggers, testing patterns, dependency injection
- **Official**: Microsoft Azure Functions documentation for latest features

#### For Testing (TUnit + NSubstitute)
- **Context7**: Use `/thomhurst/tunit` and `/nsubstitute/nsubstitute`
- **Focus**: Test attributes, mocking patterns, async testing
- **Verification**: Compare against official GitHub repositories

#### For OAuth Integration
- **Context7**: Use `/panva/oauth4webapi` for implementation patterns
- **Raindrop.io**: Always reference official API documentation
- **Security**: Follow OAuth 2.0 RFC specifications for secure implementation

## Documentation Maintenance

### Regular Updates
- Check Context7 availability monthly for new or updated libraries
- Monitor official documentation for API changes
- Update this file when new technologies are added to the project

### Version Tracking
- Document API version dependencies
- Note breaking changes in major framework updates
- Maintain compatibility matrices for integrated technologies

---

*Last Updated: 2025-01-02*
*Context7 Status: Indexed libraries available, some content access issues*
*Primary Documentation Strategy: Official sources with Context7 supplementation*