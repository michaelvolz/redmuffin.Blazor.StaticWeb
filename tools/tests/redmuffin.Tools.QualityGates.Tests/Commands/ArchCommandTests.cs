namespace redmuffin.Tools.QualityGates.Tests.Commands;

using System.Runtime.CompilerServices;
using redmuffin.Tools.QualityGates.Analysis;
using redmuffin.Tools.QualityGates.Commands;
using redmuffin.Tools.QualityGates.Models;

public sealed class ArchCommandTests
{
    [Test]
    public async Task should_parse_valid_yaml_with_all_fields()
    {
        var yaml = """
            allowed-dependencies:
              Web:
                - Core
              Api:
                - Core
              Core: []
            component-map:
              redmuffin.Blazor.StaticWeb: Web
              redmuffin.Blazor.StaticWeb.Api: Api
            ignored-components:
              - Tests
              - Tools
            fail-on-cycles: false
            fail-on-violations: false
            """;

        var config = ArchConfig.Parse(yaml);

        await Assert.That(config.AllowedDependencies.Count).IsEqualTo(3);
        await Assert.That(config.AllowedDependencies["Web"]).IsEquivalentTo(["Core"]);
        await Assert.That(config.AllowedDependencies["Api"]).IsEquivalentTo(["Core"]);
        await Assert.That(config.AllowedDependencies["Core"]).IsEmpty();
        await Assert.That(config.ComponentMap.Count).IsEqualTo(2);
        await Assert.That(config.ComponentMap["redmuffin.Blazor.StaticWeb"]).IsEqualTo("Web");
        await Assert.That(config.ComponentMap["redmuffin.Blazor.StaticWeb.Api"]).IsEqualTo("Api");
        await Assert.That(config.IgnoredComponents).IsEquivalentTo(["Tests", "Tools"]);
        await Assert.That(config.FailOnCycles).IsFalse();
        await Assert.That(config.FailOnViolations).IsFalse();
    }

    [Test]
    public async Task should_use_defaults_for_optional_fields()
    {
        var yaml = """
            allowed-dependencies:
              Core: []
            """;

        var config = ArchConfig.Parse(yaml);

        await Assert.That(config.AllowedDependencies["Core"]).IsEmpty();
        await Assert.That(config.ComponentMap).IsEmpty();
        await Assert.That(config.IgnoredComponents).IsEmpty();
        await Assert.That(config.ForbiddenDependencies).IsEmpty();
        await Assert.That(config.AllowedExceptions).IsEmpty();
        await Assert.That(config.HealthyThreshold).IsEqualTo(0.3);
        await Assert.That(config.FailOnCycles).IsTrue();
        await Assert.That(config.FailOnViolations).IsTrue();
    }

    [Test]
    public async Task should_parse_zones_forbidden_and_exceptions()
    {
        var yaml = """
            allowed-dependencies:
              Web:
                - all
              Core: []
            forbidden-dependencies:
              - from: Web
                to: Infra
            allowed-exceptions:
              - from: Core
                to: Web
            healthy-threshold: 0.25
            component-map:
              MyApp.Web: Web
              MyApp.Core: Core
            """;

        var config = ArchConfig.Parse(yaml);

        await Assert.That(config.AllowedDependencies["Web"]).IsEquivalentTo(["all"]);
        await Assert.That(config.ForbiddenDependencies).IsEquivalentTo(
            [new DependencyEdge("Web", "Infra")]);
        await Assert.That(config.AllowedExceptions).IsEquivalentTo(
            [new DependencyEdge("Core", "Web")]);
        await Assert.That(config.HealthyThreshold).IsEqualTo(0.25);
    }

    [Test]
    public async Task should_allow_all_targets_when_allowlist_is_all()
    {
        var config = ArchConfig.Parse("""
            allowed-dependencies:
              Tests:
                - all
            """);
        var cg = new ComponentGraph(
            new Dictionary<string, ISet<string>> { ["Tests"] = new HashSet<string> { "Web", "Core" } },
            new HashSet<string>());

        var violations = ArchAnalyzer.FindViolations(cg, config);

        await Assert.That(violations).IsEmpty();
    }

