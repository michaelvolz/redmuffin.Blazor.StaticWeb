---
title: "feat: Add tool resilience plugin for error recovery and rate limiting"
type: feat
status: active
date: 2026-04-14
origin: docs/brainstorms/tool-resilience-plugin-requirements.md
---

# Tool Resilience Plugin Plan

## Overview

Builds an OpenCode plugin that intercepts tool errors and rate limiting, providing smart recovery and logging. Handles skill/agent/command name resolution errors via `tool.execute.error` hook, and auto-retries 429 rate limit errors with proper delay.

## Problem Frame

Users experience silent tool failures when:

1. Skill, agent, or command names are misspelled - the tool fails without telling the model what went wrong
2. Web search API rate limits are hit (429) - the tool call is lost instead of being retried

This plugin provides error interception, smart suggestions, and auto-retry to make the system more resilient.

## Requirements Trace

- R1. Intercept all tool errors via `tool.execute.error` hook and log to file
- R2. For skill/agent/command resolution errors, suggest the correct name back to the model
- R3. Detect 429 rate limit errors from Brave Search and Exa Search
- R4. Auto-retry rate-limited calls after appropriate delay (Brave: 1.1s, Exa: 100ms)
- R5. Log all throttle events to rate-limits log file

## Scope Boundaries

- Does NOT modify tool definitions or add new tools
- Does NOT persist state across sessions (per-session Map is sufficient)
- Does NOT change OpenCode core behavior except via hooks

### Deferred to Separate Tasks

- Advanced rate limit header parsing (dynamic delay based on X-RateLimit-Reset) - future iteration

## Context & Research

### Relevant Code and Patterns

- **Plugin location:** `.opencode/plugins/` - existing plugins like `block-push.js` follow async export pattern
- **Plugin type:** Import `Plugin` from `@opencode-ai/plugin`
- **Logging:** Use `client.app.log()` for structured logging (levels: debug, info, warn, error)
- **Fail-open pattern:** From `block-push.js` - catch errors, log, don't block for safety

### Institutional Learnings

- Fail-open error handling from `docs/solutions/logic-errors/block-push-plugin-logic-errors-2026-04-03.md`:
  - Policy violations re-thrown to enforce, unexpected errors logged and continue
  - Use error prefixes like "BLOCKED by Policy:" for distinguishability

### External References

- **OpenCode tool.execute.error hook:** GitHub issue #10027 (merged Feb 2026) - enables error interception
- **Brave Search rate limits:** 1 req/sec free tier, 50 QPS paid - X-RateLimit-Reset header for retry delay
- **Exa Search rate limits:** 10 QPS default - X-RateLimit-Reset header for retry delay

## Key Technical Decisions

- **Hook selection:** Use `tool.execute.error` (new in Feb 2026) over `tool.execute.after` for error-only handling
- **Rate limit detection:** Match by tool name for Brave/Exa tools, not by error message content
- **Auto-retry approach:** Re-execute the tool call with same args after delay - preserves tool call, no loss
- **Delay calculation:** 1.1s for Brave free tier (1s + 100ms buffer), 100ms for Exa (respects 10 QPS)

## Open Questions

### Resolved During Planning

- **Rate limit detection method:** By tool name (brave-search_brave_web_search, exa search) - simpler and explicit

### Deferred to Implementation

- Dynamic delay from X-RateLimit-Reset header - requires testing actual header response
- Skill/agent name suggestions - requires mapping of common misspellings

> **Retry approach uncertainty:** We don't know if `tool.execute.error` hook has access to re-invoke tools via `client.callTool()`. Two strategies:
>
> - **Strategy A (auto-retry):** Re-execute from error hook — simpler but may face API access issues
> - **Strategy B (model hint):** Set output to tell model "rate limited, wait Xms and retry" — more reliable but requires model cooperation
>   This is an implementation-time discovery. Start with Strategy A, fall back to Strategy B if API access is unavailable.

## Implementation Units

