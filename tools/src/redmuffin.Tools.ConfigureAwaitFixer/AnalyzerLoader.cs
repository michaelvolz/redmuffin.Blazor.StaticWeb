using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;

namespace redmuffin.Tools.ConfigureAwaitFixer;

/// <summary>
///     Loads the official Microsoft CA2007 analyzer from the NetAnalyzers DLLs.
/// </summary>
public static class AnalyzerLoader
{
    public static ImmutableArray<DiagnosticAnalyzer> Load()
    {
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
                Console.Error.WriteLine($"Analyzer DLL not found: {dll}");
                continue;
            }

            var assembly = Assembly.LoadFrom(dll);
            var loaded = assembly.GetTypes()
                .Where(t => typeof(DiagnosticAnalyzer).IsAssignableFrom(t) && !t.IsAbstract)
                .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t)!);
            analyzers = analyzers.AddRange(loaded);
        }

        return analyzers;
    }
}
