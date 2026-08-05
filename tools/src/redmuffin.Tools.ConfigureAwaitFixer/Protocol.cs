using System.Text;
using System.Text.Json;

namespace redmuffin.Tools.ConfigureAwaitFixer;

/// <summary>
///     Wire protocol between the <c>--fix</c> client and the daemon: a named
///     pipe with length-prefixed UTF-8 JSON messages (4-byte big-endian length
///     header). One request/response pair per connection.
/// </summary>
public static class Protocol
{
    private const string PipeNameBase = "redmuffin.configureawaitfixer";
    private const string MutexNameBase = "redmuffin.configureawaitfixer.spawn";
    private const int MaxMessageBytes = 1 << 20;

    /// <summary>
    ///     Environment variable that isolates the pipe and mutex names so test
    ///     daemons never collide with a live daemon.
    /// </summary>
    public const string InstanceEnvVar = "CONFIGUREAWAITFIXER_INSTANCE";

    /// <summary>
    ///     Environment variable pointing the daemon at its log file.
    /// </summary>
    public const string LogEnvVar = "CONFIGUREAWAITFIXER_LOG";

    /// <summary>
    ///     Environment variable overriding the daemon's idle-exit timeout in
    ///     seconds.
    /// </summary>
    public const string IdleEnvVar = "CONFIGUREAWAITFIXER_IDLE_SECONDS";

    /// <summary>
    ///     Shared JSON options: camelCase property names on the wire.
    /// </summary>
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    ///     Gets the named pipe name. <c>CONFIGUREAWAITFIXER_INSTANCE</c>
    ///     suffixes the name so test daemons never collide with a live daemon.
    /// </summary>
    public static string PipeName => PipeNameBase + InstanceSuffix;

    /// <summary>
    ///     Gets the spawn-arbitration mutex name (same isolation suffix as
    ///     <see cref="PipeName"/>).
    /// </summary>
    public static string MutexName => MutexNameBase + InstanceSuffix;

    private static string InstanceSuffix =>
        Environment.GetEnvironmentVariable(InstanceEnvVar) ?? string.Empty;

    /// <summary>
    ///     Writes one JSON message to the pipe.
    /// </summary>
    public static async Task WriteAsync(Stream stream, string json, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        var header = new byte[4];
        header[0] = (byte)(bytes.Length >> 24);
        header[1] = (byte)(bytes.Length >> 16);
        header[2] = (byte)(bytes.Length >> 8);
        header[3] = (byte)bytes.Length;

        await stream.WriteAsync(header, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Reads one JSON message from the pipe.
    /// </summary>
    public static async Task<string> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        var header = new byte[4];
        await ReadExactlyAsync(stream, header, cancellationToken).ConfigureAwait(false);
        var length = (header[0] << 24) | (header[1] << 16) | (header[2] << 8) | header[3];
        if (length < 0 || length > MaxMessageBytes)
        {
            throw new InvalidDataException(
                $"Invalid message length {length.ToString(System.Globalization.CultureInfo.InvariantCulture)}.");
        }

        var buffer = new byte[length];
        await ReadExactlyAsync(stream, buffer, cancellationToken).ConfigureAwait(false);
        return Encoding.UTF8.GetString(buffer);
    }

    private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset), cancellationToken).ConfigureAwait(false);
            if (read == 0)
                throw new EndOfStreamException("Peer closed the pipe mid-message.");
            offset += read;
        }
    }

    /// <summary>
    ///     Creates a success response.
    /// </summary>
    public static FixResponse Success(int fixedAwaits, string message) => new(true, fixedAwaits, message);

    /// <summary>
    ///     Creates a failure response.
    /// </summary>
    public static FixResponse Failure(string message) => new(false, 0, message);

    /// <summary>
    ///     A fix request: the absolute path of the file to fix.
    /// </summary>
    public sealed record FixRequest(string File);

    /// <summary>
    ///     A fix response. Ok=false means the daemon failed loudly and the
    ///     client must exit 1; the message carries the reason.
    /// </summary>
    public sealed record FixResponse(bool Ok, int FixedAwaits, string Message);
}

// clj-mutate-manifest-begin
// {"version":1,"testedAt":"2026-08-04T19:44:29.3654539Z","moduleHash":"0819de37d386800b1e9daf3fe90ca39fefce441b20a6d177997ef1836d4fd4f9","forms":[{"id":"WriteAsync","line":42,"endLine":54,"hash":"54fa777a518e91c0e2ca24127d02434b469cffec054cdea4f8ebaf68c8fd08f0"},{"id":"ReadAsync","line":59,"endLine":73,"hash":"1e093081d7a10307f7c0b9fea7f3325cd79a4658cecf32bdc543a5b48f7ddb15"},{"id":"ReadExactlyAsync","line":75,"endLine":85,"hash":"231f5d1d58a5552ff568f924af3e2745511cae23064ba376cc2a4890509be035"},{"id":"Success","line":90,"endLine":90,"hash":"b77794f166ed99b643086552603294a64d830a0e9e9fe49d6b52995ebe11279c"},{"id":"Failure","line":95,"endLine":95,"hash":"5e9dd17377e5ac49010d9dd1cc9f6518f18fa26fd2fca899caa69cd17c0224fb"}]}
// clj-mutate-manifest-end
