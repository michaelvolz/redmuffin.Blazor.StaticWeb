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
