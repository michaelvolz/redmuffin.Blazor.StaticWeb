using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace redmuffin.Tools.QualityGates.Models;

public sealed record ArchConfig
{
    public IReadOnlyDictionary<string, IReadOnlyList<string>> AllowedDependencies { get; init; }
        = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, string> ComponentMap { get; init; }
        = new Dictionary<string, string>(StringComparer.Ordinal);
    public IReadOnlyList<string> IgnoredComponents { get; init; } = [];
    public IReadOnlyList<DependencyEdge> ForbiddenDependencies { get; init; } = [];
    public IReadOnlyList<DependencyEdge> AllowedExceptions { get; init; } = [];
    public double HealthyThreshold { get; init; } = 0.3;
    public bool FailOnCycles { get; init; } = true;
    public bool FailOnViolations { get; init; } = true;

    public static ArchConfig Parse(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            throw new FormatException("Config YAML must not be empty.");
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var dto = deserializer.Deserialize<ArchConfigDto>(yaml)
            ?? throw new FormatException("Config YAML is null or invalid.");

        return new ArchConfig
        {
            AllowedDependencies = dto.AllowedDependencies?.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<string>)NormalizeAllowedList(kvp.Value),
                StringComparer.Ordinal)
                ?? new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal),
            ComponentMap = dto.ComponentMap ?? new Dictionary<string, string>(StringComparer.Ordinal),
            IgnoredComponents = dto.IgnoredComponents ?? [],
            ForbiddenDependencies = ParseEdges(dto.ForbiddenDependencies),
            AllowedExceptions = ParseEdges(dto.AllowedExceptions),
            HealthyThreshold = dto.HealthyThreshold ?? 0.3,
            FailOnCycles = dto.FailOnCycles,
            FailOnViolations = dto.FailOnViolations,
        };
    }

    /// <summary>
    /// dependency-checker allows <c>:all</c> as a scalar allowlist. Accept
    /// YAML list form <c>[all]</c> or a single string via custom lists.
    /// </summary>
    private static List<string> NormalizeAllowedList(List<string>? value)
    {
        if (value is null || value.Count == 0)
        {
            return [];
        }

        return value
            .Select(v => string.Equals(v, "all", StringComparison.OrdinalIgnoreCase)
                || string.Equals(v, ":all", StringComparison.OrdinalIgnoreCase)
                ? "all"
                : v)
            .ToList();
    }

    private static List<DependencyEdge> ParseEdges(List<DependencyEdgeDto>? edges)
    {
        if (edges is null || edges.Count == 0)
        {
            return [];
        }

        return edges
            .Where(e => !string.IsNullOrWhiteSpace(e.From) && !string.IsNullOrWhiteSpace(e.To))
            .Select(e => new DependencyEdge(e.From!.Trim(), e.To!.Trim()))
            .ToList();
    }

    [SuppressMessage(
        "Design",
        "CA1812",
        Justification = "Instantiated by YamlDotNet reflection via Deserialize<ArchConfigDto>")]
    [Slopwatch.SlopwatchSuppress("SW002", "YamlDotNet reflection-instantiated DTO per ADR-0007")]
    internal sealed class ArchConfigDto
    {
        public Dictionary<string, List<string>>? AllowedDependencies { get; set; }
        public Dictionary<string, string>? ComponentMap { get; set; }
        public List<string>? IgnoredComponents { get; set; }
        public List<DependencyEdgeDto>? ForbiddenDependencies { get; set; }
        public List<DependencyEdgeDto>? AllowedExceptions { get; set; }
        public double? HealthyThreshold { get; set; }
        public bool FailOnCycles { get; set; } = true;
        public bool FailOnViolations { get; set; } = true;
    }

    [SuppressMessage(
        "Design",
        "CA1812",
        Justification = "Instantiated by YamlDotNet reflection via Deserialize<DependencyEdgeDto>")]
    [Slopwatch.SlopwatchSuppress("SW002", "YamlDotNet reflection-instantiated DTO per ADR-0007")]
    internal sealed class DependencyEdgeDto
    {
        public string? From { get; set; }
        public string? To { get; set; }
    }
}
