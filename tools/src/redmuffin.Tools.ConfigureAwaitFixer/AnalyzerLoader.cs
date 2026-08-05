using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;

namespace redmuffin.Tools.ConfigureAwaitFixer;

/// <summary>
///     Loads the official Microsoft CA2007 analyzer from the NetAnalyzers DLLs.
/// </summary>
public static class AnalyzerLoader
{
    /// <summary>
    ///     The only diagnostic this tool fixes. Filtering by the diagnostic ID
    ///     keeps the loader correct across NetAnalyzers versions — the analyzer
    ///     type name has already changed once (TaskConfigureAwaitAnalyzer →
    ///     DoNotDirectlyAwaitATaskAnalyzer), the diagnostic ID is stable.
    /// </summary>
    private const string Ca2007DiagnosticId = "CA2007";

    /// <summary>
    ///     Loads the analyzers; errors are reported through
    ///     <paramref name="onError"/> (default: stderr). The daemon passes a
    ///     logger so a missing DLL never pollutes its stderr pipes.
    /// </summary>
    public static ImmutableArray<DiagnosticAnalyzer> Load(Action<string>? onError = null)
    {
        onError ??= message => Console.Error.WriteLine(message);

        var analyzerAssemblies = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Microsoft.CodeAnalysis.NetAnalyzers.dll"),
            Path.Combine(AppContext.BaseDirectory, "Microsoft.CodeAnalysis.CSharp.NetAnalyzers.dll"),
        };

        var analyzers = ImmutableArray<DiagnosticAnalyzer>.Empty;

        foreach (var dll in analyzerAssemblies)
        {
            if (!File.Exists(dll))
            {
                onError($"Analyzer DLL not found: {dll}");
                continue;
            }

            var assembly = Assembly.LoadFrom(dll);
            var loaded = assembly.GetTypes()
                .Where(t => typeof(DiagnosticAnalyzer).IsAssignableFrom(t) && !t.IsAbstract)
                .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t)!)
                .Where(a => a.SupportedDiagnostics.Any(d => string.Equals(d.Id, Ca2007DiagnosticId, StringComparison.Ordinal)));
            analyzers = analyzers.AddRange(loaded);
        }

        return analyzers;
    }
}

// clj-mutate-manifest-begin
// {"version":1,"testedAt":"2026-08-04T19:44:28.5379047Z","moduleHash":"e5f3a51d314dc91af4ef11ef6077bda7345f0cc8614dc6fd36e755d3fb039880","forms":[{"id":"Load","line":24,"endLine":53,"hash":"e5f3a51d314dc91af4ef11ef6077bda7345f0cc8614dc6fd36e755d3fb039880"}]}
// clj-mutate-manifest-end
