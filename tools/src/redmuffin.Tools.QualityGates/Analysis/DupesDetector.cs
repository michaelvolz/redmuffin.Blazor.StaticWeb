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
    /// <returns></returns>
    public static IReadOnlyList<DupesCandidate> FindDuplicates(DupesOptions options)
    {
        var paths = options.Paths.Count > 0 ? options.Paths : ["."];
        var entries = ScanFiles(paths, options.MinLines, options.MinNodes);

        var candidates = new List<DupesCandidate>();
        for (var i = 0; i < entries.Count; i++)
        {
            for (var j = i + 1; j < entries.Count; j++)
            {
                TryAddCandidate(entries[i], entries[j], options.Threshold, candidates);
            }
        }

        candidates.Sort(CandidateComparer);
        return candidates;
    }

    private static void TryAddCandidate(DupesEntry left, DupesEntry right, double threshold, List<DupesCandidate> candidates)
    {
        var score = JaccardSimilarity(left.Fingerprints, right.Fingerprints);
        if (score >= threshold)
        {
            candidates.Add(new DupesCandidate(
                Score: Math.Round(score, 4),
                LeftFile: left.File, LeftStartLine: left.StartLine, LeftEndLine: left.EndLine,
                RightFile: right.File, RightStartLine: right.StartLine, RightEndLine: right.EndLine,
                LeftNodes: left.Nodes, RightNodes: right.Nodes));
        }
    }

    public static int CandidateComparer(DupesCandidate a, DupesCandidate b)
    {
        var scoreCmp = b.Score.CompareTo(a.Score);
        if (scoreCmp != 0) return scoreCmp;
        var fileCmp = string.CompareOrdinal(a.LeftFile, b.LeftFile);
        return fileCmp != 0 ? fileCmp : a.LeftStartLine.CompareTo(b.LeftStartLine);
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
                    TryAddEntries(file, entries, minLines, minNodes);
                }
            }
            else if (File.Exists(fullPath) && fullPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                TryAddEntries(fullPath, entries, minLines, minNodes);
            }
        }

        return entries;
    }

    private static bool IsCsFile(string path) =>
        File.Exists(path) && path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);

    private static void TryAddEntries(string filePath, List<DupesEntry> entries, int minLines, int minNodes)
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
            TryAddMethodEntry(filePath, entries, method, minLines, minNodes);
        }
    }

    private static void TryAddMethodEntry(
        string filePath, List<DupesEntry> entries,
        Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax method,
        int minLines, int minNodes)
    {
        if (method.Body == null) return;

        var startLine = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        var endLine = method.GetLocation().GetLineSpan().EndLinePosition.Line + 1;
        if (endLine - startLine + 1 < minLines) return;

        try
        {
            var normalized = DupesNormalizer.Normalize(method);
            var fingerprints = DupesNormalizer.ComputeFingerprints(normalized);

            if (fingerprints.Count >= minNodes)
            {
                entries.Add(new DupesEntry(
                    File: filePath, StartLine: startLine, EndLine: endLine,
                    Nodes: fingerprints.Count, Fingerprints: fingerprints));
            }
        }
        catch
        {
            // Skip methods that fail normalization
        }
    }

    private static bool MethodQualifies(
        Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax method,
        int minLines,
        int minNodes)
    {
        if (method.Body == null) return false;

        var startLine = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        var endLine = method.GetLocation().GetLineSpan().EndLinePosition.Line + 1;
        var lineCount = endLine - startLine + 1;

        return lineCount >= minLines;
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
