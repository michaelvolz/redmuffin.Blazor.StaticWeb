---
date: 2026-06-08
status: accepted
---

# Custom OTLP Trace Exporter for Blazor WASM

The `OpenTelemetry.Exporter.OpenTelemetryProtocol` NuGet package depends on
`Google.Protobuf`, which adds 500 KB–1.5 MB to the WASM download after
trimming — roughly doubling the current ~2 MB compressed payload. This is
unacceptable for a static web app where page load speed is a user-facing
metric.

Instead, the WASM client uses only `OpenTelemetry.Api` (~50 KB trimmed) and
hand-encodes the OTLP trace protobuf format in a custom serializer (~200
lines of C#). The wire format is standard OTLP/HTTP — any compliant
receiver (Grafana Cloud, OTel Collector, Aspire Dashboard) can ingest it.

Microsoft's Aspire team shipped the same pattern for their Blazor WASM
integration (see `WebAssemblyOtlpTraceExporter.cs` in
[aspire#15691](https://github.com/microsoft/aspire/pull/15691)).

**Considered alternative**: Accept the `Google.Protobuf` dependency. Rejected
because a 2× payload increase makes the app non-viable for users on slow
connections — the core problem the Page Load Speed feature was built to
measure and optimize.
