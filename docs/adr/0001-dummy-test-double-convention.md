# Standalone WASM Mode for Local Development

The Blazor WASM frontend can run independently of the Azure Functions
backend during local development. In this mode, `DummyRaindropAPI` and a
zero-delay `IDelayProvider` replace the real Raindrop chain, allowing the
app to start instantly without spinning up the Functions runtime.

This is the primary local development workflow. UI changes, component
debugging, and AI-assisted development sessions all use standalone WASM
mode for fast iteration — startup is near-instant, and there is no
dependency on the Functions host or an active Raindrop session.

The `Production*` variants (`ProductionDelayProvider`, `RaindropAPI`) are
used only when the full chain (WASM → Functions → Raindrop) must be
exercised — typically at final integration verification via SwaLauncher.

**Status:** accepted

## Considered Options

- **Always require Functions.** Rejected — startup latency and the
  requirement to maintain a valid Raindrop session make routine UI work
  painfully slow.
- **Mocking framework.** Rejected — hand-rolled `Dummy*` classes return
  real-shaped data and are trivially swappable via DI, without adding a
  mocking library dependency for what is essentially a data-fixture
  problem.
