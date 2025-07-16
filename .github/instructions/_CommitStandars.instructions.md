# Commit Standards - concise

Format: `<type>(<scope>): <description>` (<112 chars)

**Types:** feat, fix, docs, style, refactor, perf, test, chore, security, ci, config, revert

**Scopes:** blazor, components, pages, api, ui, db, auth, services, models, utils, build, deploy, scripts

**Breaking Changes:** Add `!` after scope (e.g., `feat(api)!: remove deprecated endpoint`)

**Body:** Blank line + 2-3 sentences explaining what/why changed + migration notes

---

# Commit Standards - detailed

Format: <type>(<scope>): <description> (<112 chars)

Types:
- feat: New feature
- fix: Bug fix
- docs: Documentation changes
- style: Code style changes (formatting, missing semicolons, etc.)
- refactor: Code refactoring without changing functionality
- perf: Performance improvements
- test: Adding or updating tests
- chore: Maintenance tasks, dependency updates
- security: Security-related changes
- ci: CI/CD pipeline changes
- config: Configuration changes
- revert: Reverting previous commits

Scopes:
- blazor: Core Blazor functionality
- components: Reusable UI components
- pages: Page-level components
- api: API-related changes
- ui: General UI/styling
- db: Database-related changes
- auth: Authentication/authorization
- services: Business logic services
- models: Data models/DTOs
- utils: Utility functions
- build: Build system
- deploy: Deployment
- scripts: PowerShell/utility scripts

Breaking Changes: Add ! after scope (e.g., feat(api)!: remove deprecated endpoint)

Body: 2-3 sentences explaining:
- Blank line between header and body
- What was changed
- Why it was changed
- Any breaking changes or migration notes

---

# Commit Standards - detailed, oneliner

Format: <type>(<scope>): <description> (<112 chars). Types: - feat: New feature, - fix: Bug fix, - docs: Documentation changes, - style: Code style changes (formatting, missing semicolons, etc.), - refactor: Code refactoring without changing functionality, - perf: Performance improvements, - test: Adding or updating tests, - chore: Maintenance tasks, dependency updates, - security: Security-related changes, - ci: CI/CD pipeline changes, - config: Configuration changes, - revert: Reverting previous commits. Scopes: - blazor: Core Blazor functionality, - components: Reusable UI components, - pages: Page-level components, - api: API-related changes, - ui: General UI/styling, - db: Database-related , changes, - auth: Authentication/authorization, - services: Business logic services, - models: Data models/DTOs, - utils: Utility functions, - build: Build system, - deploy: Deployment, - scripts: PowerShell/utility scripts. Breaking Changes: Add ! after scope (e.g., feat(api)!: remove deprecated endpoint). Body: 2-3 sentences explaining: - Blank line between header and body, - What was changed, - Why it was changed, - Any breaking changes or migration notes