- [ ] **Unit 1: Create plugin scaffolding**

  **Goal:** Create the basic plugin structure with export and logging

  **Requirements:** R1

  **Dependencies:** None

  **Files:**
  - Create: `.opencode/plugins/tool-resilience.js`

  **Approach:**
  - Export async function with context destructuring
  - Initialize log file paths
  - Return empty hooks object for skeleton

  **Patterns to follow:**
  - Async export pattern from existing plugins
  - Plugin type imports from `@opencode-ai/plugin`

  **Test scenarios:**
  - [Happy path: Plugin loads without errors in OpenCode startup]
  - [Error path: Plugin fails gracefully if log directory missing]

  **Verification:**
  - Plugin appears in `opencode.json` plugin list or loads from `.opencode/plugins/`

- [ ] **Unit 2: Implement error logging**

  **Goal:** Log all tool errors to file with timestamp, tool name, args, and error message

  **Requirements:** R1

  **Dependencies:** Unit 1

  **Files:**
  - Modify: `.opencode/plugins/tool-resilience.js`

  **Approach:**
  - Hook into `tool.execute.error` with handler
  - Format: `[ISO timestamp] TOOL_ERROR tool=<tool> callID=<callID> error=<message>`
  - Append to `.opencode/logs/tool-errors.log`
  - Use fail-open: log but don't re-throw (let OpenCode handle error propagation)
  - **Input sanitization:** Strip sensitive fields (apiKey, token, password, secret, credential) from args before logging to prevent secret exposure

  **Patterns to follow:**
  - Fail-open from block-push plugin - log errors, continue execution

  **Test scenarios:**
  - [Happy path: Tool error is logged with full context]
  - [Edge case: Log file missing - create directory if needed]

  **Verification:**
  - `.opencode/logs/tool-errors.log` contains the error entry

- [ ] **Unit 3: Implement skill/agent name suggestion**

  **Goal:** Detect resolution errors and suggest correct names back to the model

  **Requirements:** R2

  **Dependencies:** Unit 2

  **Files:**
  - Modify: `.opencode/plugins/tool-resilience.js`

  **Approach:**
  - Detect error patterns: "unknown skill", "unknown agent", "command not found", "skill not found"
  - Parse the attempted name from error message
  - Construct suggestion based on known skills/agents in the system
  - Modify `output.result` to inject hint: "Did you mean 'X'? Try: Y"

  **Technical design:**

  ```javascript
  // Error message patterns to detect
  const namePatterns = [
    /unknown skill ['"]([^'"]+)['"]/i,
    /unknown agent ['"]([^'"]+)['"]/i,
    /command not found ['"]([^'"]+)['"]/i,
    /skill ['"]([^'"]+)['"] not found/i,
  ];
  // On match: inject suggestion into output.result
  ```

  **Patterns to follow:**
  - Error prefix pattern from block-push for distinguishing

  **Test scenarios:**
  - [Error path: Unknown skill 'ce:brainstom' triggers suggestion for 'ce:brainstorm']
  - [Error path: Unknown agent 'exlore' triggers suggestion for 'explore']

  **Verification:**
  - Model receives hint in next prompt context

- [ ] **Unit 4: Implement Brave/Exa rate limit detection**

  **Goal:** Detect 429 errors from web search tools

  **Requirements:** R3

  **Dependencies:** Unit 2

  **Files:**
  - Modify: `.opencode/plugins/tool-resilience.js`

  **Approach:**
  - Match tool name: `brave-search_brave_web_search`, `websearch` (Brave), `codesearch` (Exa)
  - Check error message: "429", "rate limit", "too many requests"
  - Extract retry delay from error or use default (Brave: 1100ms, Exa: 100ms)
  - Return modified output that signals "retry needed"

  **Technical design:**

  ```javascript
  // Tool to delay mapping
  const throttleDelays = {
    "brave-search_brave_web_search": 1100, // 1s + 100ms buffer
    websearch: 1100,
    codesearch: 100, // Exa 10 QPS
    "exa-search": 100,
  };
  ```

  **Test scenarios:**
  - [Edge case: Rate limit on first Brave search succeeds with delay]
  - [Error path: Non-rate-limit 429 (other API) passes through unmodified]

  **Verification:**
  - Rate-limited tool call is identified and prepared for retry