    [Test]
    public async Task should_forbid_explicit_edge_even_when_all_allowed()
    {
        var config = ArchConfig.Parse("""
            allowed-dependencies:
              Web:
                - all
            forbidden-dependencies:
              - from: Web
                to: Infra
            """);
        var cg = new ComponentGraph(
            new Dictionary<string, ISet<string>> { ["Web"] = new HashSet<string> { "Infra" } },
            new HashSet<string>());

        var violations = ArchAnalyzer.FindViolations(cg, config);

        await Assert.That(violations.Count).IsEqualTo(1);
        await Assert.That(violations[0].SourceComponent).IsEqualTo("Web");
        await Assert.That(violations[0].TargetComponent).IsEqualTo("Infra");
    }

    [Test]
    public async Task should_suppress_violation_with_allowed_exception()
    {
        var config = ArchConfig.Parse("""
            allowed-dependencies:
              Core: []
            allowed-exceptions:
              - from: Core
                to: Web
            """);
        var cg = new ComponentGraph(
            new Dictionary<string, ISet<string>> { ["Core"] = new HashSet<string> { "Web" } },
            new HashSet<string>());

        var violations = ArchAnalyzer.FindViolations(cg, config);

        await Assert.That(violations).IsEmpty();
    }

    [Test]
    public async Task should_classify_zone_pain_when_below_main_sequence_band()
    {
        // A+I = 0.2 with threshold 0.3 → below 0.7 → pain
        await Assert.That(ArchAnalyzer.ClassifyZone(0.2, 0.3)).IsEqualTo(ArchZone.Pain);
    }

    [Test]
    public async Task should_classify_zone_useless_when_above_main_sequence_band()
    {
        await Assert.That(ArchAnalyzer.ClassifyZone(1.5, 0.3)).IsEqualTo(ArchZone.Useless);
    }

    [Test]
    public async Task should_classify_zone_healthy_near_main_sequence()
    {
        await Assert.That(ArchAnalyzer.ClassifyZone(1.0, 0.3)).IsEqualTo(ArchZone.Healthy);
    }

    [Test]
    public async Task should_compute_instability_from_fan_in_out()
    {
        var config = ArchConfig.Parse("""
            allowed-dependencies:
              Web:
                - Core
              Core: []
            component-map:
              MyApp.Web: Web
              MyApp.Core: Core
            """);
        var cg = new ComponentGraph(
            new Dictionary<string, ISet<string>> { ["Web"] = new HashSet<string> { "Core" } },
            new HashSet<string>());

        var metrics = ArchAnalyzer.ComputeMetrics(cg, config, projectPath: Path.GetTempPath());
        var web = metrics.First(m => m.Component == "Web");
        var core = metrics.First(m => m.Component == "Core");

        await Assert.That(web.FanOut).IsEqualTo(1);
        await Assert.That(web.FanIn).IsEqualTo(0);
        await Assert.That(web.Instability).IsEqualTo(1.0);
        await Assert.That(core.FanIn).IsEqualTo(1);
        await Assert.That(core.FanOut).IsEqualTo(0);
        await Assert.That(core.Instability).IsEqualTo(0.0);
    }

