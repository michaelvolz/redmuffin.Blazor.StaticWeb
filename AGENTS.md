# Agent Instructions

This is the main instruction file. Domain-specific rules are in the `skills/` folder:
- `skills/dotnet/` - .NET config, DI, build settings
- `skills/csharp-standards/` - C# analyzer rules, logging, partial classes
- `skills/testing/` - TUnit patterns, TestScope, mocking
- `skills/ui-styling/` - SCSS, Foundation CSS, accessibility
- `skills/powershell/` - Script automation
- `skills/markdown/` - Markdown standards, MarkdownLint
- `skills/package-management/` - NuGet package management

## 🚨 Critical Rules

- **ZERO BUILD WARNINGS MANDATE**: AFTER EVERY C# FILE CHANGE, RUN `dotnet clean && dotnet build --no-restore --verbosity quiet`. FIX ALL WARNINGS (EXCEPT IL2111) BEFORE CONTINUING. FAILURE CAUSES HUNDREDS OF ERRORS.
- **File Editing**: Edit one file at a time, track progress (e.g., "Edit 2 of 5").
- **Large Changes**: Outline plan, get approval, make incremental edits, ensure buildable state.
- **Batch Data Processing**: Process large datasets or text in smaller batches to prevent errors, file corruption, or performance issues. Avoid handling entire files at once unless explicitly requested.

## Important Rules


## Project Structure and Configuration

- **Frontend**: Blazor WebAssembly (.NET 9), feature-based structure.
- **Backend**: Azure Functions (.NET 8), isolated worker.
- **Testing Framework**: TUnit with `[Test]` and `[Arguments]`. NEVER use xUnit, NUnit, or MSTest. (See `skills/testing/` for details.)
- **Build Settings**: `WasmStripILAfterAOT=true`, `InvariantGlobalization=true`, `PublishTrimmed=true`.
- **Deployment**: Azure Static Web Apps with CSP and caching in `staticwebapp.config.json`.
- **Dependencies and Configuration**: See `skills/dotnet/` for details.
- **UI and Styling**: See `skills/ui-styling/` for details.

## 🔒 Security and API

- **Input Validation**: Always validate/sanitize inputs.
- **XSS/CSRF**: Use Blazor built-in protections.
- **Secrets**: Never expose in client-side code.
- **API**: Use `IHttpClientFactory` for HTTP needs.
- **CSP**: Configure in `staticwebapp.config.json`, allows 'unsafe-inline' for styles, restricts scripts.
- **Azure Functions**: Use isolated worker with dependency injection.
- **Authentication**: Use ASP.NET Core Identity with RBAC.

## 📁 File Organization

- **Features**: `src/[ProjectName]/Features/` for components and pages.
- **Static Assets**: `src/[ProjectName]/wwwroot/` for CSS, SCSS, JS, sample data, libraries.
- **Tests**: `tests/` mirroring source structure with TUnit projects.
- **Test Project Naming**: Name all test projects `[ProjectName].Tests` (e.g., `MyApp.Tests`) to maintain consistent solution structure.
- **Scripts**: `scripts/` for PowerShell automation. (See `skills/powershell/` for details.)
- **Config**: `.github/` for workflows, instructions, prompts, chatmodes.
- **Key Directories**: `.github/instructions/` (standards), `.github/prompts/` (AI prompts), `.github/chatmodes/` (AI modes), `src/[ProjectName]/Api/` (Azure Functions), `src/[ProjectName]/Common/` (shared).
- **PRD File Location**: Locate all PRDs, PRD-TaskLists, and PRD-ToDos in the `tasks/` folder. Files use the format `PRD-XXX` (XXX is 000–999) to group related documents.

## 📝 Markdown Standards

- **Compliance**: Follow `skills/markdown/` with MarkdownLint rules (MD001-MD059).
- **Configuration**: Use `.markdownlint.jsonc` for auto-fix.
- **VS Code**: Enable auto-format, lint workspace, fix violations.
- **Structure**: Proper heading hierarchy, consistent formatting, aligned tables, validated links.
- **Encoding**: UTF8 without BOM.
- **Excluded Files**: Ignore `wwwroot/sample-data/markdown-cheat-sheet.md`, `wwwroot/Example.md`.

## 🛠️ Tools and Coverage

- **Coverage**: See `skills/powershell/` for details on coverage scripts and configuration.
- **Build Verification**: Use `run_build` tool.
- **MCP Server Usage**: Use the following MCP servers for specific tasks:
  - `github`: Automate GitHub API tasks (e.g., repo creation, PRs).
  - `puppeteer`: Scrape JavaScript-rendered pages (e.g., dynamic content).
  - `fetch`: Scrape static content or convert URLs to Markdown (e.g., documentation).
  - `brave-search`: Conduct web searches with `brave_web_search` (2,000 queries/month) (e.g., find APIs).
  - `time`: Handle time-related tasks (e.g., log build times).
  - `context7`: Process HTTP-based context (e.g., analyze code context, resolve library IDs, get library docs).
  - `sequentialthinking`: Solve complex problems (e.g., optimize algorithms).
