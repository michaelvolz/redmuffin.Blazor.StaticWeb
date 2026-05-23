namespace redmuffin.Tools.QualityGates.Analysis;

using System.Collections.Frozen;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class DepthDetector
{
    public static IReadOnlyList<DepthResult> Analyze(string projectPath)
    {
        if (!Directory.Exists(projectPath))
        {
            return Array.Empty<DepthResult>();
        }

        var csFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);
        var allMethods = new List<(MethodDeclarationSyntax Method, string FilePath)>();

        // Pass 1: collect all methods (no signal computation yet).
        foreach (var file in csFiles)
        {
            CollectMethods(file, allMethods);
        }

        // Pass 2: compute caller counts, then emit results.
        var results = new List<DepthResult>();
        var callerCache = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var (method, filePath) in allMethods)
        {
            var name = method.Identifier.Text;
            if (!callerCache.TryGetValue(name, out var callerCount))
            {
                callerCount = CountDistinctCallers(name, allMethods);
                callerCache[name] = callerCount;
            }

            var result = AnalyzeMethod(method, filePath, callerCount);
            if (result.CompositeScore > 0)
            {
                results.Add(result);
            }
        }

        return results
            .OrderByDescending(r => r.CompositeScore)
            .ToList()
            .AsReadOnly();
    }

    private static void CollectMethods(
        string file,
        List<(MethodDeclarationSyntax Method, string FilePath)> allMethods)
    {
        string source;
        try
        {
            source = File.ReadAllText(file);
        }
        catch (IOException)
        {
            return;
        }

        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        CompilationUnitSyntax root;
        try
        {
            root = syntaxTree.GetCompilationUnitRoot();
        }
        catch (Exception)
        {
            return;
        }

        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
        foreach (var method in methods)
        {
            allMethods.Add((method, file));
        }
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
                var invokedName = GetInvokedMethodName(invocation.Expression);

                if (invokedName != null &&
                    string.Equals(invokedName, methodName, StringComparison.Ordinal))
                {
                    callers.Add(method.Identifier.Text + ":" + filePath);
                    break;
                }
            }
        }

        return callers.Count;
    }

    public static string? GetInvokedMethodName(ExpressionSyntax expression)
    {
        return expression switch
        {
            IdentifierNameSyntax id => id.Identifier.Text,
            MemberAccessExpressionSyntax m => m.Name.Identifier.Text,
            _ => null,
        };
    }

    public static DepthResult AnalyzeMethod(MethodDeclarationSyntax method, string filePath, int callerCount = 0)
    {
        var isPrivate = method.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword));
        var paramCount = method.ParameterList.Parameters.Count;
        var loc = ComputeLinesOfCode(method);
        var hasBranching = HasBranching(method);

        var isShallow = isPrivate && loc <= 4 && !hasBranching && callerCount < 3;
        var isWrongAbstract = isPrivate && IsWrongAbstraction(method);
        var paramBloat = paramCount > 4;
        var isEntangled = isPrivate && paramCount >= 3 && HasSideEffects(method);

        (bool Active, int Weight, string Label)[] signalData =
        [
            (isShallow, 3, "shallow(3)"),
            (isWrongAbstract, 2, "wrong-abstraction(2)"),
            (paramBloat, 1, "params(1)"),
            (isEntangled, 2, "entangled(2)"),
        ];

        var active = Array.FindAll(signalData, s => s.Active);
        var composite = active.Sum(s => s.Weight);
        var signals = Array.ConvertAll(active, s => s.Label);

        var lineSpan = method.GetLocation().GetLineSpan();
        return new DepthResult(
            method.Identifier.Text, Path.GetFullPath(filePath),
            lineSpan.StartLinePosition.Line + 1,
            isShallow, paramCount, isWrongAbstract, isEntangled,
            composite, signals);
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

    public static bool IsWrongAbstraction(MethodDeclarationSyntax method)
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

        return method.Body.DescendantNodes()
                .OfType<IfStatementSyntax>()
                .SelectMany(i => i.Condition.DescendantNodesAndSelf())
                .OfType<IdentifierNameSyntax>()
                .Any(id => paramNames.Contains(id.Identifier.Text))
            || method.Body.DescendantNodes()
                .OfType<SwitchStatementSyntax>()
                .Select(s => s.Expression)
                .Any(expr => expr is IdentifierNameSyntax identifier
                    && paramNames.Contains(identifier.Identifier.Text));
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
        private static readonly FrozenDictionary<string, bool> KnownPureMethods =
            new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                { "ToString", true }, { "ToUpper", true }, { "ToLower", true },
                { "Length", true }, { "Count", true }, { "Equals", true },
                { "StartsWith", true }, { "EndsWith", true }, { "Contains", true },
                { "IndexOf", true }, { "Substring", true }, { "Trim", true },
                { "TrimStart", true }, { "TrimEnd", true }, { "Replace", true },
                { "Split", true }, { "Join", true }, { "Math", true },
                { "Abs", true }, { "Max", true }, { "Min", true },
            }.ToFrozenDictionary(StringComparer.Ordinal);

        public bool HasSideEffect { get; private set; }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            HasSideEffect = true;
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            base.VisitInvocationExpression(node);

            var simpleName = GetInvokedMethodName(node.Expression);

            if (simpleName is not null && !IsKnownPure(simpleName))
            {
                HasSideEffect = true;
            }
        }

        public static bool IsKnownPure(string name) =>
            KnownPureMethods.ContainsKey(name);
    }
}
