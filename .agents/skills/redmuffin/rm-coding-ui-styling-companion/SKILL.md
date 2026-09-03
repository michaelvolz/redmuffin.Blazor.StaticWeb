---
name: rm-coding-ui-styling-companion
description: redmuffin.Blazor.StaticWeb styling stack, SCSS pipeline, and daisyUI migration. Load when writing or building this repo's styles. General CSS standards live in rm-coding-ui-styling.
purpose: Repo-specific styling stack and migration state for redmuffin.Blazor.StaticWeb.
---

# rm-coding-ui-styling-companion

Companion to `rm-coding-ui-styling`: the global skill owns CSS standards,
accessibility, and framework pattern catalogs; this skill owns only this
repo's stack and migration. On a conflict between the two, this skill wins
for this repo.

## Current stack

- **Foundation 6** — self-hosted SCSS, selective `@include` (9 of ~38 modules)
- **Font Awesome 6.7.0** — self-hosted woff2
- **Build** — `dart-sass` CLI → `app.min.css`
- **Migration target:** daisyUI v5 + Tailwind CSS v4 (Foundation 6
  maintenance mode). Full mapping, dev CDN workflow, production CLI:
  [migration-daisyui.md](references/migration-daisyui.md).

## Reference routing

| Topic                        | Read when                                  | File                                                    |
| ---------------------------- | ------------------------------------------ | ------------------------------------------------------- |
| SCSS rules & build           | Writing or building any pre-migration SCSS | [scss-architecture.md](references/scss-architecture.md) |
| Current stack summary        | Checking what the current stack uses       | [foundation-patterns.md](references/foundation-patterns.md) |
| daisyUI + Tailwind v4 migration | Planning or doing the migration         | [migration-daisyui.md](references/migration-daisyui.md) |
