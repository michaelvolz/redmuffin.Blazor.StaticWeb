---
title: feat/opentelemetry-hello-world
date: 2026-06-08
status: draft
---

## Problem

The app has no observability beyond browser DevTools. We need
OpenTelemetry tracing as a Mediator pipeline behavior and an Azure
Function relay to Grafana Cloud — but the standard OTLP exporter NuGet
package drags in `Google.Protobuf`, which would double the WASM
download size. We need standard OTel instrumentation without the
payload penalty.

## Solution

A Mediator pipeline behavior (`TelemetryBehavior`) wraps every request
in an OpenTelemetry Activity using `OpenTelemetry.Api` only. Completed
Activities are buffered and exported through a mode-switched endpoint:
in synthetic mode (localhost), serialized OTLP output goes to the
browser console; in production, a lightweight custom OTLP/HTTP
serializer (standard wire format, no `Google.Protobuf`) POSTs to a
new `/api/telemetry` Azure Function that relays to Grafana Cloud.
Exceptions thrown by any handler are recorded as OTLP span events
(type, message, truncated stack trace) on the failed span — making
every error across the Blazor app traceable. The ApiHealth page
serves as the proof surface — click the button, see the trace in
DevTools (dev) or Grafana Cloud (production).

## Success Metrics

- Clicking "Call ApiHealth" in production produces a trace visible in
  Grafana Cloud showing the `GetHelloQuery` span with all 5 attributes
  (`mediator.request_type`, `mediator.response_type`, `service.name`,
  `service.version`, `outcome`) and correct duration. In synthetic
  mode (localhost), the same trace appears in the browser DevTools
  Console.
- WASM compressed download size increase is negligible — a single
  `OpenTelemetry.Api` package with no exporter payload.
- All existing tests pass unchanged.

## Key Technical Decisions

