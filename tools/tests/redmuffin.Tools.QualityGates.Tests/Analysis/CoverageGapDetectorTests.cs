namespace redmuffin.Tools.QualityGates.Tests.Analysis;

using redmuffin.Tools.QualityGates.Analysis;

public sealed class CoverageGapDetectorTests
{
    [Test]
    public async Task should_return_true_when_method_is_pure_delegation()
    {
        var source = """
            public static class C
            {
                public static int DoTheThing(string input)
                {
                    return SomeHandler.Process(input);
                }
            }
            """;
        var result = CoverageGapDetector.IsCoverageGap(source, "DoTheThing", cyclomaticComplexity: 2);
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task should_return_true_when_method_has_guard_clause_and_delegation()
    {
        var source = """
            public static class C
            {
                public static int Execute(string path)
                {
                    if (!File.Exists(path)) return 1;
                    return DoTheThing(path);
                }
            }
            """;
        var result = CoverageGapDetector.IsCoverageGap(source, "Execute", cyclomaticComplexity: 3);
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task should_return_false_when_method_contains_loop()
    {
        var source = """
            public static class C
            {
                public static void Process(IReadOnlyList<int> items)
                {
                    foreach (var x in items) { DoThing(x); }
                }
            }
            """;
        var result = CoverageGapDetector.IsCoverageGap(source, "Process", cyclomaticComplexity: 2);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task should_return_false_when_cc_exceeds_three()
    {
        var source = """
            public static class C
            {
                public static int DoTheThing(string input)
                {
                    return SomeHandler.Process(input);
                }
            }
            """;
        var result = CoverageGapDetector.IsCoverageGap(source, "DoTheThing", cyclomaticComplexity: 4);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task should_return_true_when_method_is_try_catch_wrapper()
    {
        var source = """
            public static class C
            {
                public static int Parse(string input)
                {
                    try { return int.Parse(input); }
                    catch { return 0; }
                }
            }
            """;
        var result = CoverageGapDetector.IsCoverageGap(source, "Parse", cyclomaticComplexity: 3);
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task should_return_false_when_guard_has_else_branch()
    {
        var source = """
            public static class C
            {
                public static int Decide(bool x)
                {
                    if (x) return 1; else return 2;
                }
            }
            """;
        var result = CoverageGapDetector.IsCoverageGap(source, "Decide", cyclomaticComplexity: 3);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task ClassifyCoverageGaps_marks_known_gap_patterns()
    {
        var methods = new List<MethodCrap>
        {
            new("RunAnalysis", "CrapCommand.cs", 125, Complexity: 3, Coverage: 0, CrapScore: 12),
            new("Parse", "CoverageParser.cs", 8, Complexity: 13, Coverage: 71, CrapScore: 16.9),
        };
        var dir = Path.Combine(Path.GetTempPath(), "gap-test-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        try
        {
            var source = """
                public static class CrapCommand
                {
                    public static int RunAnalysis(string p, string c, int m, bool ch)
                    {
                        try
                        {
                            var methods = CyclomaticComplexity.Analyze(p);
                            var coverage = CoverageParser.Parse(c);
                            var results = MethodMapper.Map(methods, coverage);
                            return CrapHandler.Run(results, m);
                        }
                        catch { return 1; }
                    }
                }
                """;
            await File.WriteAllTextAsync(Path.Combine(dir, "CrapCommand.cs"), source).ConfigureAwait(false);
            var classified = CoverageGapDetector.ClassifyCoverageGaps(methods, dir);
            await Assert.That(classified[0].IsCoverageGap).IsTrue();
            await Assert.That(classified[1].IsCoverageGap).IsFalse();
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Test]
    public async Task should_detect_gap_in_real_RunAnalysis_method()
    {
        var srcDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "src", "redmuffin.Tools.QualityGates", "Commands"));
        var filePath = Path.Combine(srcDir, "CrapCommand.cs");
        if (!File.Exists(filePath)) return;
        var source = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        var result = CoverageGapDetector.IsCoverageGap(source, "RunAnalysis", cyclomaticComplexity: 3);
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task should_detect_switch_dispatcher_with_all_delegation_arms()
    {
        var source = """
            public static class C
            {
                private static string Dispatch(int x)
                {
                    return x switch
                    {
                        1 => HandleOne(x),
                        2 => HandleTwo(x),
                        _ => HandleDefault(x),
                    };
                }
            }
            """;
        var result = CoverageGapDetector.IsSwitchDispatcher(source, "Dispatch");
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task should_reject_switch_with_inline_literals()
    {
        var source = """
            public static class C
            {
                private static string Dispatch(int x)
                {
                    return x switch
                    {
                        1 => "one",
                        2 => HandleTwo(x),
                        _ => "unknown",
                    };
                }
            }
            """;
        var result = CoverageGapDetector.IsSwitchDispatcher(source, "Dispatch");
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task should_reject_switch_with_when_clause()
    {
        var source = """
            public static class C
            {
                private static string Dispatch(int x)
                {
                    return x switch
                    {
                        > 0 => HandlePositive(x),
                        _ => HandleDefault(x),
                    };
                }
            }
            """;
        var result = CoverageGapDetector.IsSwitchDispatcher(source, "Dispatch");
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task ClassifyCoverageGaps_marks_switch_dispatchers()
    {
        var methods = new List<MethodCrap>
        {
            new("Dispatch", "Dispatcher.cs", 1, Complexity: 11, Coverage: 0.80, CrapScore: 12.9),
        };
        var dir = Path.Combine(Path.GetTempPath(), "sd-test-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        try
        {
            var source = """
                public static class C
                {
                    private static string Dispatch(int x)
                    {
                        return x switch
                        {
                            1 => HandleOne(x),
                            2 => HandleTwo(x),
                            _ => HandleDefault(x),
                        };
                    }
                }
                """;
            await File.WriteAllTextAsync(Path.Combine(dir, "Dispatcher.cs"), source).ConfigureAwait(false);
            var classified = CoverageGapDetector.ClassifyCoverageGaps(methods, dir);
            await Assert.That(classified[0].IsCoverageGap).IsTrue();
        }
        finally { Directory.Delete(dir, recursive: true); }
    }
}
