namespace redmuffin.Tools.ConfigureAwaitFixer;

/// <summary>
///     The execution mode selected by the command-line arguments.
/// </summary>
public enum FixerMode
{
    /// <summary>
    ///     The legacy one-shot mode (positional target directory or
    ///     <c>--file &lt;path&gt;</c>): opens MSBuildWorkspace, fixes all
    ///     violations, exits. Used by OpenCode, which keeps this proven path.
    /// </summary>
    OneShot,

    /// <summary>
    ///     The daemon client mode (<c>--fix &lt;file&gt;</c>): hands the file
    ///     to a spawn-on-demand daemon and fails loudly (stderr + exit 1) when
    ///     the daemon crashes or misbehaves. There is deliberately no fallback.
    /// </summary>
    Fix,

    /// <summary>
    ///     The daemon server mode (<c>--daemon</c>): serves fix requests over
    ///     the named pipe and exits after 15 minutes of inactivity.
    /// </summary>
    Daemon,
}
