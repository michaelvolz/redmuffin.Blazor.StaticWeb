using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace redmuffin.Tools.QualityGates.Models;

public sealed record ArchConfig
{
    public Dictionary<string, List<string>> AllowedDependencies { get; init; } = new();
    public Dictionary<string, string> ComponentMap { get; init; } = new();
    public List<string> IgnoredComponents { get; init; } = [];
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
            AllowedDependencies = dto.AllowedDependencies ?? new(),
            ComponentMap = dto.ComponentMap ?? new(),
            IgnoredComponents = dto.IgnoredComponents ?? [],
            FailOnCycles = dto.FailOnCycles,
            FailOnViolations = dto.FailOnViolations,
        };
    }

    internal sealed class ArchConfigDto
    {
        public Dictionary<string, List<string>>? AllowedDependencies { get; set; }
        public Dictionary<string, string>? ComponentMap { get; set; }
        public List<string>? IgnoredComponents { get; set; }
        public bool FailOnCycles { get; set; } = true;
        public bool FailOnViolations { get; set; } = true;
    }
}
