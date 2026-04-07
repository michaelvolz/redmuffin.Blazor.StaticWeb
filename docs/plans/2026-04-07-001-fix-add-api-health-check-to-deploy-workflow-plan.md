---
title: Add API Health Check to Deployment Workflow
type: fix
status: completed
date: 2026-04-07
---

# Add API Health Check to Deployment Workflow

## Overview

Add a post-deployment health check that verifies both the frontend site and Azure Functions API are working. Currently, only the frontend site (`redmuffin.net`) is checked (but doesn't fail on error). The API was previously failing due to package mismatches and went undetected. This fix adds a strict, fast health check that verifies both endpoints and fails the workflow if either check fails.

## Problem Frame

The current deployment workflow only verifies that the frontend site is accessible after deployment. However, if the Azure Functions API has issues (like package mismatches), the deployment reports as successful even though the API is broken. This causes production issues that are only discovered manually.

## Requirements Trace

- R1. After successful deployment, verify both the frontend site and API `/api/HelloWorld` endpoint return HTTP 200
- R2. If either check fails, the workflow must fail immediately with a clear error message
- R3. Both checks must complete within 30 seconds (fast but reliable)

## Scope Boundaries

- Adds a single health check job that verifies both frontend and API
- Replaces the existing slow/lenient site health check with a fast, strict check that fails on any error
- Does not add any new code to the application itself; references to HelloWorld.cs are for context (identifying the endpoint being checked), not because code changes are needed

## Context & Research

### Relevant Code and Patterns

- **Existing workflow:** `.github/workflows/azure-static-web-apps-lively-cliff-0945be603.yml`
- **API endpoint:** `src/redmuffin.Blazor.StaticWeb.Api/Functions/HelloWorld.cs` - returns "Welcome to Azure Functions!" on HTTP GET
- **Existing site health check:** Uses `curl -f -s --max-time 5 --retry 1 --retry-delay 2` to verify `https://redmuffin.net`

### Institutional Learnings

- The existing health check at lines 273-285 already provides a pattern for curl-based verification in the workflow
- The API is served at the same domain, under the `/api/` path

### External References

- Azure Static Web Apps health check patterns

## Key Technical Decisions

- **Decision:** Use the existing HelloWorld endpoint for API verification
  - **Rationale:** This endpoint is simple, always available, and was previously used for warmup. It serves as a reliable indicator that the Azure Functions are running correctly.

- **Decision:** Make the API check failure fail the workflow
  - **Rationale:** The user explicitly requested this - when the API check fails, the action should fail. This ensures package mismatches and other API issues are caught before the deployment is marked complete.

- **Decision:** Run the API health check in a separate job after deployment
  - **Rationale:** Creating a new job provides better isolation and clearer failure handling than the inline site check pattern. Unlike the existing site health check (which only logs a warning), this job will properly fail the workflow on API failure as required by R2.

## Open Questions

### Resolved During Planning

- **Endpoint URLs:**
  - Frontend: `https://redmuffin.net`
  - API: `https://redmuffin.net/api/HelloWorld`
- **Performance requirement:** Both checks must complete within 30 seconds total
- **Retry strategy:** Use 2 retries with 5-second delay between attempts (total: ~15s for retries)
  - Rationale: Avoids false failures from transient network issues while staying under 30s budget
- **Initial wait:** 5 seconds after deployment before first check attempt
- **Timeout per request:** 10 seconds max per curl request

## Implementation Units

- [ ] **Unit 1: Add Fast Health Check Job**

**Goal:** Add a new job to the deployment workflow that verifies both frontend and API are working, failing fast if either check fails

**Requirements:** R1, R2, R3

**Dependencies:** `test_and_build_job` (must complete deployment first)

**Files:**

- Modify: `.github/workflows/azure-static-web-apps-lively-cliff-0945be603.yml`

**Approach:**

- Add a new job `health_check` that runs after `test_and_build_job`
- Wait 5 seconds for deployment to settle
- Run both checks in parallel (using shell background processes) with retry logic:
  - Each check: `curl -f -s --max-time 10 --retry 2 --retry-delay 5`
- Verify API response contains "Welcome to Azure Functions!" using grep
- If either check fails after retries, exit with code 1 to fail the workflow
- Total expected time: ~20-25 seconds (initial wait + parallel checks with retries)

**Patterns to follow:**

- Use curl with retry for reliability: `-f -s --max-time 10 --retry 2 --retry-delay 5`
- Run both checks in parallel using shell background processes for speed
- Exit with code 1 on failure after all retries exhausted
- Verify API response body contains expected string using grep

**Test scenarios:**

- **Verification scenario:** When API returns 200 with expected response, job passes
- **Error path scenario:** When API returns non-200 or times out, job fails with clear error
- **Error path scenario:** When API returns 200 but with unexpected body, job fails

**Verification:**

- The workflow should fail if the API endpoint is unreachable or returns an error
- The workflow should pass when the API returns 200 with "Welcome to Azure Functions!"

## System-Wide Impact

- **Interaction graph:** No application code changes; only CI/CD workflow modification
- **Error propagation:** If API check fails, the entire workflow will show as failed in GitHub Actions

## Risks & Dependencies

| Risk                                          | Mitigation                                                                         |
| --------------------------------------------- | ---------------------------------------------------------------------------------- |
| API not ready immediately after deployment    | 5-second initial wait + retry logic (2 retries with 5s delay) handles slow startup |
| False failure due to transient network issues | Retry logic (2 retries with 5s delay) reduces false positives                      |

## Documentation / Operational Notes

- No user-facing documentation needed
- GitHub Actions workflow logs will show API health check status

## Sources & References

- Related code: `src/redmuffin.Blazor.StaticWeb.Api/Functions/HelloWorld.cs`
- Related workflow: `.github/workflows/azure-static-web-apps-lively-cliff-0945be603.yml`
