# Agent Instructions

Skills folder (`skills/`) contains detailed rules - loaded automatically when relevant:
- dotnet, csharp-standards, testing, ui-styling, powershell, markdown, package-management, commits

## Reference Guides

For deeper context on specific topics, see `.github/guides/`:
- `blazor.md` - Blazor patterns and best practices
- `aspnet-rest-apis.md` - ASP.NET REST API development
- `azure-functions.md` - Azure Functions best practices
- `github-actions.md` - CI/CD pipeline design
- `performance.md` - Performance optimization techniques
- `documentation.md` - Documentation standards
- `security.md` - Security and API guidelines

## 🚨 Critical Rules

- **ZERO BUILD WARNINGS**: After every C# file change, run `dotnet clean && dotnet build --no-restore --verbosity quiet`. Fix all warnings (except IL2111).
- **File Editing**: Edit one file at a time, track progress (e.g., "Edit 2 of 5").
- **Large Changes**: Outline plan, get approval, make incremental edits, ensure buildable state.
- **Commit vs Push**: NEVER commit or push without explicit user permission. Always wait for the user to explicitly say "commit" or "commit and push" before taking any git action. This ensures the user has full control over when changes go public.

## Project Overview

- **Frontend**: Blazor WebAssembly (.NET 9)
- **Backend**: Azure Functions (.NET 9), isolated worker
- **Testing**: TUnit (NEVER xUnit, NUnit, MSTest)
- **Deployment**: Azure Static Web Apps

## File Organization

- Features: `src/[Project]/Features/`
- Tests: `tests/` mirroring source
- PRDs: `tasks/PRD-XXX-*.md`
- Scripts: `scripts/`

## Build Scripts

- `scripts/test-build-fast.ps1` - Development builds (AoT disabled, ~9s)
- `scripts/test-build-aot.ps1` - Production parity testing
- `scripts/Generate-CoverageReport.ps1` - Test coverage reports

## Development Modes

| Mode | Port | Use Case |
|------|------|----------|
| Simplified | 5233 | UI work, uses mock data when API unavailable |
| Full Stack | 4280 | API integration, OAuth, E2E testing |

## Configuration

- Use `Debug-Sass` configuration for SCSS compilation
- Debug mode: analyzers ENABLED
- Release mode: analyzers DISABLED

## MCP Servers

Use these MCP servers for specific tasks:
- **github**: Automate GitHub API tasks (e.g., repo creation, PRs)
- **puppeteer**: Scrape JavaScript-rendered pages (e.g., dynamic content)
- **fetch**: Scrape static content or convert URLs to Markdown (e.g., documentation)
- **brave-search**: Conduct web searches (2,000 queries/month) (e.g., find APIs)
- **time**: Handle time-related tasks (e.g., log build times)
- **context7**: Process HTTP-based context (e.g., analyze code, resolve library IDs, get library docs)
- **sequentialthinking**: Solve complex problems (e.g., optimize algorithms)
