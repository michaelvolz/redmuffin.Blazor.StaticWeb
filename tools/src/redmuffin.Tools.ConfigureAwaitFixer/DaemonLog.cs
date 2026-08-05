using System.Globalization;

namespace redmuffin.Tools.ConfigureAwaitFixer;

/// <summary>
///     Append-only daemon log, defaulting to
///     <c>~/.grok/logs/configureawait-daemon.log</c> (overridable via
///     <c>CONFIGUREAWAITFIXER_LOG</c>, e.g. by tests). A logging failure must
///     never take the daemon or the client down; the failed message is
///     remembered and attached to the next log line that does get written, so
///     the problem stays visible instead of vanishing.
/// </summary>
public static class DaemonLog
{
    private static readonly Lock SyncRoot = new();
    private static string? _lastWriteError;

    /// <summary>
    ///     Gets the log file path.
    /// </summary>
    public static string LogPath =>
        Environment.GetEnvironmentVariable("CONFIGUREAWAITFIXER_LOG") is { Length: > 0 } configured
            ? configured
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".grok",
                "logs",
                "configureawait-daemon.log");

    /// <summary>
    ///     Writes an informational line to the log.
    /// </summary>
    public static void Info(string message) => Write("INFO", message);

    /// <summary>
    ///     Writes an error line to the log.
    /// </summary>
    public static void Error(string message) => Write("ERROR", message);

    private static void Write(string level, string message)
    {
        var line = string.Format(
            CultureInfo.InvariantCulture,
            "[{0}] {1} {2}",
            DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
            level,
            message);

        lock (SyncRoot)
        {
            if (_lastWriteError is not null)
            {
                line += $" [previous log write failed: {_lastWriteError}]";
                _lastWriteError = null;
            }

            try
            {
                var directory = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                // A logging failure must never crash the daemon or the
                // client, but it must not vanish either: remember it and
                // attach it to the next log line that does get written.
                _lastWriteError = ex.Message;
            }
        }
    }
}

// clj-mutate-manifest-begin
// {"version":1,"testedAt":"2026-08-04T19:44:28.8197086Z","moduleHash":"73705260ed8bde1d3b8bcd4b51481d84530d8fb8da2365ac29aa171de37c4dac","forms":[{"id":"Info","line":32,"endLine":32,"hash":"98f21dd71ef874fd619c68508be8657afa97a47c3048597bc75ed1fb721055d8"},{"id":"Error","line":37,"endLine":37,"hash":"4d4a0b65279bf396d909e662284a8a85e4ee008783dacbcc35e5bbcfad3086d9"},{"id":"Write","line":39,"endLine":71,"hash":"8b965d8bf3def09e5c6822c025ddb9ec673e29513051ab099b4f443cf4b93786"}]}
// clj-mutate-manifest-end
