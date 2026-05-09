namespace redmuffin.Tools.QualityGates.Tests.Commands;

using System.Runtime.CompilerServices;
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
        await Assert.That(config.FailOnCycles).IsTrue();
        await Assert.That(config.FailOnViolations).IsTrue();
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
        var graph = new ProjectGraph(new Dictionary<string, List<string>>
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
        var graph = new ProjectGraph(new Dictionary<string, List<string>>
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
        var graph = new ProjectGraph(new Dictionary<string, List<string>>
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
            new() { ["Web"] = ["Core"] },
            []);

        var violations = ArchHandler.FindViolations(cg, config);

        await Assert.That(violations).IsEmpty();
    }

    [Test]
    public async Task should_find_violation_for_disallowed_dependency()
    {
        var config = ConfigWithMap();
        var cg = new ComponentGraph(
            new() { ["Core"] = ["Web"] },
            []);

        var violations = ArchHandler.FindViolations(cg, config);

        await Assert.That(violations.Count).IsEqualTo(1);
        await Assert.That(violations[0].SourceComponent).IsEqualTo("Core");
        await Assert.That(violations[0].TargetComponent).IsEqualTo("Web");
    }

    [Test]
    public async Task should_report_unmapped_projects_as_violations()
    {
        var config = ConfigWithoutMap();
        var cg = new ComponentGraph(
            new() { ["Default"] = [] },
            ["SomeLib"]);

        var violations = ArchHandler.FindViolations(cg, config);

        await Assert.That(violations.Count).IsEqualTo(1);
        await Assert.That(violations[0].Reason).Contains("not assigned to any component");
    }

    [Test]
    public async Task should_allow_same_component_references()
    {
        var config = ConfigWithMap();
        var cg = new ComponentGraph(
            new() { ["Web"] = [] },
            []);

        var violations = ArchHandler.FindViolations(cg, config);

        await Assert.That(violations).IsEmpty();
    }

    // -- U5: Cycle detection tests --

    [Test]
    public async Task should_find_no_cycles_in_acyclic_graph()
    {
        var cg = new ComponentGraph(
            new() { ["A"] = ["B"], ["B"] = ["C"] },
            []);

        var cycles = ArchHandler.FindCycles(cg);

        await Assert.That(cycles).IsEmpty();
    }

    [Test]
    public async Task should_detect_simple_two_component_cycle()
    {
        var cg = new ComponentGraph(
            new() { ["A"] = ["B"], ["B"] = ["A"] },
            []);

        var cycles = ArchHandler.FindCycles(cg);

        await Assert.That(cycles.Count).IsEqualTo(1);
    }

    [Test]
    public async Task should_detect_three_component_cycle()
    {
        var cg = new ComponentGraph(
            new() { ["A"] = ["B"], ["B"] = ["C"], ["C"] = ["A"] },
            []);

        var cycles = ArchHandler.FindCycles(cg);

        await Assert.That(cycles.Count).IsEqualTo(1);
    }

    [Test]
    public async Task should_detect_multiple_independent_cycles()
    {
        var cg = new ComponentGraph(
            new() { ["A"] = ["B"], ["B"] = ["A"], ["C"] = ["D"], ["D"] = ["C"] },
            []);

        var cycles = ArchHandler.FindCycles(cg);

        await Assert.That(cycles.Count).IsEqualTo(2);
    }

    // -- U6: Exit code and orchestration tests --

    [Test]
    public async Task should_return_zero_for_clean_graph()
    {
        var config = ConfigWithMap();

        var exitCode = ArchHandler.DecideExitCode([], [], config);

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

        var exitCode = ArchHandler.DecideExitCode(violations, [], config);

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

        var exitCode = ArchHandler.DecideExitCode(violations, [], config);

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

        var exitCode = ArchHandler.DecideExitCode([], cycles, config);

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

        var exitCode = ArchHandler.DecideExitCode([], cycles, config);

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

        var exitCode = ArchHandler.DecideExitCode(violations, cycles, config);

        await Assert.That(exitCode).IsEqualTo(2);
    }

    [Test]
    public async Task should_return_one_when_config_file_not_found()
    {
        using var temp = new TempDir();
        temp.WriteProject("Test", []);

        var exitCode = ArchHandler.Run(
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
            try { Directory.Delete(Path, true); }
            catch { /* best effort */ }
        }
    }
}
