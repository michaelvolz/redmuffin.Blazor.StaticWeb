---
title: OpenCode CE package update recovery procedure
problem_type: workflow_issue
category: integration-issues
date: 2026-04-05
last_updated: 2026-04-05
track: knowledge
component: opencode
module: instruction-architecture
tags:
  [
    opencode,
    agents,
    ce-review,
    recovery,
    package-updates,
    trigger-optimization,
    uncle-bob-csharp,
  ]
applies_when: >
  OpenCode package updates overwrite local CE-aligned reviewer edits and the
  rm-* reviewer integration needs to be restored without rethinking the design.
---

# OpenCode CE package update recovery procedure

## Context

This procedure is for an agent restoring the CE-aligned `rm-*` reviewer setup after
an OpenCode package update resets or reverses local edits. Treat the current local
files as the working source of truth, but use this document to reapply the known
shape quickly and consistently.

The current known shape includes five `rm-*` reviewers and their CE routing hooks.

## Recovery goal

Restore the CE workflow integration for:

- `rm-dotnet-csharp-reviewer`
- `rm-powershell-reviewer`
- `rm-blazor-reviewer`
- `rm-html-css-blazor-reviewer`
- `rm-uncle-bob-csharp-reviewer`

without widening their scope or losing the CE JSON review contract.

## Procedure

### 1. Confirm what was overwritten

- Compare the current `rm-*` agent files against the intended CE-aligned shape.
- Check the CE review workflow files for missing stack-specific routing entries.
- Verify whether the Blazor vs HTML/CSS split is still explicit.
- Verify that `rm-uncle-bob-csharp-reviewer` is still distinct from `rm-dotnet-csharp-reviewer`.

### 2. Restore the CE-style trigger language

Reapply the same trigger discipline:

- put the selection scope in the frontmatter description
- keep the description conditional and narrow
- avoid broad, generic reviewer language
- keep the agent short and easy to skim

### 3. Restore the suppression / non-goal sections

Each reviewer should keep a small exclusion list so it does not drift into generic feedback:

- C# reviewer: ignore formatting-only noise and framework-mandated boilerplate
- Uncle Bob C# reviewer: ignore ordinary readability nits that do not change architecture, testability, or dependency direction
- PowerShell reviewer: ignore style preferences and platform necessities
- Blazor reviewer: ignore pure HTML/CSS taste issues
- HTML/CSS reviewer: ignore lifecycle and rendering logic

### 4. Restore the CE JSON output contract

Each reviewer must continue to return the CE-shaped payload:

- `reviewer`
- `findings`
- `residual_risks`
- `testing_gaps`

Do not add prose outside the JSON.

### 5. Reintegrate the reviewers into `ce:review`

Update the CE routing matrix and persona catalog so the five `rm-*` reviewers are
selected as stack-specific conditionals rather than standalone-only agents.

Keep the boundary explicit:

- `rm-blazor-reviewer` owns component behavior, rendering, lifecycle, and Blazor security
- `rm-html-css-blazor-reviewer` owns semantic HTML, CSS, layout, and accessibility
- `rm-uncle-bob-csharp-reviewer` owns the stricter C# craftsmanship lens for architecture, dependency direction, and testability

### 6. Recheck the documentation trail

Update or verify these documents after restoring the files:

- `docs/plans/2026-04-05-008-refactor-selective-reviewer-routing-plan.md`
- `docs/plans/2026-04-05-009-refactor-rm-reviewer-ce-alignment-plan.md`

## Recovery checklist

- [ ] `rm-*` frontmatter descriptions are narrow and selection-focused
- [ ] each reviewer has a short suppression section
- [ ] each reviewer returns the CE JSON schema
- [ ] `ce:review` routes the five `rm-*` reviewers through the persona catalog
- [ ] Blazor and HTML/CSS ownership are still distinct
- [ ] Uncle Bob C# and general .NET C# ownership are still distinct
- [ ] no vendor reviewer or unrelated CE persona was modified during recovery

## Guardrails

- Do not widen triggers to make recovery easier.
- Do not replace CE reviewers with the `rm-*` reviewers.
- Do not change the review domains.
- Do not add procedural chatter to the agent files.

## Related

- `docs/plans/2026-04-05-008-refactor-selective-reviewer-routing-plan.md`
- `docs/plans/2026-04-05-009-refactor-rm-reviewer-ce-alignment-plan.md`
- `.opencode/agents/rm-dotnet-csharp-reviewer.md`
- `.opencode/agents/rm-powershell-reviewer.md`
- `.opencode/agents/rm-blazor-reviewer.md`
- `.opencode/agents/rm-html-css-blazor-reviewer.md`
- `.opencode/agents/rm-uncle-bob-csharp-reviewer.md`
