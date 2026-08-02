namespace redmuffin.Tools.QualityGates.Commands;

using System.Diagnostics;
using System.Globalization;
using redmuffin.Tools.QualityGates.Analysis;

public static class MutateHandler
{
    public static async Task<int> RunAsync(
        string sourcePath, string testProjectPath, MutateOptions options,
        TextWriter? output = null)
    {
        output ??= Console.Out;

        if (await CheckSourceFileMissingAsync(sourcePath, output).ConfigureAwait(false)) return 1;

        return await RunMutationCoreAsync(sourcePath, testProjectPath, options, output).ConfigureAwait(false);
    }

    public static async Task<int> RunMutationCoreAsync(
        string sourcePath, string testProjectPath, MutateOptions options, TextWriter output)
    {
        var (sites, allSites, covered, uncovered, changedCount, existingManifest, strippedSource) =
            await DiscoverSitesAsync(sourcePath, testProjectPath, options, output).ConfigureAwait(false);
        if (sites is null) return 1;

        await PrintHeaderAsync(sourcePath, allSites, covered, uncovered,
            changedCount, existingManifest, output).ConfigureAwait(false);

        if (options.Scan) return 0;

        return await ExecuteMutationsAsync(sourcePath, testProjectPath, options,
            sites, uncovered, strippedSource, existingManifest, output).ConfigureAwait(false);
    }

    private static async Task<bool> CheckSourceFileMissingAsync(string sourcePath, TextWriter output)
    {
        if (!File.Exists(sourcePath))
        {
            await output.WriteLineAsync($"Error: Source file not found: {sourcePath}").ConfigureAwait(false);
            return true;
        }

        return false;
    }

    public static async
        Task<(IReadOnlyList<MutationSite>? Sites, IReadOnlyList<MutationSite> AllSites,
            IReadOnlyList<MutationSite> Covered, IReadOnlyList<MutationSite> Uncovered,
            int ChangedCount, Manifest? Manifest, string StrippedSource)>
        DiscoverSitesAsync(string sourcePath, string testProjectPath, MutateOptions options, TextWriter output)
    {
        var source = await File.ReadAllTextAsync(sourcePath).ConfigureAwait(false);
        var strippedSource = MutationManifest.Strip(source);
        var existingManifest = MutationManifest.Extract(source);
        var allSites = MutationDiscoverer.FindSites(strippedSource);

        var coveredLines = LoadCoverage(testProjectPath, options, output);
        if (coveredLines is null)
        {
            return (null, allSites, [], [], 0, existingManifest, strippedSource);
        }

        var resolvedLines = await ResolveCoverageLinesAsync(
                testProjectPath, options, coveredLines!, output)
            .ConfigureAwait(false);

        var (sites, covered, uncovered, changedCount) = ClassifyAndFilterSites(
            allSites, resolvedLines, sourcePath, strippedSource, existingManifest, options);

        return (sites, allSites, covered, uncovered, changedCount, existingManifest, strippedSource);
    }

    public static (IReadOnlyList<MutationSite> Sites, IReadOnlyList<MutationSite> Covered,
        IReadOnlyList<MutationSite> Uncovered, int ChangedCount)
        ClassifyAndFilterSites(
            IReadOnlyList<MutationSite> allSites,
            IReadOnlySet<string> coveredLines,
            string sourcePath,
            string strippedSource,
            Manifest? existingManifest,
            MutateOptions options)
    {
        var (covered, uncovered) = CoverageReader.PartitionByCoverage(allSites, sourcePath, coveredLines);
        var sites = new List<MutationSite>(covered);
        var changedCount = ApplyDifferentialFilter(sites, strippedSource, existingManifest, options);

        // When coverage is absent and auto-coverage failed, mutate all sites
        if (sites.Count == 0 && uncovered.Count == allSites.Count)
        {
            sites = new List<MutationSite>(allSites);
            changedCount = sites.Count;
            covered = allSites;
            uncovered = [];
        }

        if (options.Lines is { Count: > 0 })
        {
            sites = [.. sites.Where(s => options.Lines.Contains(s.Line))];
        }

        return (sites, covered, uncovered, changedCount);
    }

