namespace redmuffin.Tools.QualityGates.Analysis;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using redmuffin.Tools.QualityGates.Commands;

/// <summary>
///     Detects structural duplicate code candidates across C# source files
///     using the dry4clj algorithm: normalize, fingerprint, Jaccard similarity.
/// </summary>
public static class DupesDetector
{
    /// <summary>
    ///     Scans C# source files for structural duplicate candidates.
    /// </summary>
    public static IReadOnlyList<DupesCandidate> FindDuplicates(DupesOptions options)
    {
        var paths = options.Paths.Count > 0 ? options.Paths : ["."];
        var entries = ScanFiles(paths, options.MinLines, options.MinNodes);

        var candidates = new List<DupesCandidate>();
        for (var i = 0; i < entries.Count; i++)
        {
            for (var j = i + 1; j < entries.Count; j++)
            {
                var left = entries[i];
                var right = entries[j];

                var score = JaccardSimilarity(left.Fingerprints, right.Fingerprints);
                if (score >= options.Threshold)
                {
                    candidates.Add(new DupesCandidate(
                        Score: Math.Round(score, 4),
                        LeftFile: left.File,
                        LeftStartLine: left.StartLine,
                        LeftEndLine: left.EndLine,
                        RightFile: right.File,
                        RightStartLine: right.StartLine,
                        RightEndLine: right.EndLine,
                        LeftNodes: left.Nodes,
                        RightNodes: right.Nodes));
                }
            }
        }

        candidates.Sort((a, b) =>
        {
            var scoreCmp = b.Score.CompareTo(a.Score);
            if (scoreCmp != 0) return scoreCmp;
            var fileCmp = string.Compare(a.LeftFile, b.LeftFile, StringComparison.Ordinal);
            if (fileCmp != 0) return fileCmp;
            var lineCmp = a.LeftStartLine.CompareTo(b.LeftStartLine);
            return lineCmp;
        });

        return candidates;
    }

    private static List<DupesEntry> ScanFiles(IReadOnlyList<string> paths, int minLines, int minNodes)
    {
        var entries = new List<DupesEntry>();

        foreach (var path in paths)
        {
            var fullPath = Path.GetFullPath(path);
            if (Directory.Exists(fullPath))
            {
                foreach (var file in Directory.EnumerateFiles(fullPath, "*.cs", SearchOption.AllDirectories))
                {
                    ScanFile(file, entries, minLines, minNodes);
                }
            }
            else if (File.Exists(fullPath) && fullPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                ScanFile(fullPath, entries, minLines, minNodes);
            }
        }

        return entries;
    }

    private static void ScanFile(string filePath, List<DupesEntry> entries, int minLines, int minNodes)
    {
        SyntaxTree tree;
        try
        {
            tree = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath), path: filePath);
        }
        catch
        {
            return;
        }

        var root = tree.GetRoot();
        var methods = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            if (method.Body == null) continue;

            var startLine = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            var endLine = method.GetLocation().GetLineSpan().EndLinePosition.Line + 1;
            var lineCount = endLine - startLine + 1;

            if (lineCount < minLines) continue;

            try
            {
                var normalized = DupesNormalizer.Normalize(method);
                var fingerprints = DupesNormalizer.ComputeFingerprints(normalized);

                if (fingerprints.Count < minNodes) continue;

                entries.Add(new DupesEntry(
                    File: filePath,
                    StartLine: startLine,
                    EndLine: endLine,
                    Nodes: fingerprints.Count,
                    Fingerprints: fingerprints));
            }
            catch
            {
                // Skip methods that fail normalization
            }
        }
    }

    private static double JaccardSimilarity(ISet<string> a, ISet<string> b)
    {
        var intersection = Enumerable.Intersect(a, b, StringComparer.Ordinal).Count();
        var union = Enumerable.Union(a, b, StringComparer.Ordinal).Count();
        return union == 0 ? 0.0 : (double)intersection / union;
    }

    private sealed record DupesEntry(
        string File,
        int StartLine,
        int EndLine,
        int Nodes,
        ISet<string> Fingerprints);
}