    [Test]
    public async Task should_compute_abstractness_from_interfaces_and_classes()
    {
        using var temp = new TempDir();
        // Per-project subdirs so each component's types are scanned only once.
        WriteMappedProject(temp.Path, "MyApp.Web", """
            public interface IService { }
            public class Service : IService { }
            """);
        WriteMappedProject(temp.Path, "MyApp.Core", """
            public class Entity { }
            """);

        var config = ArchConfig.Parse("""
            allowed-dependencies:
              Web:
                - Core
              Core: []
            component-map:
              MyApp.Web: Web
              MyApp.Core: Core
            """);
        var cg = new ComponentGraph(
            new Dictionary<string, ISet<string>> { ["Web"] = new HashSet<string> { "Core" } },
            new HashSet<string>());

        var metrics = ArchAnalyzer.ComputeMetrics(cg, config, temp.Path);
        var web = metrics.First(m => m.Component == "Web");
        var core = metrics.First(m => m.Component == "Core");

        // Web: interface + class → A = 0.5; Core: concrete only → A = 0
        await Assert.That(web.Abstractness).IsEqualTo(0.5);
        await Assert.That(core.Abstractness).IsEqualTo(0.0);
        await Assert.That(web.Distance).IsEqualTo(Math.Abs(0.5 + 1.0 - 1.0));
        await Assert.That(core.Zone).IsEqualTo(ArchZone.Pain);
    }

    [Test]
    public async Task should_parse_yaml_with_component_map()
    {
        var yaml = """
            allowed-dependencies:
              Frontend: []
              Backend: []
            component-map:
              MyApp.Web: Frontend
              MyApp.Api: Backend
            """;

        var config = ArchConfig.Parse(yaml);

        await Assert.That(config.ComponentMap.Count).IsEqualTo(2);
        await Assert.That(config.ComponentMap["MyApp.Web"]).IsEqualTo("Frontend");
        await Assert.That(config.ComponentMap["MyApp.Api"]).IsEqualTo("Backend");
    }

