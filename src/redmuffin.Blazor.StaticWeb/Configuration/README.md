# PageLoadSpeed Configuration

## Overview

The `PageLoadSpeedConfig` class provides centralized configuration for the PageLoadSpeed component behavior.

## Configuration Options

### `PageLoadSpeedConfig.IsEnabled`

- **Type**: `bool`
- **Default**: `true`
- **Description**: Master switch to enable/disable the component entirely

### `PageLoadSpeedConfig.EnableOnLocalhost`

- **Type**: `bool`
- **Default**: `true`
- **Description**: Controls whether the component is displayed on localhost/development environments

### `PageLoadSpeedConfig.AutoLoadDelayMs`

- **Type**: `int`
- **Default**: `1000`
- **Description**: Delay in milliseconds before automatically loading performance metrics

### `PageLoadSpeedConfig.JsInteropTimeoutSeconds`

- **Type**: `int`
- **Default**: `5`
- **Description**: Timeout for JavaScript interop operations

## Usage Examples

### Enable on localhost only (development)

```csharp
PageLoadSpeedConfig.EnableOnLocalhost = true;
```

### Enable on production only (disable on localhost)

```csharp
PageLoadSpeedConfig.EnableOnLocalhost = false;
```

### Disable component entirely

```csharp
PageLoadSpeedConfig.IsEnabled = false;
```

### Adjust auto-load timing

```csharp
PageLoadSpeedConfig.AutoLoadDelayMs = 2000; // 2 second delay
```

### Configure shorter JS timeout

```csharp
PageLoadSpeedConfig.JsInteropTimeoutSeconds = 3;
```

## Where to Configure

Add configuration at application startup (typically in `Program.cs` or similar):

```csharp
// Enable on localhost for development
PageLoadSpeedConfig.EnableOnLocalhost = true;
PageLoadSpeedConfig.AutoLoadDelayMs = 1000;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
// ... rest of configuration
```

## Component Behavior

The component will:

1. Check `PageLoadSpeedConfig.IsEnabled` first
2. If enabled, check `PageLoadSpeedConfig.ShouldDisplayComponent(baseUri)` which:
   - Returns `true` immediately if `EnableOnLocalhost` is `true`
   - Returns `!isLocalhost` if `EnableOnLocalhost` is `false`
3. If displayed, automatically load metrics after `AutoLoadDelayMs` milliseconds
4. Use `JsInteropTimeoutSeconds` for all JavaScript interop timeouts
