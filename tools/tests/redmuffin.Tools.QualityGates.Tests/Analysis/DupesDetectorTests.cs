namespace redmuffin.Tools.QualityGates.Tests.Analysis;

using redmuffin.Tools.QualityGates.Analysis;
using redmuffin.Tools.QualityGates.Commands;

[Category("Feature:Dupes")]
public sealed class DupesDetectorTests
{
    [Test]
    public async Task FindDuplicates_reports_structurally_identical_methods()
    {
        var dir = CreateTempDir();
        try
        {
            // Two methods with the same control-flow shape and enough nodes/lines.
            await File.WriteAllTextAsync(Path.Combine(dir, "A.cs"), """
                class A {
                    void Left() {
                        if (flag) {
                            DoWork(value);
                            return;
                        }
                        Log("miss");
                    }
                }
                """).ConfigureAwait(false);
            await File.WriteAllTextAsync(Path.Combine(dir, "B.cs"), """
                class B {
                    void Right() {
                        if (enabled) {
                            Process(item);
                            return;
                        }
                        Log("skip");
                    }
                }
                """).ConfigureAwait(false);

            var options = new DupesOptions(
                Threshold: 0.5, MinLines: 3, MinNodes: 3,
                Format: "text", Paths: [dir]);

            var results = DupesDetector.FindDuplicates(options);

            await Assert.That(results.Count).IsGreaterThanOrEqualTo(1);
            await Assert.That(results[0].Score).IsGreaterThanOrEqualTo(0.5);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task FindDuplicates_respects_high_threshold()
    {
        var dir = CreateTempDir();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "Diff.cs"), """
                class Diff {
                    void Alpha() {
                        if (a) { x = 1; y = 2; z = 3; }
                        else { x = 9; }
                    }
                    void Beta() {
                        while (b) { q = 1; }
                        for (int i = 0; i < 3; i++) { r = i; }
                    }
                }
                """).ConfigureAwait(false);

            var options = new DupesOptions(
                Threshold: 0.99, MinLines: 2, MinNodes: 2,
                Format: "text", Paths: [dir]);

            var results = DupesDetector.FindDuplicates(options);

            await Assert.That(results).IsEmpty();
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task FindDuplicates_accepts_single_file_path()
    {
        var dir = CreateTempDir();
        try
        {
            var file = Path.Combine(dir, "Pair.cs");
            await File.WriteAllTextAsync(file, """
                class Pair {
                    void One() {
                        if (flag) {
                            Work(a);
                            Work(b);
                            return;
                        }
                        Done();
                    }
                    void Two() {
                        if (flag) {
                            Work(a);
                            Work(b);
                            return;
                        }
                        Done();
                    }
                }
                """).ConfigureAwait(false);

            var options = new DupesOptions(
                Threshold: 0.8, MinLines: 3, MinNodes: 3,
                Format: "text", Paths: [file]);

            var results = DupesDetector.FindDuplicates(options);

            await Assert.That(results.Count).IsGreaterThanOrEqualTo(1);
            await Assert.That(results[0].LeftFile).IsEqualTo(Path.GetFullPath(file));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task FindDuplicates_skips_methods_below_min_lines()
    {
        var dir = CreateTempDir();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "Tiny.cs"), """
                class Tiny {
                    void A() { x = 1; }
                    void B() { x = 1; }
                }
                """).ConfigureAwait(false);

            var options = new DupesOptions(
                Threshold: 0.5, MinLines: 20, MinNodes: 1,
                Format: "text", Paths: [dir]);

            var results = DupesDetector.FindDuplicates(options);

            await Assert.That(results).IsEmpty();
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task FindDuplicates_returns_empty_for_empty_directory()
    {
        var dir = CreateTempDir();
        try
        {
            var options = new DupesOptions(
                Threshold: 0.82, MinLines: 4, MinNodes: 20,
                Format: "text", Paths: [dir]);

            var results = DupesDetector.FindDuplicates(options);

            await Assert.That(results).IsEmpty();
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task FormatText_with_candidates_includes_score_and_spans()
    {
        var candidates = new List<DupesCandidate>
        {
            new(Score: 0.91, LeftFile: "A.cs", LeftStartLine: 1, LeftEndLine: 10,
                RightFile: "B.cs", RightStartLine: 2, RightEndLine: 12,
                LeftNodes: 5, RightNodes: 5),
            new(Score: 0.85, LeftFile: "C.cs", LeftStartLine: 3, LeftEndLine: 8,
                RightFile: "D.cs", RightStartLine: 4, RightEndLine: 9,
                LeftNodes: 4, RightNodes: 4),
        };

        var text = DupesOutputFormatter.Format(candidates, "text");

        await Assert.That(text).Contains("DUPLICATE score=0.91");
        await Assert.That(text).Contains("A.cs:1-10");
        await Assert.That(text).Contains("B.cs:2-12");
        await Assert.That(text).Contains("DUPLICATE score=0.85");
    }

    [Test]
    public async Task FindDuplicates_ignores_missing_paths_and_non_cs_files()
    {
        var dir = CreateTempDir();
        try
        {
            var txt = Path.Combine(dir, "notes.txt");
            await File.WriteAllTextAsync(txt, "not csharp").ConfigureAwait(false);
            var missing = Path.Combine(dir, "gone.cs");

            var options = new DupesOptions(
                Threshold: 0.5, MinLines: 1, MinNodes: 1,
                Format: "text", Paths: [missing, txt]);

            var results = DupesDetector.FindDuplicates(options);

            await Assert.That(results).IsEmpty();
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task FindDuplicates_sorts_equal_scores_by_file_then_line()
    {
        var dir = CreateTempDir();
        try
        {
            // Three identical shapes → multiple candidates at the same score.
            await File.WriteAllTextAsync(Path.Combine(dir, "Z.cs"), MethodBody("Za")).ConfigureAwait(false);
            await File.WriteAllTextAsync(Path.Combine(dir, "A.cs"), MethodBody("Aa")).ConfigureAwait(false);
            await File.WriteAllTextAsync(Path.Combine(dir, "M.cs"), MethodBody("Ma")).ConfigureAwait(false);

            var options = new DupesOptions(
                Threshold: 0.5, MinLines: 3, MinNodes: 3,
                Format: "text", Paths: [dir]);

            var results = DupesDetector.FindDuplicates(options);

            await Assert.That(results.Count).IsGreaterThanOrEqualTo(2);
            // Sorted by score desc, then LeftFile ordinal, then LeftStartLine.
            for (var i = 1; i < results.Count; i++)
            {
                var prev = results[i - 1];
                var cur = results[i];
                if (prev.Score == cur.Score)
                {
                    var cmp = string.CompareOrdinal(prev.LeftFile, cur.LeftFile);
                    await Assert.That(cmp <= 0).IsTrue();
                }
            }
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task FindDuplicates_skips_bodyless_methods_in_scan()
    {
        var dir = CreateTempDir();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "Abstract.cs"), """
                abstract class Abs {
                    public abstract void One();
                    public abstract void Two();
                }
                """).ConfigureAwait(false);

            var options = new DupesOptions(
                Threshold: 0.5, MinLines: 1, MinNodes: 1,
                Format: "text", Paths: [dir]);

            var results = DupesDetector.FindDuplicates(options);

            await Assert.That(results).IsEmpty();
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task FindDuplicates_includes_score_exactly_at_threshold()
    {
        var dir = CreateTempDir();
        try
        {
            // Identical methods → Jaccard 1.0; threshold 1.0 must include (>= not >).
            await File.WriteAllTextAsync(Path.Combine(dir, "Same.cs"), """
                class Same {
                    void One() {
                        if (flag) {
                            DoWork(value);
                            return;
                        }
                        Log("miss");
                    }
                    void Two() {
                        if (flag) {
                            DoWork(value);
                            return;
                        }
                        Log("miss");
                    }
                }
                """).ConfigureAwait(false);

            var options = new DupesOptions(
                Threshold: 1.0, MinLines: 3, MinNodes: 3,
                Format: "text", Paths: [dir]);

            var results = DupesDetector.FindDuplicates(options);

            await Assert.That(results.Count).IsEqualTo(1);
            await Assert.That(results[0].Score).IsEqualTo(1.0);
            await Assert.That(results[0].LeftStartLine).IsGreaterThan(0);
            await Assert.That(results[0].LeftEndLine).IsGreaterThan(results[0].LeftStartLine);
            await Assert.That(results[0].RightStartLine).IsGreaterThan(0);
            await Assert.That(results[0].RightEndLine).IsGreaterThan(results[0].RightStartLine);
            // Line span is inclusive: end - start + 1 >= minLines (3).
            await Assert.That(results[0].LeftEndLine - results[0].LeftStartLine + 1)
                .IsGreaterThanOrEqualTo(3);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task FindDuplicates_reports_exact_line_numbers_for_known_source()
    {
        var dir = CreateTempDir();
        try
        {
            // Line 1: class, 2: method One starts, ... carefully laid out.
            var source = """
                class Exact {
                    void One() {
                        if (flag) {
                            DoWork(value);
                            return;
                        }
                        Log("miss");
                    }
                    void Two() {
                        if (flag) {
                            DoWork(value);
                            return;
                        }
                        Log("miss");
                    }
                }
                """;
            await File.WriteAllTextAsync(Path.Combine(dir, "Exact.cs"), source).ConfigureAwait(false);

            var options = new DupesOptions(
                Threshold: 0.9, MinLines: 3, MinNodes: 3,
                Format: "text", Paths: [dir]);

            var results = DupesDetector.FindDuplicates(options);
            await Assert.That(results).HasSingleItem();
            // Roslyn 1-based: "void One()" is line 2 in this file.
            await Assert.That(results[0].LeftStartLine).IsEqualTo(2);
            await Assert.That(results[0].RightStartLine).IsEqualTo(9);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task FindDuplicates_excludes_methods_below_min_nodes_even_if_long()
    {
        var dir = CreateTempDir();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "Long.cs"), """
                class Long {
                    void A() {
                        // padding
                        // padding
                        // padding
                        // padding
                        // padding
                        x = 1;
                    }
                    void B() {
                        // padding
                        // padding
                        // padding
                        // padding
                        // padding
                        x = 1;
                    }
                }
                """).ConfigureAwait(false);

            var options = new DupesOptions(
                Threshold: 0.5, MinLines: 3, MinNodes: 10_000,
                Format: "text", Paths: [dir]);

            var results = DupesDetector.FindDuplicates(options);

            await Assert.That(results).IsEmpty();
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task FindDuplicates_default_paths_scans_when_paths_empty_list_is_non_empty_cwd_safe()
    {
        // Paths.Count > 0 branch is the normal path; empty Paths uses ["."].
        // Only assert the API accepts empty Paths without throwing.
        var options = new DupesOptions(
            Threshold: 0.99, MinLines: 50, MinNodes: 50,
            Format: "text", Paths: []);

        var results = DupesDetector.FindDuplicates(options);

        await Assert.That(results).IsNotNull();
    }

    [Test]
    public async Task FindDuplicates_does_not_parse_cs_shaped_content_in_txt_files()
    {
        // Kills File.Exists && EndsWith(.cs) → || : a .txt with C# methods must stay unscanned.
        var dir = CreateTempDir();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "trap.txt"), """
                class Trap {
                    void One() {
                        if (flag) {
                            DoWork(value);
                            return;
                        }
                        Log("miss");
                    }
                    void Two() {
                        if (flag) {
                            DoWork(value);
                            return;
                        }
                        Log("miss");
                    }
                }
                """).ConfigureAwait(false);

            var options = new DupesOptions(
                Threshold: 0.5, MinLines: 3, MinNodes: 3,
                Format: "text", Paths: [Path.Combine(dir, "trap.txt")]);

            var results = DupesDetector.FindDuplicates(options);

            await Assert.That(results).IsEmpty();
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task FindDuplicates_sorts_equal_score_pairs_by_left_file_then_start_line()
    {
        var dir = CreateTempDir();
        try
        {
            // Two files, each with two identical methods → multiple score=1.0 pairs.
            await File.WriteAllTextAsync(Path.Combine(dir, "B.cs"), """
                class B {
                    void B1() {
                        if (flag) { DoWork(value); return; }
                        Log("miss");
                    }
                    void B2() {
                        if (flag) { DoWork(value); return; }
                        Log("miss");
                    }
                }
                """).ConfigureAwait(false);
            await File.WriteAllTextAsync(Path.Combine(dir, "A.cs"), """
                class A {
                    void A1() {
                        if (flag) { DoWork(value); return; }
                        Log("miss");
                    }
                    void A2() {
                        if (flag) { DoWork(value); return; }
                        Log("miss");
                    }
                }
                """).ConfigureAwait(false);

            var options = new DupesOptions(
                Threshold: 1.0, MinLines: 3, MinNodes: 3,
                Format: "text", Paths: [dir]);

            var results = DupesDetector.FindDuplicates(options);

            await Assert.That(results.Count).IsGreaterThanOrEqualTo(2);
            await Assert.That(results.All(r => r.Score == 1.0)).IsTrue();
            // First pair must start in A.cs (ordinal before B.cs) — kills score-sort bypass.
            await Assert.That(results[0].LeftFile.EndsWith("A.cs", StringComparison.Ordinal)).IsTrue();
            // LeftFile ordinal order across equal scores.
            for (var i = 1; i < results.Count; i++)
            {
                var cmp = string.CompareOrdinal(results[i - 1].LeftFile, results[i].LeftFile);
                if (cmp == 0)
                {
                    await Assert.That(results[i - 1].LeftStartLine <= results[i].LeftStartLine).IsTrue();
                }
                else
                {
                    await Assert.That(cmp < 0).IsTrue();
                }
            }
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task FindDuplicates_includes_method_when_fingerprint_count_equals_min_nodes()
    {
        var dir = CreateTempDir();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "Eq.cs"), """
                class Eq {
                    void One() {
                        if (flag) {
                            DoWork(value);
                            return;
                        }
                        Log("miss");
                    }
                    void Two() {
                        if (flag) {
                            DoWork(value);
                            return;
                        }
                        Log("miss");
                    }
                }
                """).ConfigureAwait(false);

            // Discover node count at threshold 0, then re-scan with MinNodes == that count.
            var probe = DupesDetector.FindDuplicates(new DupesOptions(
                Threshold: 0.5, MinLines: 3, MinNodes: 1,
                Format: "text", Paths: [dir]));
            await Assert.That(probe.Count).IsGreaterThanOrEqualTo(1);
            var minNodes = probe[0].LeftNodes;

            var atBoundary = DupesDetector.FindDuplicates(new DupesOptions(
                Threshold: 0.5, MinLines: 3, MinNodes: minNodes,
                Format: "text", Paths: [dir]));
            var aboveBoundary = DupesDetector.FindDuplicates(new DupesOptions(
                Threshold: 0.5, MinLines: 3, MinNodes: minNodes + 1,
                Format: "text", Paths: [dir]));

            await Assert.That(atBoundary.Count).IsGreaterThanOrEqualTo(1);
            await Assert.That(aboveBoundary).IsEmpty();
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static string MethodBody(string name) => $$"""
        class {{name}} {
            void Work() {
                if (flag) {
                    DoWork(value);
                    return;
                }
                Log("miss");
            }
        }
        """;

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"dupes_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }
}