- **Decision:** Custom OTLP/HTTP trace serializer hand-encoding
  protobuf instead of `OpenTelemetry.Exporter.OpenTelemetryProtocol`.
  **Why:** `Google.Protobuf` adds 500 KB–1.5 MB to the WASM payload
  after trimming — a 2× increase. The Aspire team validated this
  pattern for Blazor WASM (aspire#15691).

- **Decision:** Telemetry Buffer in browser memory, flushed on
  piggyback (next HTTP call) with a 15s fallback timer. **Why:** no
  extra HTTP calls in the normal case, no lost traces on idle.

- **Decision:** `OpenTelemetry.Api` on WASM, full
  `OpenTelemetry.Exporter.OpenTelemetryProtocol` deferred to the
  Function (B-mode conversion later). **Why:** keeps WASM lean; the
  Function has no size constraints.

- **Decision:** Traces only for hello-world. Metrics and logs deferred.
  **Why:** traces form the backbone — metrics and logs are additive
  layers on the same infrastructure.

- **Decision:** Telemetry failure must never block Mediator request
  execution. Exporter flushes and serializer errors are caught, logged,
  and discarded.

- **Decision:** Every handler exception is recorded as an OTLP span
  event (type, message, truncated stack trace at 10 frames). **Why:**
  exceptions are the highest-priority signal — more important than
  duration or success counts. Span events surface them natively in
  Grafana Tempo without a separate error-tracking product.

## Modules & Seams

| Module              | Path                                                      | Change                                                                                                                                                                                                                                                                                                                                                                 | Test surface                                                                                                           |
| ------------------- | --------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| Common (pipeline)   | `src/redmuffin.Blazor.StaticWeb.Common/`                  | New: `TelemetryBehavior.cs`, `TelemetryBehavior.Logging.cs`, `OtlpTraceSerializer.cs`, `TelemetryBuffer.cs`, `TelemetryBufferExporter.cs`. Exporter detects synthetic mode via `IWebAssemblyHostEnvironment`: console in dev, HTTP POST to relay in production. Modified: `MediatorServiceExtensions.cs` — register TelemetryBehavior. New NuGet: `OpenTelemetry.Api`. | Serializer unit tests (round-trip Activity → protobuf), buffer flush logic, exporter mode-switch tests, behavior tests |
| Host (Blazor app)   | `src/redmuffin.Blazor.StaticWeb/`                         | Modified: `Program.cs` — register TelemetryBehavior after LoggingBehavior. Modified: `wwwroot/appsettings.json` — add `Telemetry:Buffer:MaxItems` and `Telemetry:Buffer:FlushIntervalSeconds`.                                                                                                                                                                         | Integration: Mediator pipeline runs TelemetryBehavior on ApiHealth click                                               |
| Azure Functions API | `src/redmuffin.Blazor.StaticWeb.Api/`                     | New: `Functions/TelemetryRelay.cs`, `Functions/TelemetryRelay.Logging.cs`. Modified: `local.settings.json` — add `GrafanaCloud:OtlpEndpoint` and `GrafanaCloud:ApiToken`.                                                                                                                                                                                              | Relay function tests (receives OTLP bytes, forwards with correct auth headers)                                         |
| ApiHealth.Tests     | `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth.Tests/` | New: `TelemetryBehaviorTests.cs` — mirrors LoggingBehaviorTests pattern. New: `OtlpTraceSerializerTests.cs`.                                                                                                                                                                                                                                                           | Behavior attributes set, buffer enqueues, serializer produces valid OTLP                                               |
| Api.Tests           | `tests/redmuffin.Blazor.StaticWeb.Api.Tests/`             | New: `TelemetryRelayTests.cs` — Function receives payload, forwards with auth headers                                                                                                                                                                                                                                                                                  | Relay pass-through and error handling                                                                                  |

## Testing Strategy

Tests are written before implementation (see `rm-tdd` and `rm-testing` for
conventions: TUnit, plain-English underscore naming, no method-name prefix).
All tests verify behavior at seams without a live Grafana backend — the
relay function's `HttpClient` is faked at the seam.

- **TelemetryBehavior**: unit-tested like LoggingBehavior — instantiate
  with a Logger spy, call Handle with a GetHelloQuery, assert Activity
  attributes (request type, response type, outcome) and that the handler
  still returns the correct response. Separate test: handler throws →
  outcome is "failure," span event recorded with exception type, message,
  and truncated stack trace.
- **OtlpTraceSerializer**: unit-tested with a completed Activity —
  round-trip via deserialization (or snapshot of known-good OTLP bytes).
  Assert field numbers, varint encoding, attribute presence.
- **TelemetryBuffer+Exporter**: unit-tested — enqueue Activities, assert
  flush on piggyback and fallback timer. In synthetic mode, assert output
  written to console (verify via `StringWriter`). In production mode, fake
  `HttpClient` to verify POST payload and auth header.
- **TelemetryRelay**: unit-tested — POST OTLP bytes, assert forwarded to
  Grafana endpoint with correct `Authorization` header. Assert error
  status codes on upstream failure.
- **Integration smoke test**: existing ApiHealthTests still pass —
  Mediator pipeline unchanged from the handler's perspective.
- **Manual dev-mode verification**: click "Call ApiHealth" on localhost →
  DevTools Console shows serialized OTLP trace output with correct span
  name and attributes.

## Non-Functional Requirements

- **WASM payload**: the `OpenTelemetry.Api` NuGet package is the only
  new dependency on WASM. No `Google.Protobuf`, no exporter SDK. The
  payload delta should be negligible — if it exceeds 200 KB compressed
  (Brotli), investigate before proceeding.
- **Resilience**: Telemetry export failures (network down, Function
  unreachable, Grafana rejects) must not surface as Mediator errors to
  the user. Behavior catches, logs at Warning level, discards buffer
  contents.
- **Configurability**: Buffer limits (`MaxItems`, `FlushIntervalSeconds`)
  configurable in `appsettings.json` without rebuild.

## Out of Scope

- Metrics and logs — traces only for this PRD.
- Sampling, filtering, or enrichment on the Function (B-mode: later).
- Browser-side instrumentation beyond Mediator requests (no HttpClient
  auto-instrumentation, no page-render spans).
- Grafana Cloud dashboard setup — the trace reaches Grafana, viewing
  it there is manual verification.
- `Google.Protobuf` NuGet package anywhere in the repo.

## Assumptions

- Grafana Cloud account exists with an active API token scoped to
  `traces:write`, `metrics:write`, `logs:write`. OTLP endpoint:
  `https://otlp-gateway-prod-eu-west-0.grafana.net/otlp` (EU/Germany).
- The Functions project's `local.settings.json` accepts
  `GrafanaCloud:OtlpEndpoint` and `GrafanaCloud:ApiToken` entries
  (standard Azure Functions configuration model).
- CORS on the Functions project allows the `/api/telemetry` endpoint
  from the WASM origin (already configured — `"CORS": "*"` in
  `local.settings.json`).
- Configurable buffer values fit within `appsettings.json` constraints
  (Blazor WASM reads `builder.Configuration` from `wwwroot/appsettings.json`
  by default).

## Acceptance Criteria

- [ ] `dotnet build` succeeds with zero warnings.
- [ ] `TelemetryBehaviorTests.Records_request_and_response_type_attributes` passes.
- [ ] `TelemetryBehaviorTests.Outcome_is_success_when_handler_returns` passes.
- [ ] `TelemetryBehaviorTests.Outcome_is_failure_when_handler_throws` passes.
- [ ] `TelemetryBehaviorTests.Records_exception_as_span_event_on_failure` passes.
- [ ] `TelemetryBehaviorTests.Truncates_stack_trace_to_10_frames` passes.
- [ ] `OtlpTraceSerializerTests.Round_trip_produces_valid_otlp_bytes` passes.
- [ ] `TelemetryBufferExporterTests.Synthetic_mode_writes_to_console` passes.
- [ ] `TelemetryBufferExporterTests.Production_mode_posts_to_relay` passes.
- [ ] `TelemetryRelayTests.Forwards_otlp_payload_with_auth_header` passes.
- [ ] `TelemetryRelayTests.Returns_502_when_upstream_unreachable` passes.
- [ ] Existing `ApiHealthTests.*` pass unchanged.
- [ ] Existing `LoggingBehaviorTests.Logs_before_and_after_handler_execution` passes.
- [ ] `dotnet publish` WASM compressed size delta under 200 KB vs baseline
      (investigate if exceeded).
- [ ] Manual: click "Call ApiHealth", observe trace in Grafana Cloud with span
      name `GetHelloQuery` and attributes `mediator.request_type`,
      `mediator.response_type`, `service.name`, `service.version`, `outcome`.