    private static async Task<int> ExecuteMutationsAsync(
        string sourcePath, string testProjectPath, MutateOptions options,
        IReadOnlyList<MutationSite> sites, IReadOnlyList<MutationSite> uncovered,
        string strippedSource, Manifest? existingManifest, TextWriter output)
    {
        var testFilter = ResolveTestFilter(sourcePath, testProjectPath, options);

        var results = await MutationRunner.RunAsync(
            sourcePath, sites, testProjectPath, options.TimeoutFactor, testFilter)
            .ConfigureAwait(false);

        if (results.Count == 0)
        {
            await output.WriteLineAsync("FAIL — tests do not pass without mutations")
                .ConfigureAwait(false);
            return 1;
        }

        PrintSummary(results, uncovered, output);
        WriteManifest(sourcePath, strippedSource);
        return 0;
    }

    private static void WriteManifest(string sourcePath, string strippedSource)
    {
        var newManifest = MutationManifest.Build(strippedSource, DateTime.UtcNow);
        var newSource = MutationManifest.Embed(strippedSource, newManifest);
        File.WriteAllText(sourcePath, newSource);
    }

    private static string? ResolveTestFilter(
        string sourcePath, string testProjectPath, MutateOptions options)
    {
        if (options.NoTestFilter)
            return null;

        return TestClassDiscovery.Discover(sourcePath, testProjectPath);
    }

    private static HashSet<string>? LoadCoverage(
        string testProjectPath, MutateOptions options, TextWriter output)
    {
        return LoadCoverageFromPath(CoverageFilePath(testProjectPath), options, output);
    }

    private static HashSet<string>? LoadCoverageFromPath(
        string coveragePath, MutateOptions options, TextWriter output)
    {
        if (File.Exists(coveragePath))
        {
            return [.. CoverageReader.LoadCoverage(coveragePath)];
        }

        return CoverageFileMissing(options, output);
    }

    private static HashSet<string>? CoverageFileMissing(MutateOptions options, TextWriter output)
    {
        if (options.ReuseCoverage)
        {
            output.Write("Error: --reuse-coverage specified but no coverage file found.");
            return null;
        }

        return [];
    }

    public static int ApplyDifferentialFilter(
        IList<MutationSite> sites, string strippedSource, Manifest? existingManifest,
        MutateOptions options)
    {
        if (existingManifest is null || options.MutateAll)
        {
            return sites.Count;
        }

        return FilterSitesByManifest(sites, strippedSource, existingManifest);
    }

    private static int FilterSitesByManifest(
        IList<MutationSite> sites, string strippedSource, Manifest existingManifest)
    {
        var currentManifest = MutationManifest.Build(strippedSource, DateTime.UtcNow);
        var changedIndices = MutationManifest.ChangedFormIndices(existingManifest, currentManifest);
        if (changedIndices.Count == 0)
        {
            return sites.Count;
        }

        var changedLines = CollectChangedLines(changedIndices, currentManifest);
        var filtered = sites.Where(s => changedLines.Contains(s.Line)).ToList();
        sites.Clear();
        foreach (var site in filtered)
        {
            sites.Add(site);
        }

        return sites.Count;
    }

    private static HashSet<int> CollectChangedLines(
        IEnumerable<int> changedIndices, Manifest currentManifest)
    {
        var lines = new HashSet<int>();
        foreach (var idx in changedIndices)
            CollectFormLines(idx, currentManifest, lines);
        return lines;
    }

    private static void CollectFormLines(int idx, Manifest manifest, HashSet<int> lines)
    {
        if (idx < manifest.Forms.Count)
            AddFormLines(manifest.Forms[idx], lines);
    }

    private static void AddFormLines(FormEntry form, HashSet<int> lines)
    {
        for (var l = form.Line; l <= form.EndLine; l++)
            lines.Add(l);
    }

    private static async Task PrintHeaderAsync(
        string sourcePath, IReadOnlyList<MutationSite> allSites,
        IReadOnlyList<MutationSite> covered, IReadOnlyList<MutationSite> uncovered,
        int changedCount, Manifest? existingManifest, TextWriter output)
    {
        foreach (var line in BuildHeaderLines(sourcePath, allSites.Count,
            covered.Count, uncovered.Count, changedCount, existingManifest))
        {
            await output.WriteLineAsync(line).ConfigureAwait(false);
        }

        await output.WriteLineAsync().ConfigureAwait(false);
    }

