namespace redmuffin.Tools.QualityGates.Commands;

using redmuffin.Tools.QualityGates.Analysis;

/// <summary>
///     Handler for the dupes (duplicate code detection) gate.
///     Exit codes: 0 = completed, 1 = error, 2 = violations found.
/// </summary>
public static class DupesHandler
{
    public static (int ExitCode, List<DupesCandidate> Candidates) Run(DupesOptions options)
    {
        try
        {
            var candidates = DupesDetector.FindDuplicates(options);
            var exitCode = candidates.Count > 0 ? 2 : 0;
            return (exitCode, candidates);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return (1, []);
        }
    }
}
