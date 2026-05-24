# Persona Catalog

23 reviewer personas organized into always-on, cross-cutting conditional, stack-specific conditional, and RM author reviewer layers, plus CE-specific agents. The orchestrator uses this catalog to select which reviewers to spawn for each review.

## Always-on (4 personas + 2 CE agents)

Spawned on every review regardless of diff content.

**Persona agents (structured JSON output):**

| Persona             | Agent                           | Focus                                                                                                             |
| ------------------- | ------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| `correctness`       | `ce-correctness-reviewer`       | Logic errors, edge cases, state bugs, error propagation, intent compliance                                        |
| `testing`           | `ce-testing-reviewer`           | Coverage gaps, weak assertions, brittle tests, missing edge case tests                                            |
| `maintainability`   | `ce-maintainability-reviewer`   | Coupling, complexity, naming, dead code, premature abstraction                                                    |
| `project-standards` | `ce-project-standards-reviewer` | CLAUDE.md and AGENTS.md compliance -- frontmatter, references, naming, cross-platform portability, tool selection |

**CE agents (unstructured output, synthesized separately):**

| Agent                      | Focus                                                                            |
| -------------------------- | -------------------------------------------------------------------------------- |
| `ce-agent-native-reviewer` | Verify new features are agent-accessible                                         |
| `ce-learnings-researcher`  | Search docs/solutions/ for past issues related to this PR's modules and patterns |

## Conditional (7 personas)

Spawned when the orchestrator identifies relevant patterns in the diff. The orchestrator reads the full diff and reasons about selection -- this is agent judgment, not keyword matching.

| Persona             | Agent                           | Select when diff touches...                                                                                                                                                                                                                                                           |
| ------------------- | ------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `security`          | `ce-security-reviewer`          | Auth middleware, public endpoints, user input handling, permission checks, secrets management                                                                                                                                                                                         |
| `performance`       | `ce-performance-reviewer`       | Database queries, ORM calls, loop-heavy data transforms, caching layers, async/concurrent code                                                                                                                                                                                        |
| `api-contract`      | `ce-api-contract-reviewer`      | Route definitions, serializer/interface changes, event schemas, exported type signatures, API versioning                                                                                                                                                                              |
| `data-migrations`   | `ce-data-migrations-reviewer`   | Migration files, schema changes, backfill scripts, data transformations                                                                                                                                                                                                               |
| `reliability`       | `ce-reliability-reviewer`       | Error handling, retry logic, circuit breakers, timeouts, background jobs, async handlers, health checks                                                                                                                                                                               |
| `adversarial`       | `ce-adversarial-reviewer`       | Diff has >=50 changed non-test, non-generated, non-lockfile lines, OR touches auth, payments, data mutations, external API integrations, or other high-risk domains                                                                                                                   |
| `previous-comments` | `ce-previous-comments-reviewer` | **PR-only AND comment-gated.** Reviewing a PR that has existing review comments or review threads from prior review rounds. Skip entirely when no PR metadata was gathered in Stage 1, OR when Stage 1's `hasPriorComments` flag is false (no `reviews` and no `comments` on the PR). |

## Stack-Specific Conditional (6 personas)

These reviewers keep their original opinionated lens. They are additive with the cross-cutting personas above, not replacements for them.

| Persona                | Agent                              | Select when diff touches...                                                                                                                                                                                                                          |
| ---------------------- | ---------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `dhh-rails`            | `ce-dhh-rails-reviewer`            | Rails architecture, service objects, authentication/session choices, Hotwire-vs-SPA boundaries, or abstractions that may fight Rails conventions                                                                                                     |
| `kieran-rails`         | `ce-kieran-rails-reviewer`         | Rails controllers, models, views, jobs, components, routes, or other application-layer Ruby code where clarity and conventions matter                                                                                                                |
| `kieran-python`        | `ce-kieran-python-reviewer`        | Python modules, endpoints, services, scripts, or typed domain code                                                                                                                                                                                   |
| `kieran-typescript`    | `ce-kieran-typescript-reviewer`    | TypeScript components, services, hooks, utilities, or shared types                                                                                                                                                                                   |
| `julik-frontend-races` | `ce-julik-frontend-races-reviewer` | Stimulus/Turbo controllers, DOM event wiring, timers, async UI flows, animations, or frontend state transitions with race potential                                                                                                                  |
| `farley`               | `rm-farley-csharp-reviewer`        | CI/CD files (.yml, .ps1), deployment configuration, build scripts, pipeline infrastructure. Also fires when the diff changes how the system is built, tested, or deployed — even if the changed file is C# that affects the pipeline.                |
| `swift-ios`            | `ce-swift-ios-reviewer`            | Swift files, SwiftUI views, UIKit controllers, `.entitlements`, `PrivacyInfo.xcprivacy`, `.xcdatamodeld`, `Package.swift`, `Package.resolved`, storyboards, XIBs, or semantic build-setting / target-membership / code-signing changes in `.pbxproj` |