    public static IReadOnlyList<string> BuildHeaderLines(
        string sourcePath, int totalSites, int coveredSites, int uncoveredSites,
        int changedCount, Manifest? existingManifest)
    {
        var ci = CultureInfo.InvariantCulture;
        return
        [
            $"=== Mutation Testing: {sourcePath} ===",
            $"Previous mutation test: {existingManifest?.TestedAt.ToString("O", ci) ?? "none"}",
            $"Total mutation sites: {totalSites.ToString(ci)}",
            $"Covered mutation sites: {coveredSites.ToString(ci)}",
            $"Uncovered mutation sites: {uncoveredSites.ToString(ci)}",
            $"Changed mutation sites: {changedCount.ToString(ci)}",
            $"Manifest exists: {(existingManifest is not null ? "yes" : "no")}",
            $"Module hash changed: {HashChanged(existingManifest, changedCount)}",
            $"Differential surface area: {changedCount.ToString(ci)} mutations in changed forms",
        ];
    }

    private static void PrintSummary(
        IReadOnlyList<MutantResult> results, IReadOnlyList<MutationSite> uncovered,
        TextWriter output)
    {
        foreach (var line in BuildSummaryLines(results, uncovered))
            output.WriteLine(line);
    }

    public static IReadOnlyList<string> BuildSummaryLines(
        IReadOnlyList<MutantResult> results, IReadOnlyList<MutationSite> uncovered)
    {
        var ci = CultureInfo.InvariantCulture;
        var killed = results.Count(r => r.Result == MutantResultType.Killed);
        var survived = results.Where(r => r.Result == MutantResultType.Survived).ToList();
        var noOps = results.Where(r => r.Result == MutantResultType.NoOp).ToList();
        var errors = results.Count(r => r.Result == MutantResultType.Error);
        // Kill rate only over mutants that actually rewrote source and ran tests.
        var totalTested = killed + survived.Count;
        var killPct = totalTested == 0
            ? 0.0
            : 100.0 * killed / totalTested;

        var lines = new List<string>
        {
            "=== Summary ===",
            $"{killed.ToString(ci)}/{totalTested.ToString(ci)} mutants killed ({killPct.ToString("F1", ci)}%)",
            $"{uncovered.Count.ToString(ci)} uncovered mutations skipped",
            $"{noOps.Count.ToString(ci)} apply no-ops (source unchanged)",
        };

        if (errors > 0)
            lines.Add($"{errors.ToString(ci)} mutant run errors");

        if (survived.Count > 0)
        {
            lines.Add("Survivors:");
            foreach (var s in survived)
            {
                lines.Add($"  #{s.SiteIndex.ToString(ci)}  L{s.Line.ToString(ci)}   {s.Description}");
            }
        }

        if (noOps.Count > 0)
        {
            lines.Add("Apply no-ops:");
            foreach (var s in noOps)
            {
                lines.Add($"  #{s.SiteIndex.ToString(ci)}  L{s.Line.ToString(ci)}   {s.Description}");
            }
        }

        return lines;
    }

    private static string HashChanged(Manifest? existingManifest, int changedCount) =>
        existingManifest is not null && changedCount > 0 ? "yes" : "n/a";

    private static string CoverageFilePath(string testProjectPath) =>
        Path.Combine(Path.GetDirectoryName(testProjectPath) ?? ".", "coverage.cobertura.xml");

    public static async Task<IReadOnlySet<string>> ResolveCoverageLinesAsync(
        string testProjectPath, MutateOptions options, IReadOnlySet<string> currentCoverage, TextWriter output,
        Func<string, Task<string?>>? generateCoverage = null)
    {
        if (ShouldSkipCoverageGeneration(currentCoverage, options))
            return currentCoverage;

        generateCoverage ??= GenerateCoverageAsync;

        await output.WriteLineAsync("Generating coverage data...").ConfigureAwait(false);
        var generatedPath = await generateCoverage(testProjectPath).ConfigureAwait(false);

        if (!WasCoverageGenerated(generatedPath))
        {
            await output.WriteLineAsync("Warning: Coverage generation failed.")
                .ConfigureAwait(false);
            return currentCoverage;
        }

        var destPath = CoverageFilePath(testProjectPath);
        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
        File.Copy(generatedPath!, destPath, overwrite: true);

        return CoverageReader.LoadCoverage(destPath);
    }

    public static bool ShouldSkipCoverageGeneration(IReadOnlySet<string> currentCoverage, MutateOptions options)
        => currentCoverage.Count > 0 || !options.AutoCoverage;

    public static bool WasCoverageGenerated(string? generatedPath)
        => generatedPath is not null;

    private static Task<string?> GenerateCoverageAsync(string testProjectPath)
        => CoverageRunner.GenerateAsync(testProjectPath);
}
