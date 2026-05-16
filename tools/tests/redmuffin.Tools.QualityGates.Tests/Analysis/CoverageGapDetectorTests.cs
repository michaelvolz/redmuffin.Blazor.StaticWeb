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
    public async Task should_return_false_when_body_contains_loop()
    {
        var source = """
            public static class C
            {
                public static int DoTheThing(string input)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        SomeHandler.Process(input);
                    }
                    return 0;
                }
            }
            """;
        var result = CoverageGapDetector.IsCoverageGap(source, "DoTheThing", cyclomaticComplexity: 2);
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
                        _ when x > 0 => HandlePositive(x),
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

    // ── IsCoverageGap: string.Equals boundary ──
    [Test]
    public async Task IsCoverageGap_should_return_false_when_method_name_not_found()
    {
        var source = """
            public static class C
            {
                public static int RealMethod(string input) => 1;
            }
            """;
        var result = CoverageGapDetector.IsCoverageGap(source, "NonExistent", cyclomaticComplexity: 2);
        await Assert.That(result).IsFalse();
    }

    // ── IsSwitchDispatcher: body/statements boundaries ──
    [Test]
    public async Task IsSwitchDispatcher_should_return_false_when_body_is_null()
    {
        var source = """
            public abstract class C
            {
                public abstract int Dispatch(int x);
            }
            """;
        var result = CoverageGapDetector.IsSwitchDispatcher(source, "Dispatch");
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsSwitchDispatcher_should_return_false_when_zero_statements()
    {
        var source = """
            public static class C
            {
                public static int Dispatch(int x) { }
            }
            """;
        var result = CoverageGapDetector.IsSwitchDispatcher(source, "Dispatch");
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsSwitchDispatcher_should_return_false_when_two_statements()
    {
        var source = """
            public static class C
            {
                public static int Dispatch(int x)
                {
                    var y = x + 1;
                    return y switch
                    {
                        1 => HandleOne(x),
                        _ => HandleDefault(x),
                    };
                }
            }
            """;
        var result = CoverageGapDetector.IsSwitchDispatcher(source, "Dispatch");
        await Assert.That(result).IsFalse();
    }

    // ── ClassifyCoverageGaps: conductor boundaries (private TryClassifyAsConductor) ──
    [Test]
    public async Task ClassifyCoverageGaps_should_mark_cc4_delegation_as_gap()
    {
        var methods = new List<MethodCrap>
        {
            new("DoTheThing", "Conductor.cs", 1, Complexity: 4, Coverage: 0.0, CrapScore: 12.0),
        };
        var dir = Path.Combine(Path.GetTempPath(), "cg-bnd-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        try
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
            await File.WriteAllTextAsync(Path.Combine(dir, "Conductor.cs"), source).ConfigureAwait(false);
            var classified = CoverageGapDetector.ClassifyCoverageGaps(methods, dir);
            await Assert.That(classified[0].IsCoverageGap).IsTrue();
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Test]
    public async Task ClassifyCoverageGaps_should_not_mark_covered_cc3_as_gap()
    {
        var methods = new List<MethodCrap>
        {
            new("DoTheThing", "Conductor.cs", 1, Complexity: 3, Coverage: 0.01, CrapScore: 12.0),
        };
        var dir = Path.Combine(Path.GetTempPath(), "cg-bnd2-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        try
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
            await File.WriteAllTextAsync(Path.Combine(dir, "Conductor.cs"), source).ConfigureAwait(false);
            var classified = CoverageGapDetector.ClassifyCoverageGaps(methods, dir);
            await Assert.That(classified[0].IsCoverageGap).IsFalse();
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Test]
    public async Task ClassifyCoverageGaps_should_not_mark_missing_file_as_gap()
    {
        var methods = new List<MethodCrap>
        {
            new("AnyMethod", "NonExistent.cs", 1, Complexity: 2, Coverage: 0.0, CrapScore: 4.0),
        };
        var dir = Path.Combine(Path.GetTempPath(), "cg-nofile-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        try
        {
            var classified = CoverageGapDetector.ClassifyCoverageGaps(methods, dir);
            await Assert.That(classified[0].IsCoverageGap).IsFalse();
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Test]
    public async Task ClassifyCoverageGaps_should_not_mark_cc5_as_conductor()
    {
        var methods = new List<MethodCrap>
        {
            new("DoTheThing", "Conductor.cs", 1, Complexity: 5, Coverage: 0.0, CrapScore: 25.0),
        };
        var dir = Path.Combine(Path.GetTempPath(), "cg-cc5-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        try
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
            await File.WriteAllTextAsync(Path.Combine(dir, "Conductor.cs"), source).ConfigureAwait(false);
            var classified = CoverageGapDetector.ClassifyCoverageGaps(methods, dir);
            await Assert.That(classified[0].IsCoverageGap).IsFalse();
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    // ── ClassifyCoverageGaps: switch-dispatcher boundaries (private TryClassifyAsSwitchDispatcher) ──
    [Test]
    public async Task ClassifyCoverageGaps_should_not_mark_high_cc_low_crap_as_switch_dispatcher()
    {
        var methods = new List<MethodCrap>
        {
            new("Dispatch", "Dispatcher.cs", 1, Complexity: 5, Coverage: 0.80, CrapScore: 7.0),
        };
        var dir = Path.Combine(Path.GetTempPath(), "sd-bnd-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        try
        {
            var source = """
                public static class C
                {
                    public static string Dispatch(int x)
                    {
                        return x switch
                        {
                            1 => HandleOne(x),
                            _ => HandleDefault(x),
                        };
                    }
                }
                """;
            await File.WriteAllTextAsync(Path.Combine(dir, "Dispatcher.cs"), source).ConfigureAwait(false);
            var classified = CoverageGapDetector.ClassifyCoverageGaps(methods, dir);
            await Assert.That(classified[0].IsCoverageGap).IsFalse();
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Test]
    public async Task ClassifyCoverageGaps_should_not_mark_high_cc_low_coverage_as_switch_dispatcher()
    {
        var methods = new List<MethodCrap>
        {
            new("Dispatch", "Dispatcher.cs", 1, Complexity: 5, Coverage: 0.5, CrapScore: 20.0),
        };
        var dir = Path.Combine(Path.GetTempPath(), "sd-cov-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        try
        {
            var source = """
                public static class C
                {
                    public static string Dispatch(int x)
                    {
                        return x switch
                        {
                            1 => HandleOne(x),
                            _ => HandleDefault(x),
                        };
                    }
                }
                """;
            await File.WriteAllTextAsync(Path.Combine(dir, "Dispatcher.cs"), source).ConfigureAwait(false);
            var classified = CoverageGapDetector.ClassifyCoverageGaps(methods, dir);
            await Assert.That(classified[0].IsCoverageGap).IsFalse();
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Test]
    public async Task ClassifyCoverageGaps_should_not_mark_high_cc_low_cc3_as_switch_dispatcher()
    {
        // Complexity=3 is below the CC>4 conductor threshold, so the conductor
        // classifier runs too. Use a body with inline logic so conductor doesn't catch.
        var methods = new List<MethodCrap>
        {
            new("Dispatch", "Dispatcher.cs", 1, Complexity: 3, Coverage: 0.80, CrapScore: 20.0),
        };
        var dir = Path.Combine(Path.GetTempPath(), "sd-cc3-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        try
        {
            var source = """
                public static class C
                {
                    public static string Dispatch(int x)
                    {
                        var y = x + 1;
                        return y switch
                        {
                            1 => HandleOne(x),
                            _ => HandleDefault(x),
                        };
                    }
                }
                """;
            await File.WriteAllTextAsync(Path.Combine(dir, "Dispatcher.cs"), source).ConfigureAwait(false);
            var classified = CoverageGapDetector.ClassifyCoverageGaps(methods, dir);
            await Assert.That(classified[0].IsCoverageGap).IsFalse();
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Test]
    public async Task ClassifyCoverageGaps_should_not_mark_switch_dispatcher_with_missing_file()
    {
        var methods = new List<MethodCrap>
        {
            new("Dispatch", "MissingDispatcher.cs", 1, Complexity: 5, Coverage: 0.80, CrapScore: 15.0),
        };
        var dir = Path.Combine(Path.GetTempPath(), "sd-nofile-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        try
        {
            var classified = CoverageGapDetector.ClassifyCoverageGaps(methods, dir);
            await Assert.That(classified[0].IsCoverageGap).IsFalse();
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    // ── IsSwitchDispatcher: non-matching switch arms (kills #24, #25) ──
    [Test]
    public async Task IsSwitchDispatcher_should_return_false_when_default_arm_matches()
    {
        var source = """
            public static class C
            {
                public static string Dispatch(int x)
                {
                    return x switch
                    {
                        99 => "unknown",
                    };
                }
            }
            """;
        var result = CoverageGapDetector.IsSwitchDispatcher(source, "Dispatch");
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsSwitchDispatcher_should_return_true_for_valid_dispatcher_one_statement()
    {
        var source = """
            public static class C
            {
                public static string Dispatch(int x)
                {
                    return x switch
                    {
                        1 => HandleOne(x),
                        _ => HandleDefault(x),
                    };
                }
            }
            """;
        var result = CoverageGapDetector.IsSwitchDispatcher(source, "Dispatch");
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsSwitchDispatcher_should_return_false_when_statement_is_not_return_switch()
    {
        var source = """
            public static class C
            {
                public static string Dispatch(int x)
                {
                    return x.ToString();
                }
            }
            """;
        var result = CoverageGapDetector.IsSwitchDispatcher(source, "Dispatch");
        await Assert.That(result).IsFalse();
    }

    // ── Boundary: CC=3 switch-dispatcher guard (kills #12) ──
    [Test]
    public async Task ClassifyCoverageGaps_should_not_mark_cc3_switch_dispatcher()
    {
        var methods = new List<MethodCrap>
        {
            new("Dispatch", "Dispatcher.cs", 1, Complexity: 3, Coverage: 0.80, CrapScore: 15.0),
        };
        var dir = Path.Combine(Path.GetTempPath(), "sd-bnd3-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        try
        {
            var source = """
                public static class C
                {
                    public static string Dispatch(int x)
                    {
                        return x switch
                        {
                            1 => HandleOne(x),
                            _ => HandleDefault(x),
                        };
                    }
                }
                """;
            await File.WriteAllTextAsync(Path.Combine(dir, "Dispatcher.cs"), source).ConfigureAwait(false);
            var classified = CoverageGapDetector.ClassifyCoverageGaps(methods, dir);
            await Assert.That(classified[0].IsCoverageGap).IsFalse();
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    // ── Boundary: CRAP=8.0 switch-dispatcher guard (kills #14) ──
    [Test]
    public async Task ClassifyCoverageGaps_should_not_mark_crap8_switch_dispatcher()
    {
        var methods = new List<MethodCrap>
        {
            new("Dispatch", "Dispatcher.cs", 1, Complexity: 5, Coverage: 0.80, CrapScore: 8.0),
        };
        var dir = Path.Combine(Path.GetTempPath(), "sd-bnd4-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        try
        {
            var source = """
                public static class C
                {
                    public static string Dispatch(int x)
                    {
                        return x switch
                        {
                            1 => HandleOne(x),
                            _ => HandleDefault(x),
                        };
                    }
                }
                """;
            await File.WriteAllTextAsync(Path.Combine(dir, "Dispatcher.cs"), source).ConfigureAwait(false);
            var classified = CoverageGapDetector.ClassifyCoverageGaps(methods, dir);
            await Assert.That(classified[0].IsCoverageGap).IsFalse();
        }
        finally { Directory.Delete(dir, recursive: true); }
    }
}