    [Test]
    public async Task should_throw_on_empty_yaml()
    {
        await Assert.ThrowsAsync<FormatException>(() =>
        {
            ArchConfig.Parse(string.Empty);
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task should_throw_on_invalid_yaml_syntax()
    {
        var yaml = "not: valid: yaml: :: :";

        await Assert.ThrowsAsync(() =>
        {
            ArchConfig.Parse(yaml);
            return Task.CompletedTask;
        });
    }

    // -- U2: ProjectGraph tests --

    [Test]
    public async Task should_extract_single_project_reference()
    {
        using var temp = new TempDir();
        temp.WriteProject("MyApp", ["MyLib"]);
        temp.WriteProject("MyLib", []);

        var graph = ProjectGraph.From(temp.Path);

        await Assert.That(graph.Dependencies.Count).IsEqualTo(2);
        await Assert.That(graph.Dependencies["MyApp"]).IsEquivalentTo(["MyLib"]);
        await Assert.That(graph.Dependencies["MyLib"]).IsEmpty();
    }

    [Test]
    public async Task should_return_empty_graph_for_empty_directory()
    {
        using var temp = new TempDir();

        var graph = ProjectGraph.From(temp.Path);

        await Assert.That(graph.Dependencies).IsEmpty();
    }

    [Test]
    public async Task should_extract_multiple_project_references()
    {
        using var temp = new TempDir();
        temp.WriteProject("Api", ["Core", "Shared"]);
        temp.WriteProject("Core", []);
        temp.WriteProject("Shared", []);

        var graph = ProjectGraph.From(temp.Path);

        await Assert.That(graph.Dependencies["Api"]).IsEquivalentTo(["Core", "Shared"]);
    }

    [Test]
    public async Task should_resolve_solution_style_references()
    {
        using var temp = new TempDir();
        temp.WriteProject("Api", ["../Core/Core.csproj"]);
        temp.WriteProject("Core", []);

        var graph = ProjectGraph.From(temp.Path);

        await Assert.That(graph.Dependencies["Api"]).IsEquivalentTo(["Core"]);
    }

    [Test]
    public async Task should_handle_project_with_no_references()
    {
        using var temp = new TempDir();
        temp.WriteProject("Standalone", []);

        var graph = ProjectGraph.From(temp.Path);

        await Assert.That(graph.Dependencies["Standalone"]).IsEmpty();
    }

    // -- U3: ComponentGraph tests --

    [Test]
    public async Task should_map_projects_to_components()
    {
        var config = ConfigWithMap();
        var graph = new ProjectGraph(new Dictionary<string, IReadOnlyList<string>>
        {
            ["MyApp.Web"] = ["MyApp.Core"],
            ["MyApp.Core"] = [],
        });

        var componentGraph = ComponentGraph.From(graph, config);

        await Assert.That(componentGraph.Dependencies.Count).IsEqualTo(2);
        await Assert.That(componentGraph.Dependencies["Web"]).IsEquivalentTo(["Core"]);
        await Assert.That(componentGraph.UnmappedProjects).IsEmpty();
    }

    [Test]
    public async Task should_assign_unmapped_projects_to_default()
    {
        var config = ConfigWithoutMap();
        var graph = new ProjectGraph(new Dictionary<string, IReadOnlyList<string>>
        {
            ["SomeLib"] = [],
        });

        var componentGraph = ComponentGraph.From(graph, config);

        await Assert.That(componentGraph.UnmappedProjects).IsEquivalentTo(["SomeLib"]);
        await Assert.That(componentGraph.Dependencies).IsEmpty();
    }

    [Test]
    public async Task should_skip_ignored_components()
    {
        var config = ArchConfig.Parse("""
            allowed-dependencies:
              Web: []
            ignored-components:
              - Tests
            component-map:
              MyTests: Tests
              MyApp.Web: Web
            """);
        var graph = new ProjectGraph(new Dictionary<string, IReadOnlyList<string>>
        {
            ["MyTests"] = ["MyApp.Web"],
        });

        var componentGraph = ComponentGraph.From(graph, config);

        await Assert.That(componentGraph.Dependencies).IsEmpty();
    }

    // -- U4: Violation detection tests --

    [Test]
    public async Task should_find_no_violations_for_allowed_dependency()
    {
        var config = ConfigWithMap();
        var cg = new ComponentGraph(
            new Dictionary<string, ISet<string>> { ["Web"] = new HashSet<string> { "Core" } },
            new HashSet<string>());

        var violations = ArchAnalyzer.FindViolations(cg, config);

        await Assert.That(violations).IsEmpty();
    }

    [Test]
    public async Task should_find_violation_for_disallowed_dependency()
    {
        var config = ConfigWithMap();
        var cg = new ComponentGraph(
            new Dictionary<string, ISet<string>> { ["Core"] = new HashSet<string> { "Web" } },
            new HashSet<string>());

        var violations = ArchAnalyzer.FindViolations(cg, config);

        await Assert.That(violations.Count).IsEqualTo(1);
        await Assert.That(violations[0].SourceComponent).IsEqualTo("Core");
        await Assert.That(violations[0].TargetComponent).IsEqualTo("Web");
    }

    [Test]
    public async Task should_report_unmapped_projects_as_violations()
    {
        var config = ConfigWithoutMap();
        var cg = new ComponentGraph(
            new Dictionary<string, ISet<string>> { ["Default"] = new HashSet<string>() },
            new HashSet<string> { "SomeLib" });

        var violations = ArchAnalyzer.FindViolations(cg, config);

        await Assert.That(violations.Count).IsEqualTo(1);
        await Assert.That(violations[0].Reason).Contains("not assigned to any component");
    }

    [Test]
    public async Task should_allow_same_component_references()
    {
        var config = ConfigWithMap();
        var cg = new ComponentGraph(
            new Dictionary<string, ISet<string>> { ["Web"] = new HashSet<string>() },
            new HashSet<string>());

        var violations = ArchAnalyzer.FindViolations(cg, config);

        await Assert.That(violations).IsEmpty();
    }

    // -- U5: Cycle detection tests --

    [Test]
    public async Task should_find_no_cycles_in_acyclic_graph()
    {
        var cg = new ComponentGraph(
            new Dictionary<string, ISet<string>> { ["A"] = new HashSet<string> { "B" }, ["B"] = new HashSet<string> { "C" } },
            new HashSet<string>());

        var cycles = ArchAnalyzer.FindCycles(cg);

        await Assert.That(cycles).IsEmpty();
    }

    [Test]
    public async Task should_detect_simple_two_component_cycle()
    {
        var cg = new ComponentGraph(
            new Dictionary<string, ISet<string>> { ["A"] = new HashSet<string> { "B" }, ["B"] = new HashSet<string> { "A" } },
            new HashSet<string>());

        var cycles = ArchAnalyzer.FindCycles(cg);

        await Assert.That(cycles.Count).IsEqualTo(1);
    }

    [Test]
    public async Task should_detect_three_component_cycle()
    {
        var cg = new ComponentGraph(
            new Dictionary<string, ISet<string>> { ["A"] = new HashSet<string> { "B" }, ["B"] = new HashSet<string> { "C" }, ["C"] = new HashSet<string> { "A" } },
            new HashSet<string>());

        var cycles = ArchAnalyzer.FindCycles(cg);

        await Assert.That(cycles.Count).IsEqualTo(1);
    }

    [Test]
    public async Task should_detect_multiple_independent_cycles()
    {
        var cg = new ComponentGraph(
            new Dictionary<string, ISet<string>> { ["A"] = new HashSet<string> { "B" }, ["B"] = new HashSet<string> { "A" }, ["C"] = new HashSet<string> { "D" }, ["D"] = new HashSet<string> { "C" } },
            new HashSet<string>());

        var cycles = ArchAnalyzer.FindCycles(cg);

        await Assert.That(cycles.Count).IsEqualTo(2);
    }

    // -- U6: Exit code and orchestration tests --

    [Test]
    public async Task should_return_zero_for_clean_graph()
    {
        var config = ConfigWithMap();

        var exitCode = ArchAnalyzer.DecideExitCode([], [], config);

        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task should_return_two_for_violations()
    {
        var config = ConfigWithMap();
        var violations = new List<ArchViolation>
        {
            new("p1", "p2", "Core", "Web", "bad"),
        };

        var exitCode = ArchAnalyzer.DecideExitCode(violations, [], config);

        await Assert.That(exitCode).IsEqualTo(2);
    }

    [Test]
    public async Task should_return_zero_for_violations_when_fail_off()
    {
        var config = ArchConfig.Parse("""
            allowed-dependencies:
              Core: []
            fail-on-violations: false
            """);
        var violations = new List<ArchViolation>
        {
            new("p1", "p2", "Core", "Web", "bad"),
        };

        var exitCode = ArchAnalyzer.DecideExitCode(violations, [], config);

        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task should_return_two_for_cycles()
    {
        var config = ConfigWithMap();
        var cycles = new List<ArchCycle>
        {
            new(["A", "B"], 2),
        };

        var exitCode = ArchAnalyzer.DecideExitCode([], cycles, config);

        await Assert.That(exitCode).IsEqualTo(2);
    }

    [Test]
    public async Task should_return_zero_for_cycles_when_fail_off()
    {
        var config = ArchConfig.Parse("""
            allowed-dependencies:
              Core: []
            fail-on-cycles: false
            """);
        var cycles = new List<ArchCycle>
        {
            new(["A", "B"], 2),
        };

        var exitCode = ArchAnalyzer.DecideExitCode([], cycles, config);

        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task should_return_two_when_both_violations_and_cycles()
    {
        var config = ConfigWithMap();
        var violations = new List<ArchViolation>
        {
            new("p1", "p2", "C", "W", "bad"),
        };
        var cycles = new List<ArchCycle>
        {
            new(["A", "B"], 2),
        };

        var exitCode = ArchAnalyzer.DecideExitCode(violations, cycles, config);

        await Assert.That(exitCode).IsEqualTo(2);
    }

    [Test]
    public async Task should_return_one_when_config_file_not_found()
    {
        using var temp = new TempDir();
        temp.WriteProject("Test", []);

        var (exitCode, _) = ArchHandler.Run(
            "/nonexistent/config.yml",
            temp.Path);

        await Assert.That(exitCode).IsEqualTo(1);
    }

    // -- U7: Output formatting tests --

    [Test]
    public async Task should_format_clean_result_with_zero_counts()
    {
        var result = new ArchResult(0, [], [], 5, 3);
        var output = ArchOutputFormatter.Format(result, json: false);

        await Assert.That(output).Contains("5 projects scanned");
        await Assert.That(output).Contains("3 components defined");
        await Assert.That(output).Contains("0 violations found");
        await Assert.That(output).Contains("0 cycles found");
        await Assert.That(output).DoesNotContain("Violations:");
    }

    [Test]
    public async Task should_format_result_with_violations()
    {
        var violations = new List<ArchViolation>
        {
            new("Core", "Web", "Core", "Web",
                "Core is not allowed to depend on Web."),
        };
        var result = new ArchResult(2, violations, [], 5, 3);
        var output = ArchOutputFormatter.Format(result, json: false);

        await Assert.That(output).Contains("1 violations found");
        await Assert.That(output).Contains("Violations:");
        await Assert.That(output).Contains("Core → Web");
        await Assert.That(output).Contains("Core is not allowed to depend on Web.");
    }

    [Test]
    public async Task should_format_json_output()
    {
        var violations = new List<ArchViolation>
        {
            new("Core", "Web", "Core", "Web", "bad"),
        };
        var result = new ArchResult(2, violations, [], 5, 3);
        var output = ArchOutputFormatter.Format(result, json: true);

        await Assert.That(output).Contains("\"exitCode\": 2");
        await Assert.That(output).Contains("\"sourceComponent\": \"Core\"");
    }

    [Test]
    public async Task should_format_json_for_clean_result()
    {
        var result = new ArchResult(0, [], [], 5, 3);
        var output = ArchOutputFormatter.Format(result, json: true);

        await Assert.That(output).Contains("\"exitCode\": 0");
        await Assert.That(output).Contains("\"violations\": []");
        await Assert.That(output).Contains("\"cycles\": []");
    }

    [Test]
    public async Task should_format_component_metrics_with_zone()
    {
        var result = new ArchResult(0, [], [], 2, 1)
        {
            Metrics =
            [
                new ComponentMetric("Web", FanIn: 0, FanOut: 1, Instability: 1.0,
                    Abstractness: 0.5, Distance: 0.5, Zone: ArchZone.Healthy),
            ],
        };

        var output = ArchOutputFormatter.Format(result, json: false);

        await Assert.That(output).Contains("Component metrics");
        await Assert.That(output).Contains("Web:");
        await Assert.That(output).Contains("A=0.50");
        await Assert.That(output).Contains("I=1.00");
        await Assert.That(output).Contains("D=0.50");
        await Assert.That(output).Contains("zone=Healthy");
    }

    private static void WriteMappedProject(string root, string projectName, string csharpSource)
    {
        var dir = Path.Combine(root, projectName);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, $"{projectName}.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
            </Project>
            """);
        File.WriteAllText(Path.Combine(dir, "Types.cs"), csharpSource);
    }

    private static ArchConfig ConfigWithMap()
    {
        return ArchConfig.Parse("""
            allowed-dependencies:
              Web:
                - Core
              Core: []
            component-map:
              MyApp.Web: Web
              MyApp.Core: Core
            """);
    }

    private static ArchConfig ConfigWithoutMap()
    {
        return ArchConfig.Parse("""
            allowed-dependencies:
              Default: []
            """);
    }

    internal sealed class TempDir : IDisposable
    {
        public string Path { get; }

        public TempDir([CallerMemberName] string? name = null)
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"agt_{name}_{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public void WriteProject(string name, string[] references)
        {
            var refs = string.Join('\n',
                references.Select(r => r.EndsWith(".csproj")
                    ? $"""    <ProjectReference Include="{r}" />"""
                    : $"""    <ProjectReference Include="{r}.csproj" />"""));

            var xml = $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <ItemGroup>
                {refs}
                  </ItemGroup>
                </Project>
                """;

            File.WriteAllText(System.IO.Path.Combine(Path, $"{name}.csproj"), xml);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, true);
        }
    }
}
