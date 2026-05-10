namespace redmuffin.Tools.QualityGates.Analysis;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class MutationManifest
{
    private const string BeginMarker = "// clj-mutate-manifest-begin";
    private const string EndMarker = "// clj-mutate-manifest-end";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static Manifest? Extract(string source)
    {
        var beginIdx = source.LastIndexOf(BeginMarker, StringComparison.Ordinal);
        if (beginIdx < 0)
        {
            return null;
        }

        var jsonStart = source.IndexOf('\n', beginIdx) + 1;
        var endIdx = source.LastIndexOf(EndMarker, StringComparison.Ordinal);
        if (endIdx < 0)
        {
            return null;
        }

        // JSON is on the line between begin and end markers, prefixed with "// "
        var jsonLine = source[jsonStart..endIdx].Trim();
        if (jsonLine.StartsWith("// ", StringComparison.Ordinal))
        {
            jsonLine = jsonLine[3..];
        }

        return JsonSerializer.Deserialize<JsonManifest>(jsonLine, JsonOptions)?.ToManifest();
    }

    public static string Strip(string source)
    {
        var beginIdx = source.LastIndexOf(BeginMarker, StringComparison.Ordinal);
        if (beginIdx < 0)
        {
            return source;
        }

        // Remove trailing newline before the marker if present
        if (beginIdx > 0 && source[beginIdx - 1] == '\n')
        {
            beginIdx--;
        }

        return source[..beginIdx];
    }

    public static Manifest Build(string source, DateTime testedAt)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();

        // Collect methods from all top-level types (classes, structs, interfaces)
        var topLevelTypes = root.DescendantNodes().OfType<TypeDeclarationSyntax>()
            .Where(t => t.Parent == root || t.Parent is BaseNamespaceDeclarationSyntax)
            .ToList();

        var members = new List<MemberDeclarationSyntax>();
        foreach (var type in topLevelTypes)
        {
            members.AddRange(type.Members.OfType<MethodDeclarationSyntax>());
        }

        var forms = new List<FormEntry>();
        var moduleText = new StringBuilder();

        foreach (var member in members)
        {
            var normalized = member.NormalizeWhitespace().ToFullString();
            var hash = ComputeHash(normalized);
            var lineSpan = member.GetLocation().GetLineSpan();

            forms.Add(new FormEntry(
                GetMemberId(member),
                lineSpan.StartLinePosition.Line,
                lineSpan.EndLinePosition.Line,
                hash));

            moduleText.Append(normalized);
        }

        var moduleHash = ComputeHash(moduleText.ToString());

        return new Manifest(1, testedAt, moduleHash, forms.AsReadOnly());
    }

    public static string Embed(string source, Manifest manifest)
    {
        var json = JsonSerializer.Serialize(
            new JsonManifest(manifest.Version, manifest.TestedAt, manifest.ModuleHash, manifest.Forms),
            JsonOptions);

        return source + "\n" + BeginMarker + "\n// " + json + "\n" + EndMarker + "\n";
    }

    public static IReadOnlySet<int> ChangedFormIndices(Manifest prior, Manifest current)
    {
        var changed = new HashSet<int>();

        for (var i = 0; i < Math.Min(prior.Forms.Count, current.Forms.Count); i++)
        {
            if (!string.Equals(prior.Forms[i].Hash, current.Forms[i].Hash, StringComparison.Ordinal))
            {
                changed.Add(i);
            }
        }

        // Forms that exist in current but not in prior are considered changed
        for (var i = prior.Forms.Count; i < current.Forms.Count; i++)
        {
            changed.Add(i);
        }

        return changed;
    }

    private static string GetMemberId(MemberDeclarationSyntax member) => member switch
    {
        MethodDeclarationSyntax m => m.Identifier.Text,
        ClassDeclarationSyntax c => c.Identifier.Text,
        StructDeclarationSyntax s => s.Identifier.Text,
        InterfaceDeclarationSyntax i => i.Identifier.Text,
        PropertyDeclarationSyntax p => p.Identifier.Text,
        FieldDeclarationSyntax f => f.Declaration.Variables.FirstOrDefault()?.Identifier.Text ?? "field",
        _ => "member",
    };

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    private sealed record JsonManifest(
        int Version,
        DateTime TestedAt,
        string ModuleHash,
        IReadOnlyList<FormEntry> Forms)
    {
        public Manifest ToManifest() => new(Version, TestedAt, ModuleHash, Forms);
    }
}
