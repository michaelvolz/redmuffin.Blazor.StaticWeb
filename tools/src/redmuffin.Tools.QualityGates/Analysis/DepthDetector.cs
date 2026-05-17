namespace redmuffin.Tools.QualityGates.Analysis;

using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class DepthDetector
{
    public static IReadOnlyList<DepthResult> Analyze(string projectPath)
    {
        var results = new List<DepthResult>();

        if (!Directory.Exists(projectPath))
        {
            return results.AsReadOnly();
        }

        var csFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);
        var allMethods = new List<(MethodDeclarationSyntax Method, string FilePath)>();

        // Pass 1: collect all methods and flag structural quality issues.
        foreach (var file in csFiles)
        {
            string source;
            try
            {
                source = File.ReadAllText(file);
            }
            catch (IOException)
            {
                continue;
            }

            CompilationUnitSyntax root;
            try
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(source);
                root = syntaxTree.GetCompilationUnitRoot();
            }
            catch (Exception)
            {
                // Catastrophic parse failure — skip file
                continue;
            }

            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                allMethods.Add((method, file));

                var result = AnalyzeMethod(method, file);
                if (result.CompositeScore > 0)
                {
                    results.Add(result);
                }
            }
        }

        // Pass 2 (Phase 2): suppress shallow(3) for multi-caller methods.
        ApplyCallerCountFilters(results, allMethods);

        return results
            .Where(r => r.CompositeScore > 0)
            .OrderByDescending(r => r.CompositeScore)
            .ToList()
            .AsReadOnly();
    }

    private static void ApplyCallerCountFilters(
        List<DepthResult> results,
        List<(MethodDeclarationSyntax Method, string FilePath)> allMethods)
    {
        for (var i = 0; i < results.Count; i++)
        {
            var result = results[i];
            if (!result.IsShallow)
            {
                continue;
            }

            var callers = CountDistinctCallers(result.MethodName, allMethods);
            if (callers >= 3)
            {
                results[i] = RecalculateWithoutShallow(result);
            }
        }
    }

    private static DepthResult RecalculateWithoutShallow(DepthResult original)
    {
        if (!original.IsShallow)
        {
            return original;
        }

        var newSignals = original.Signals
            .Where(s => !string.Equals(s, "shallow(3)", StringComparison.Ordinal))
            .ToArray();

        var newComposite = original.CompositeScore - 3;

        return original with
        {
            IsShallow = false,
            CompositeScore = newComposite,
            Signals = newSignals.AsReadOnly(),
        };
    }

    private static int CountDistinctCallers(
        string methodName,
        List<(MethodDeclarationSyntax Method, string FilePath)> allMethods)
    {
        var callers = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (method, filePath) in allMethods)
        {
            if (method.Body is null)
            {
                continue;
            }

            var invocations = method.Body.DescendantNodes()
                .OfType<InvocationExpressionSyntax>();

            foreach (var invocation in invocations)
            {
                var invokedName = invocation.Expression switch
                {
                    IdentifierNameSyntax id => id.Identifier.Text,
                    MemberAccessExpressionSyntax m => m.Name.Identifier.Text,
                    _ => null,
                };

                if (invokedName != null &&
                    string.Equals(invokedName, methodName, StringComparison.Ordinal))
                {
                    callers.Add(method.Identifier.Text + ":" + filePath);
                    break; // one match per caller method is enough
                }
            }
        }

        return callers.Count;
    }

    private static DepthResult AnalyzeMethod(MethodDeclarationSyntax method, string filePath)
    {
        var isPrivate = method.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword));
        var loc = ComputeLinesOfCode(method);
        var hasBranching = HasBranching(method);
        var paramCount = method.ParameterList.Parameters.Count;

        var isShallow = isPrivate && loc <= 4 && !hasBranching;
        var paramBloat = paramCount > 4;
        var isWrongAbstraction = isPrivate && IsWrongAbstraction(method);
        var isEntangled = isPrivate && paramCount >= 3 && HasSideEffects(method);

        var composite = (isShallow ? 3 : 0) +
                        (isWrongAbstraction ? 2 : 0) +
                        (paramBloat ? 1 : 0) +
                        (isEntangled ? 2 : 0);

        var signals = new List<string>();
        if (isShallow) signals.Add($"shallow(3)");
        if (isWrongAbstraction) signals.Add($"wrong-abstraction(2)");
        if (paramBloat) signals.Add($"params(1)");
        if (isEntangled) signals.Add($"entangled(2)");

        var lineSpan = method.GetLocation().GetLineSpan();

        return new DepthResult(
            method.Identifier.Text,
            Path.GetFullPath(filePath),
            lineSpan.StartLinePosition.Line + 1,
            isShallow,
            paramCount,
            isWrongAbstraction,
            isEntangled,
            composite,
            signals.AsReadOnly());
    }

    private static int ComputeLinesOfCode(MethodDeclarationSyntax method)
    {
        var lineSpan = method.GetLocation().GetLineSpan();
        return lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line + 1;
    }

    private static bool HasBranching(MethodDeclarationSyntax method)
    {
        var walker = new BranchingWalker();
        walker.Visit(method);
        return walker.HasBranching;
    }

    private static bool IsWrongAbstraction(MethodDeclarationSyntax method)
    {
        if (method.Body is null)
        {
            return false;
        }

        var paramNames = method.ParameterList.Parameters
            .Select(p => p.Identifier.Text)
            .ToHashSet(StringComparer.Ordinal);

        if (paramNames.Count == 0)
        {
            return false;
        }

        var conditionals = method.Body.DescendantNodes()
            .OfType<IfStatementSyntax>()
            .SelectMany(i => i.Condition.DescendantNodesAndSelf())
            .OfType<IdentifierNameSyntax>();

        foreach (var identifier in conditionals)
        {
            if (paramNames.Contains(identifier.Identifier.Text))
            {
                return true;
            }
        }

        var switchStatements = method.Body.DescendantNodes()
            .OfType<SwitchStatementSyntax>();

        foreach (var switchStmt in switchStatements)
        {
            if (switchStmt.Expression is IdentifierNameSyntax identifier &&
                paramNames.Contains(identifier.Identifier.Text))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasSideEffects(MethodDeclarationSyntax method)
    {
        if (method.Body is null)
        {
            return false;
        }

        var walker = new SideEffectWalker();
        walker.Visit(method);
        return walker.HasSideEffect;
    }

    private sealed class BranchingWalker : CSharpSyntaxWalker
    {
        public bool HasBranching { get; private set; }

        public override void VisitIfStatement(IfStatementSyntax node) => HasBranching = true;

        public override void VisitSwitchStatement(SwitchStatementSyntax node) => HasBranching = true;

        public override void VisitForStatement(ForStatementSyntax node) => HasBranching = true;

        public override void VisitForEachStatement(ForEachStatementSyntax node) => HasBranching = true;

        public override void VisitWhileStatement(WhileStatementSyntax node) => HasBranching = true;

        public override void VisitDoStatement(DoStatementSyntax node) => HasBranching = true;

        public override void VisitTryStatement(TryStatementSyntax node) => HasBranching = true;
    }

    private sealed class SideEffectWalker : CSharpSyntaxWalker
    {
        public bool HasSideEffect { get; private set; }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            HasSideEffect = true;
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var simpleName = node.Expression switch
            {
                MemberAccessExpressionSyntax m => m.Name.Identifier.Text,
                IdentifierNameSyntax i => i.Identifier.Text,
                _ => null,
            };

            if (simpleName is not null && !IsKnownPure(simpleName))
            {
                HasSideEffect = true;
            }
        }

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (node.Expression is not ThisExpressionSyntax)
            {
                HasSideEffect = true;
            }
        }

        private static bool IsKnownPure(string name)
        {
            return name is "ToString" or "ToUpper" or "ToLower" or "Length" or "Count" or "Equals"
                or "StartsWith" or "EndsWith" or "Contains" or "IndexOf" or "Substring"
                or "Trim" or "TrimStart" or "TrimEnd" or "Replace" or "Split" or "Join"
                or "Math" or "Abs" or "Max" or "Min";
        }
    }
}
