namespace redmuffin.Tools.QualityGates.Commands;

using System.CommandLine;

public static class MutateCommand
{
    private static readonly Option<string> SourceOption = new("--project")
    {
        Description = "Path to the source file to mutate",
        Required = true,
    };

    private static readonly Option<string> TestProjectOption = new("--test-project")
    {
        Description = "Path to the test project directory",
        Required = true,
    };

    private static readonly Option<bool> ScanOption = new("--scan")
    {
        Description = "Perform structural scan only (no test execution)",
    };

    private static readonly Option<int> MaxWorkersOption = new("--max-workers")
    {
        Description = "Maximum parallel mutation workers",
        DefaultValueFactory = _ => 1,
    };

    private static readonly Option<bool> SinceLastRunOption = new("--since-last-run")
    {
        Description = "Only mutate sites in forms changed since last run",
    };

    private static readonly Option<bool> MutateAllOption = new("--mutate-all")
    {
        Description = "Mutate all sites (ignore manifest)",
    };

    private static readonly Option<IReadOnlySet<int>?> LinesOption = new("--lines")
    {
        Description = "Only mutate specific line numbers (comma-separated)",
    };

    private static readonly Option<int> MutationWarningOption = new("--mutation-warning")
    {
        Description = "Warning threshold for mutation site count",
        DefaultValueFactory = _ => 50,
    };

    private static readonly Option<int> TimeoutFactorOption = new("--timeout-factor")
    {
        Description = "Multiplier on baseline test time for per-mutant timeout",
        DefaultValueFactory = _ => 10,
    };

    private static readonly Option<bool> ReuseCoverageOption = new("--reuse-coverage")
    {
        Description = "Reuse existing coverage data",
    };

    public static Command Create()
    {
        var command = new Command("mutate", "Run mutation testing on a source file")
        {
            SourceOption, TestProjectOption, ScanOption, MaxWorkersOption,
            SinceLastRunOption, MutateAllOption, LinesOption,
            MutationWarningOption, TimeoutFactorOption, ReuseCoverageOption,
        };

        command.SetAction(async parseResult =>
        {
            var source = parseResult.GetValue(SourceOption)!;
            var testProject = parseResult.GetValue(TestProjectOption)!;
            var options = new MutateOptions(
                Scan: parseResult.GetValue(ScanOption),
                MutateAll: parseResult.GetValue(MutateAllOption),
                SinceLastRun: parseResult.GetValue(SinceLastRunOption),
                MaxWorkers: parseResult.GetValue(MaxWorkersOption),
                MutationWarning: parseResult.GetValue(MutationWarningOption),
                TimeoutFactor: parseResult.GetValue(TimeoutFactorOption),
                ReuseCoverage: parseResult.GetValue(ReuseCoverageOption),
                Lines: parseResult.GetValue(LinesOption));

            return await MutateHandler.RunAsync(source, testProject, options).ConfigureAwait(false);
        });

        return command;
    }
}
