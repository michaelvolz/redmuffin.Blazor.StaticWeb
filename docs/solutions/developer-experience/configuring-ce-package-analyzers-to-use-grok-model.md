---
title: Configuring CE Package Analyzers to Use Grok Model
date: 2026-04-06
category: developer-experience
module: opencode
problem_type: developer_experience
component: tooling
severity: medium
applies_when:
  - Setting up CE package analyzers
  - Upgrading analyzer configurations
  - Needing enhanced code review capabilities
tags:
  - ce-package-analyzers
  - grok-model
  - configuration
---

# Configuring CE Package Analyzers to Use Grok Model

## Context

CE package analyzers are specialized reviewer agents that provide automated code analysis, security checks, performance reviews, and other quality assessments. These agents can be configured to use different AI models, with the default model potentially not optimized for the specialized review tasks they perform.

## Guidance

To configure CE package analyzers to use the Grok model for enhanced reasoning and analysis:

1. Open `opencode.json` in the project root
2. Locate the `"agent"` section
3. Add model configuration entries for each CE analyzer using the format:
   ```json
   "analyzer-name": { "model": "github-copilot/grok-code-fast-1" }
   ```
4. Ensure all 50+ CE package analyzers are configured (see Examples section)

The configuration should be added to the existing `"agent"` object, maintaining any existing configurations for other agents.

## Why This Matters

Using the Grok model provides enhanced reasoning capabilities, better accuracy in code analysis, and improved code review quality. The model is specifically designed for coding tasks and provides faster, more reliable analysis compared to default models. This results in higher-quality automated reviews and better developer experience.

## When to Apply

- During initial CE package setup
- When upgrading or updating analyzer configurations
- When experiencing suboptimal analysis performance
- When needing more accurate code review results
- During project configuration optimization

## Examples

Complete `opencode.json` agent configuration with all CE package analyzers:

```json
"agent": {
  "rm-build": { "disable": true },
  "rm-plan": { "disable": true },
  "uncle-bob-csharp-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "dotnet-csharp-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "blazor-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "html-css-blazor-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "powershell-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "adversarial-document-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "adversarial-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "agent-native-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "ankane-readme-writer": { "model": "github-copilot/grok-code-fast-1" },
  "api-contract-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "architecture-strategist": { "model": "github-copilot/grok-code-fast-1" },
  "best-practices-researcher": { "model": "github-copilot/grok-code-fast-1" },
  "bug-reproduction-validator": { "model": "github-copilot/grok-code-fast-1" },
  "cli-agent-readiness-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "cli-readiness-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "code-simplicity-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "coherence-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "correctness-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "data-integrity-guardian": { "model": "github-copilot/grok-code-fast-1" },
  "data-migration-expert": { "model": "github-copilot/grok-code-fast-1" },
  "data-migrations-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "deployment-verification-agent": { "model": "github-copilot/grok-code-fast-1" },
  "design-implementation-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "design-iterator": { "model": "github-copilot/grok-code-fast-1" },
  "design-lens-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "dhh-rails-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "feasibility-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "figma-design-sync": { "model": "github-copilot/grok-code-fast-1" },
  "framework-docs-researcher": { "model": "github-copilot/grok-code-fast-1" },
  "git-history-analyzer": { "model": "github-copilot/grok-code-fast-1" },
  "issue-intelligence-analyst": { "model": "github-copilot/grok-code-fast-1" },
  "julik-frontend-races-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "kieran-python-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "kieran-rails-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "kieran-typescript-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "learnings-researcher": { "model": "github-copilot/grok-code-fast-1" },
  "lint": { "model": "github-copilot/grok-code-fast-1" },
  "maintainability-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "pattern-recognition-specialist": { "model": "github-copilot/grok-code-fast-1" },
  "performance-oracle": { "model": "github-copilot/grok-code-fast-1" },
  "performance-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "pr-comment-resolver": { "model": "github-copilot/grok-code-fast-1" },
  "previous-comments-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "product-lens-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "project-standards-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "reliability-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "repo-research-analyst": { "model": "github-copilot/grok-code-fast-1" },
  "schema-drift-detector": { "model": "github-copilot/grok-code-fast-1" },
  "scope-guardian-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "security-lens-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "security-reviewer": { "model": "github-copilot/grok-code-fast-1" },
  "security-sentinel": { "model": "github-copilot/grok-code-fast-1" },
  "spec-flow-analyzer": { "model": "github-copilot/grok-code-fast-1" },
  "testing-reviewer": { "model": "github-copilot/grok-code-fast-1" }
}
```

## Related

- [OpenCode Instruction Management Lessons](docs/solutions/integration-issues/opencode-instruction-management-lessons-2026-04-03.md) - Covers `opencode.json` usage for skill permissions and global config management
- [Compound Engineering Plugin Installation](docs/solutions/integration-issues/compound-engineering-plugin-installation-2026-04-01.md) - Details installing CE plugin which provides the analyzers configured here
- [OpenCode CE Package Update Recovery Procedure](docs/solutions/integration-issues/opencode-ce-package-update-recovery-procedure-2026-04-05.md) - Describes maintaining CE-aligned reviewers after package updates
- [OpenCode Instruction Architecture Pattern](docs/solutions/integration-issues/opencode-instruction-architecture-pattern-2026-04-03.md) - Establishes patterns for organizing OpenCode instructions and agent setup
