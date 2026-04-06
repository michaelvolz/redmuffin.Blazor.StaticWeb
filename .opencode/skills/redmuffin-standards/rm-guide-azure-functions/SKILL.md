---
name: rm-guide-azure-functions
description: "Shortcut: rm:guide-azure-functions. Use when creating or reviewing Azure Functions isolated worker code."
---

# rm-guide-azure-functions

## CRITICAL

- Use isolated worker patterns.
- Keep functions small and single-purpose.
- Validate inputs early and return clear status codes.
- Use structured `ILogger` logging.

## WHEN TO LOAD

- Creating or changing function handlers, bindings, or host setup.

## GUIDANCE

- Use async all the way through I/O paths.
- Keep configuration in environment variables or options.
- Make event-driven handlers idempotent when duplicates are possible.

## NEVER

- Do not block on async work.
- Do not let HTTP handlers return opaque failures.
