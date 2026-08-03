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

        candidates.Sort((a, b) =>
        {
            var c = b.Score.CompareTo(a.Score);
            if (c != 0) return c;
            c = string.CompareOrdinal(a.LeftFile, b.LeftFile);
            return c != 0 ? c : a.LeftStartLine.CompareTo(b.LeftStartLine);
        });
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

    private static List<DupesEntry> ScanFiles(IReadOnlyList<string> paths, int minLines, int minNodes)
    {
        var entries = new List<DupesEntry>();

        foreach (var path in paths)
        {
            var fullPath = Path.GetFullPath(path);
            if (Directory.Exists(fullPath))
            {
                foreach (var file in SourcePathFilter.EnumerateCsFiles(fullPath))
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

        if (!DupesNormalizer.TryComputeMethodFingerprints(method, out var fingerprints))
            return;

        if (fingerprints.Count >= minNodes)
        {
            entries.Add(new DupesEntry(
                File: filePath, StartLine: startLine, EndLine: endLine,
                Nodes: fingerprints.Count, Fingerprints: fingerprints));
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

// clj-mutate-manifest-begin
// {"version":1,"testedAt":"2026-08-03T13:14:22.1887729Z","moduleHash":"e3ffbf5561200c1d4d94d8783bc9c6d852a08557bfb564ab8094756aa6eeb0bc","forms":[{"id":"FindDuplicates","line":17,"endLine":39,"hash":"d828cdad6bae69767fd83e519cfbea86ba4f29cb96cf0b34d76b54f13c4254c7"},{"id":"TryAddCandidate","line":41,"endLine":52,"hash":"df3c6722f43c535bb5b121819dc67e1cb962d15496b82884c65be6b610641188"},{"id":"ScanFiles","line":54,"endLine":75,"hash":"fd95c3c526065b8128d29028991ee0b9e7a96dc49a40da59ee5d6eb4ff292687"},{"id":"TryAddEntries","line":77,"endLine":96,"hash":"6d283b72cb94c9b32c333ba2ae113a37afe43d01608c827d515b63c03096a50c"},{"id":"TryAddMethodEntry","line":98,"endLine":118,"hash":"9795e3c71060567c573015e9ec6cd6eb9f1ae691f1665296aeea30420953c5e1"},{"id":"JaccardSimilarity","line":120,"endLine":125,"hash":"ca49a7c465b05e26c530fa646cb187af553268ce3684333deba8d2b7d91cfacb"}]}
// clj-mutate-manifest-end
