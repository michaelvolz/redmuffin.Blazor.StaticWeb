using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Pages.Debug.FetchProbe;

#pragma warning disable MA0049 // Type name matches namespace — standard Blazor component pattern
public partial class FetchProbe : ComponentBase
#pragma warning restore MA0049
{
    private readonly HttpClient _http;
    private readonly ILogger<FetchProbe> _logger;
    private bool _isRunning;
    private bool _hasError;
    private string _errorMessage = string.Empty;
    private bool? _reached;
    private string? _apiError;
    private long? _statusCode;
    private string? _contentType;
    private long? _contentLength;
    private bool? _bodyRead;
    private long? _byteCount;
    private string? _reason;

    public FetchProbe(HttpClient http, ILogger<FetchProbe> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private bool HasResult => _reached is not null;

    private async Task RunProbeAsync()
    {
        _isRunning = true;
        _hasError = false;
        _errorMessage = string.Empty;
        _reached = null;
        _apiError = null;
        _statusCode = null;
        _contentType = null;
        _contentLength = null;
        _bodyRead = null;
        _byteCount = null;
        _reason = null;

        try
        {
            LogProbeStarted(_logger);

            var json = await _http.GetStringAsync("/api/CoverFetchProbe").ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidOperationException(
                    "The probe endpoint returned an empty response — the API is not deployed on this host."
                );
            }

            ParseResult(json);

            LogProbeCompleted(_logger);
        }
        catch (Exception ex)
        {
            LogProbeFailed(_logger, ex);
            _hasError = true;
            _errorMessage = ex.Message;
        }
        finally
        {
            _isRunning = false;
            StateHasChanged();
        }
    }

    // JsonDocument reads the payload without type metadata, so the page stays trim-safe.
    private void ParseResult(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        _reached =
            root.TryGetProperty("reached", out var reached)
            && reached.ValueKind == JsonValueKind.True;
        _apiError = GetStringOrNull(root, "error");
        _statusCode = GetNumberOrNull(root, "statusCode");
        _contentType = GetStringOrNull(root, "contentType");
        _contentLength = GetNumberOrNull(root, "contentLength");
        _bodyRead = root.TryGetProperty("bodyRead", out var bodyRead)
            ? bodyRead.ValueKind == JsonValueKind.True
            : null;
        _byteCount = GetNumberOrNull(root, "byteCount");
        _reason = GetStringOrNull(root, "reason");
    }

    private static string? GetStringOrNull(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static long? GetNumberOrNull(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt64()
            : null;
}
