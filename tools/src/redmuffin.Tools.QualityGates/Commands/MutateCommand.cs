namespace redmuffin.Tools.QualityGates.Commands;

using System.CommandLine;

public static class MutateCommand
{
    public static Command Create()
    {
        var sourceOption = new Option<string>("--project")
        {
            Description = "Path to the source file to mutate",
            Required = true,
        };

        var testProjectOption = new Option<string>("--test-project")
        {
            Description = "Path to the test project directory",
            Required = true,
        };

        var scanOption = new Option<bool>("--scan")
        {
            Description = "Perform structural scan only (no test execution)",
        };

        var maxWorkersOption = new Option<int>("--max-workers")
        {
            Description = "Maximum parallel mutation workers",
            DefaultValueFactory = _ => 1,
        };

        var sinceLastRunOption = new Option<bool>("--since-last-run")
        {
            Description = "Only mutate sites in forms changed since last run",
        };

        var mutateAllOption = new Option<bool>("--mutate-all")
        {
            Description = "Mutate all sites (ignore manifest)",
        };

        var linesOption = new Option<IReadOnlySet<int>?>("--lines")
        {
            Description = "Only mutate specific line numbers (comma-separated)",
        };

        var mutationWarningOption = new Option<int>("--mutation-warning")
        {
            Description = "Warning threshold for mutation site count",
            DefaultValueFactory = _ => 50,
        };

        var timeoutFactorOption = new Option<int>("--timeout-factor")
        {
            Description = "Multiplier on baseline test time for per-mutant timeout",
            DefaultValueFactory = _ => 10,
        };

        var reuseCoverageOption = new Option<bool>("--reuse-coverage")
        {
            Description = "Reuse existing coverage data",
        };

        var command = new Command("mutate", "Run mutation testing on a source file")
        {
            sourceOption,
            testProjectOption,
            scanOption,
            maxWorkersOption,
            sinceLastRunOption,
            mutateAllOption,
            linesOption,
            mutationWarningOption,
            timeoutFactorOption,
            reuseCoverageOption,
        };

        command.SetAction(async parseResult =>
            {
                var source = parseResult.GetValue(sourceOption)!;
                var testProject = parseResult.GetValue(testProjectOption)!;
                var scan = parseResult.GetValue(scanOption);
                var maxWorkers = parseResult.GetValue(maxWorkersOption);
                var sinceLastRun = parseResult.GetValue(sinceLastRunOption);
                var mutateAll = parseResult.GetValue(mutateAllOption);
                var lines = parseResult.GetValue(linesOption);
                var mutationWarning = parseResult.GetValue(mutationWarningOption);
                var timeoutFactor = parseResult.GetValue(timeoutFactorOption);
                var reuseCoverage = parseResult.GetValue(reuseCoverageOption);

                var options = new MutateOptions(
                    Scan: scan,
                    MutateAll: mutateAll,
                    SinceLastRun: sinceLastRun,
                    MaxWorkers: maxWorkers,
                    MutationWarning: mutationWarning,
                    TimeoutFactor: timeoutFactor,
                    ReuseCoverage: reuseCoverage,
                    Lines: lines);

                var exitCode = await MutateHandler.RunAsync(source, testProject, options);
                return exitCode;
            });

        return command;
    }
}
