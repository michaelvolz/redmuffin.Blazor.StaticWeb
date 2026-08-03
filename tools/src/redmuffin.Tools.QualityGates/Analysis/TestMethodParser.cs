namespace redmuffin.Tools.QualityGates.Analysis;

using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class TestMethodParser
{
    public static IReadOnlyList<TestMethod> FindTests(string projectPath)
    {
        var results = new List<TestMethod>();

        foreach (var file in SourcePathFilter.EnumerateCsFiles(projectPath))
        {
            var source = File.ReadAllText(file);
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var root = syntaxTree.GetCompilationUnitRoot();
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                if (!HasTestAttribute(method))
                {
                    continue;
                }

                // Skip bodyless methods (abstract, interface, partial declarations)
                if (method.Body is null)
                {
                    continue;
                }

                var lineSpan = method.GetLocation().GetLineSpan();
                var className = method.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault()?.Identifier.Text ?? string.Empty;

                results.Add(new TestMethod(
                    method.Identifier.Text,
                    Path.GetFullPath(file),
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.EndLinePosition.Line + 1,
                    method.Body,
                    className));
            }
        }

        return results.AsReadOnly();
    }

    private static bool HasTestAttribute(MethodDeclarationSyntax method)
    {
        return method.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString().Contains("Test", StringComparison.Ordinal));
    }
}

// clj-mutate-manifest-begin
// {"version":1,"testedAt":"2026-08-03T12:26:05.4332416Z","moduleHash":"14c5c93357ade6d7a9d721e94a12f98217bd14de2804115e3c96bdd1c2b5cab7","forms":[{"id":"FindTests","line":9,"endLine":47,"hash":"da652dd79b1aac183bec29ddd2f8dff0b5fcab35cad52bd35d3bc91909f0e707"},{"id":"HasTestAttribute","line":49,"endLine":54,"hash":"3c3029d1a18e6d61ff0fad8b5d5dec2af6f30f02be5ffa47d09ee0a1df421c1c"}]}
// clj-mutate-manifest-end