## RM Author Reviewers (6 personas)

These reviewers apply the lens of a specific software engineering author to C# code. Each has a non-overlapping domain — they never find the same thing. Selected when the diff contains `.cs` files AND the domain is active.

| Persona             | Agent                           | Author           | Select when diff...                                                                                                                                                                         |
| ------------------- | ------------------------------- | ---------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `uncle-bob-csharp`  | `rm-uncle-bob-csharp-reviewer`  | Robert C. Martin | Touches architecture boundaries, DI registration, class structure, or dependency chains. Domain: structural design, dependency direction, class responsibility.                             |
| `ousterhout-csharp` | `rm-ousterhout-csharp-reviewer` | John Ousterhout  | Introduces new public APIs, classes, interfaces, or method signature changes. Domain: complexity depth — shallow modules, pass-through layers, information leakage.                         |
| `feathers-csharp`   | `rm-feathers-csharp-reviewer`   | Michael Feathers | Touches test files or production code that lacks corresponding characterization tests. Domain: safety before change — missing seams, static coupling, untestable dependencies.              |
| `beck-csharp`       | `rm-beck-csharp-reviewer`       | Kent Beck        | Touches test files or introduces new C# code. Domain: process quality — weak assertions, over-engineering, Simple Design violations.                                                        |
| `fowler-csharp`     | `rm-fowler-csharp-reviewer`     | Martin Fowler    | Touches domain, service, or business-logic classes with detectable refactoring patterns. Domain: transformation patterns — missed Extract/Move/Replace opportunities, anemic domain models. |
| `farley`            | `rm-farley-csharp-reviewer`     | Dave Farley      | Touches CI/CD files (.yml, .ps1), deployment configuration, build scripts, or pipeline infrastructure. Domain: deployment safety, pipeline quality, fast feedback loops.                    |

### RM Author Reviewer Selection Rules

1. **All six are conditional.** Never spawn an RM author reviewer just because `.cs` files are present. The orchestrator reads the diff and decides whether the author's domain is active.
2. **Domain overlap prevention.** These reviewers are designed for zero overlap. Uncle Bob looks at dependency arrows; Ousterhout looks at complexity depth. Feathers asks "is this change safe?"; Fowler asks "which pattern should I apply?" Beck asks "is this the simplest thing?"; Farley looks at the pipeline around the code. If two would flag the same thing, you selected wrong — re-read the domain descriptions.
3. **Max 3 RM author reviewers per review.** Beyond 3, the signal-to-noise ratio degrades. Pick the 3 most relevant to the diff. If the diff genuinely activates 4+ domains, pick the top 3 by impact.
4. **They supplement, not replace.** RM author reviewers are additive with the always-on and cross-cutting CE personas. A security reviewer and an Uncle Bob reviewer may both find something in the same file — they're looking through different lenses.

## CE Conditional Agents (migration-specific)

These CE-native agents provide specialized analysis beyond what the persona agents cover. Spawn them when the diff includes database migrations, schema.rb, or data backfills.

| Agent                              | Focus                                                                                        |
| ---------------------------------- | -------------------------------------------------------------------------------------------- |
| `ce-schema-drift-detector`         | Cross-references schema.rb changes against included migrations to catch unrelated drift      |
| `ce-deployment-verification-agent` | Produces Go/No-Go deployment checklist with SQL verification queries and rollback procedures |

## Selection rules

1. **Always spawn all 4 always-on personas** plus the 2 CE always-on agents.
2. **For each cross-cutting conditional persona**, the orchestrator reads the diff and decides whether the persona's domain is relevant. This is a judgment call, not a keyword match.
3. **For each stack-specific conditional persona**, use file types and changed patterns as a starting point, then decide whether the diff actually introduces meaningful work for that reviewer. Do not spawn language-specific reviewers just because one config or generated file happens to match the extension.
4. **For CE conditional agents**, spawn when the diff includes migration files (`db/migrate/*.rb`, `db/schema.rb`) or data backfill scripts.
5. **Announce the team** before spawning with a one-line justification per conditional reviewer selected.
