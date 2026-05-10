namespace redmuffin.Tools.QualityGates.Commands;

using redmuffin.Tools.QualityGates.Analysis;

public static class MutateHandler
{
    public static async Task<int> RunAsync(
        string sourcePath,
        string testProjectPath,
        MutateOptions options,
        TextWriter? output = null)
    {
        output ??= Console.Out;

        if (!File.Exists(sourcePath))
        {
            await output.WriteLineAsync($"Error: Source file not found: {sourcePath}");
            return 1;
        }

        // 1. Read source, strip manifest, discover sites
        var source = await File.ReadAllTextAsync(sourcePath);
        var strippedSource = MutationManifest.Strip(source);
        var existingManifest = MutationManifest.Extract(source);
        var allSites = MutationDiscoverer.FindSites(strippedSource);

        // 2. Load coverage
        var coveragePath = Path.Combine(
            Path.GetDirectoryName(testProjectPath) ?? ".",
            "coverage.cobertura.xml");

        HashSet<int> coveredLines;

        if (File.Exists(coveragePath))
        {
            coveredLines = new HashSet<int>(CoverageReader.LoadCoverage(coveragePath));
        }
        else if (options.ReuseCoverage)
        {
            await output.WriteLineAsync("Error: --reuse-coverage specified but no coverage file found. Run coverage generation first.");
            return 1;
        }
        else
        {
            coveredLines = [];
        }

        var (covered, uncovered) = CoverageReader.PartitionByCoverage(allSites, coveredLines);

        // 3. Apply manifest differential filtering
        var sites = new List<MutationSite>(covered);
        var changedCount = sites.Count;

        if (existingManifest is not null && !options.MutateAll)
        {
            var currentManifest = MutationManifest.Build(strippedSource, DateTime.UtcNow);
            var changedIndices = MutationManifest.ChangedFormIndices(existingManifest, currentManifest);

            if (changedIndices.Count > 0)
            {
                var changedLines = new HashSet<int>();
                foreach (var idx in changedIndices)
                {
                    if (idx < currentManifest.Forms.Count)
                    {
                        var form = currentManifest.Forms[idx];
                        for (var l = form.Line; l <= form.EndLine; l++)
                        {
                            changedLines.Add(l);
                        }
                    }
                }

                sites = sites.Where(s => changedLines.Contains(s.Line)).ToList();
                changedCount = sites.Count;
            }
        }

        // 4. Apply --lines filtering
        if (options.Lines is { Count: > 0 })
        {
            sites = sites.Where(s => options.Lines.Contains(s.Line)).ToList();
        }

        // 5. Print header
        await output.WriteLineAsync($"=== Mutation Testing: {sourcePath} ===");
        await output.WriteLineAsync($"Previous mutation test: {(existingManifest?.TestedAt.ToString("O") ?? "none")}");
        await output.WriteLineAsync($"Total mutation sites: {allSites.Count}");
        await output.WriteLineAsync($"Covered mutation sites: {covered.Count}");
        await output.WriteLineAsync($"Uncovered mutation sites: {uncovered.Count}");
        await output.WriteLineAsync($"Changed mutation sites: {changedCount}");
        await output.WriteLineAsync($"Manifest exists: {(existingManifest is not null ? "yes" : "no")}");
        await output.WriteLineAsync($"Module hash changed: {(existingManifest is not null && !options.MutateAll ? "yes" : "n/a")}");
        await output.WriteLineAsync($"Differential surface area: {changedCount} mutations in changed forms");
        await output.WriteLineAsync();

        // 6. If scan mode, stop here
        if (options.Scan)
        {
            return 0;
        }

        // 7. Run mutations
        var results = await MutationRunner.RunAsync(
            sourcePath, sites.AsReadOnly(), testProjectPath, options.TimeoutFactor);

        if (results.Count == 0)
        {
            await output.WriteLineAsync("FAIL — tests do not pass without mutations");
            return 1;
        }

        var killed = results.Count(r => r.Result == MutantResultType.Killed);
        var survived = results.Where(r => r.Result == MutantResultType.Survived).ToList();
        var totalTested = killed + survived.Count;

        await output.WriteLineAsync("=== Summary ===");
        await output.WriteLineAsync($"{killed}/{totalTested} mutants killed ({100.0 * killed / totalTested:F1}%)");
        await output.WriteLineAsync($"{uncovered.Count} uncovered mutations skipped");

        if (survived.Count > 0)
        {
            await output.WriteLineAsync("Survivors:");
            foreach (var s in survived)
            {
                await output.WriteLineAsync($"  #{s.SiteIndex}  L{s.Line}   {s.Description}");
            }
        }

        // 8. Write updated manifest
        var newManifest = MutationManifest.Build(strippedSource, DateTime.UtcNow);
        var newSource = MutationManifest.Embed(strippedSource, newManifest);
        await File.WriteAllTextAsync(sourcePath, newSource);

        return 0; // survivors are informational per clj-mutate
    }
}