- [ ] **Unit 5: Implement auto-retry with delay**

  **Goal:** Re-execute the tool call after delay, preserving the original request

  **Requirements:** R4, R5

  **Dependencies:** Unit 4

  **Files:**
  - Modify: `.opencode/plugins/tool-resilience.js`

  **Approach:**
  - After detecting rate limit, delay using `setTimeout`
  - Re-invoke the same tool with original args
  - Log the retry event to `.opencode/logs/rate-limits.log`
  - Return the retry result as if original call succeeds
  - **Idempotency check:** For non-read operations, use Strategy B instead of auto-retry to prevent duplicate side effects

  **Technical design:**

  ```javascript
  // Idempotent tools that can be safely auto-retried
  const idempotentTools = new Set([
    "brave-search_brave_web_search",
    "websearch",
    "codesearch",
    "exa-search",
    "brave-search",
    "context7_query-docs",
    "context7_resolve-library-id",
  ]);

  const delayMs = throttleDelays[input.tool] || 1000;

  // Check idempotency before retry
  if (!idempotentTools.has(input.tool)) {
    // Fall back to Strategy B: signal model to retry
    output.hint = `Rate limited. Wait ${delayMs}ms and retry.`;
    return output;
  }

  await new Promise((r) => setTimeout(r, delayMs));

  // Re-execute tool
  const result = await ctx.client.callTool(input.tool, input.args);

  // Log retry
  logToFile(
    "rate-limits.log",
    `RETRY_SUCCESS tool=${input.tool} delay=${delayMs}ms`,
  );
  ```

  **Integration notes:**
  - The retry requires executing the tool again - may need to use client.app.callTool or similar
  - Alternative: set output to signal model to retry with delay hint instead

  **Test scenarios:**
  - [Happy path: First 429 retried after 1.1s delay, succeeds]
  - [Edge case: Retry also hits rate limit - single retry only, then fail]
  - [Edge case: Rate limit log file missing - create if needed]

  **Verification:**
  - No tool call is lost to rate limiting
  - `.opencode/logs/rate-limits.log` shows retry events

- [ ] **Unit 6: End-to-end testing**

  **Goal:** Verify all error types are handled correctly

  **Requirements:** R1, R2, R3, R4, R5

  **Dependencies:** Units 1-5

  **Files:**
  - Test: Manual testing via OpenCode session

  **Approach:**
  - Test with intentional skill typo - should suggest correction
  - Test with rapid Brave search - should retry after delay
  - Verify logs are created

  **Test scenarios:**
  - [Integration: Skill typo triggers suggestion, model retries correctly]
  - [Integration: Rapid web search triggers retry, succeeds after delay]
  - [Integration: Both log files exist with entries]

  **Verification:**
  - All requirements trace to passing test scenarios
  - No tool calls lost in normal operation

## System-Wide Impact

- **Interaction graph:** Hooks into `tool.execute.error` - central error path
- **Error propagation:** Errors logged but not blocked (fail-open)
- **State lifecycle:** Per-session state via Map (rate limit tracking)
- **Unchanged invariants:** Other hooks work unchanged

## Risks & Dependencies

| Risk                                                  | Mitigation                                     |
| ----------------------------------------------------- | ---------------------------------------------- |
| Retry recursion (rate limit on retry)                 | Single retry only, then fail with logged error |
| Hook timing (error fires before/after retry possible) | Test with actual OpenCode hook lifecycle       |
| Log file permission errors                            | Fail-open: log to console if file fails        |

## Documentation / Operational Notes

- Add plugin to `opencode.json` if not auto-discovered from `.opencode/plugins/`
- Log files location: `.opencode/logs/tool-errors.log`, `.opencode/logs/rate-limits.log`
- Create log directory on first use if missing

## Sources & References

- OpenCode plugin docs: `@opencode-ai/plugin` package
- Issue #10027: `tool.execute.error` hook (merged Feb 2026)
- Brave Search API rate limits: api-dashboard.search.brave.com
- Exa API rate limits: exa.ai/docs/reference/rate-limits
