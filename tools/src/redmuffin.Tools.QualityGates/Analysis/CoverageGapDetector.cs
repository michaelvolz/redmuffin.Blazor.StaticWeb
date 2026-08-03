namespace redmuffin.Tools.QualityGates.Analysis;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class CoverageGapDetector
{
    public static IReadOnlyList<MethodCrap> ClassifyCoverageGaps(
        IReadOnlyList<MethodCrap> methods, string projectPath)
    {
        return methods
            .GroupBy(m => m.FilePath, StringComparer.Ordinal)
            .SelectMany(g => ClassifyGroup(g.Key, [.. g], projectPath))
            .ToList();
    }

    private static IEnumerable<MethodCrap> ClassifyGroup(
        string filePath, IReadOnlyList<MethodCrap> methods, string projectPath)
    {
        var resolved = ResolvePath(filePath, projectPath);
        if (!File.Exists(resolved))
        {
            foreach (var m in methods)
                yield return m;
            yield break;
        }

        var sourceCode = File.ReadAllText(resolved);

        foreach (var m in methods)
        {
            yield return ClassifyOneWithSource(m, sourceCode);
        }
    }

    private static MethodCrap ClassifyOneWithSource(MethodCrap m, string sourceCode)
    {
        return TryClassifyAsConductor(m, sourceCode)
            ?? TryClassifyAsSwitchDispatcher(m, sourceCode)
            ?? m;
    }

    private static MethodCrap? TryClassifyAsConductor(MethodCrap m, string sourceCode)
    {
        if (m.Complexity > 4) return null;
        if (m.Complexity <= 3 && m.Coverage >= 0.01) return null;

        var result = m with { IsCoverageGap = IsCoverageGap(sourceCode, m.MethodName, m.Complexity) };
        return result.IsCoverageGap ? result : null;
    }

    private static MethodCrap? TryClassifyAsSwitchDispatcher(MethodCrap m, string sourceCode)
    {
        if (m.Complexity <= 3 || m.Coverage <= 0.5 || m.CrapScore <= 8) return null;

        var result = m with { IsCoverageGap = IsSwitchDispatcher(sourceCode, m.MethodName) };
        return result.IsCoverageGap ? result : null;
    }

    private static string ResolvePath(string filePath, string projectPath) =>
        filePath.StartsWith('/') ? filePath : Path.Combine(projectPath, filePath);

    public static bool IsCoverageGap(string sourceCode, string methodName, int cyclomaticComplexity)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = tree.GetCompilationUnitRoot();
        var method = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => string.Equals(m.Identifier.Text, methodName, StringComparison.Ordinal));

        if (method?.Body is null) return false;

        return ContainsOnlyDelegationAndGuards(method.Body);
    }

    public static bool IsSwitchDispatcher(string sourceCode, string methodName)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = tree.GetCompilationUnitRoot();
        var method = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => string.Equals(m.Identifier.Text, methodName, StringComparison.Ordinal));

        if (method?.Body is null || method.Body.Statements.Count != 1) return false;

        return method.Body.Statements[0] switch
        {
            ReturnStatementSyntax { Expression: SwitchExpressionSyntax switchExpr }
                => AllArmsAreSingleDelegation(switchExpr),
            _ => false,
        };
    }

    private static bool AllArmsAreSingleDelegation(SwitchExpressionSyntax switchExpr)
    {
        foreach (var arm in switchExpr.Arms)
        {
            if (arm.WhenClause is not null) return false;
            if (!IsSimpleArmExpression(arm.Expression)) return false;
        }

        return true;
    }

    private static bool IsSimpleArmExpression(ExpressionSyntax expr) =>
        expr is InvocationExpressionSyntax
            or CollectionExpressionSyntax
            or ImplicitArrayCreationExpressionSyntax;

    private static bool ContainsOnlyDelegationAndGuards(BlockSyntax body)
    {
        foreach (var statement in body.Statements)
        {
            if (IsLoopOrComplex(statement)) return false;
        }

        return true;
    }

    private static bool IsLoopOrComplex(StatementSyntax statement)
    {
        if (IsLoopStatement(statement)) return true;
        if (IsComplexConditional(statement)) return true;
        if (statement is BlockSyntax block) return !ContainsOnlyDelegationAndGuards(block);
        if (statement is TryStatementSyntax tryStmt)
            return !ContainsOnlyDelegationAndGuards(tryStmt.Block)
                || tryStmt.Catches.Any(c => !ContainsOnlyDelegationAndGuards(c.Block));
        return false;
    }

    private static bool IsLoopStatement(StatementSyntax s) =>
        s is ForStatementSyntax or ForEachStatementSyntax or WhileStatementSyntax or DoStatementSyntax;

    private static bool IsComplexConditional(StatementSyntax s) =>
        s is IfStatementSyntax { Else: not null } or SwitchStatementSyntax;
}

// clj-mutate-manifest-begin
// {"version":1,"testedAt":"2026-08-03T13:02:25.1882208Z","moduleHash":"c30d8741f8e6c02fda09bff74b47fa76f824488bfb28831e39078aba0d119cad","forms":[{"id":"ClassifyCoverageGaps","line":8,"endLine":15,"hash":"5466a8ff9e3174f39b0c1251f4167de7f0d64e309de0e3315baf6935fca028e6"},{"id":"ClassifyGroup","line":17,"endLine":34,"hash":"3321efd95fcc731a6bfc1e416fb92c58a666268df7c415758912f52e4235017b"},{"id":"ClassifyOneWithSource","line":36,"endLine":41,"hash":"18dc2b5f604be733fc537508d2618556b42298a15340b9bef2047a7079046e0e"},{"id":"TryClassifyAsConductor","line":43,"endLine":50,"hash":"9a79b37e394993d92b7b8fa8825d7c72cd189074c413c1334c1f61b513803018"},{"id":"TryClassifyAsSwitchDispatcher","line":52,"endLine":58,"hash":"62a9da7e0df6f4a9931844d3fbbbdc039c6da1bd49f70664f9a52c5a2d7c2546"},{"id":"ResolvePath","line":60,"endLine":61,"hash":"50dc9e43d23a18ea633cc9610482d35adb34c85ec585ae350225ea442b6cfe8a"},{"id":"IsCoverageGap","line":63,"endLine":74,"hash":"b02252c3f71eb7e2f5eb4ff695734c048702ba5d84357c99e5842e2823ac4db0"},{"id":"IsSwitchDispatcher","line":76,"endLine":92,"hash":"f7534aafad88eda2f0458ef81cb05b62054abffb447d79e8e306eed224fb2d54"},{"id":"AllArmsAreSingleDelegation","line":94,"endLine":103,"hash":"55b39de999dc1a1fcbde0b474bec2e7374bcd4e89bea8dd48a13b500d04b0f9b"},{"id":"IsSimpleArmExpression","line":105,"endLine":108,"hash":"41ffeec2563200a86f5a179f04f7f1decc06b91fa0c2f9f85780b7bf62f5f853"},{"id":"ContainsOnlyDelegationAndGuards","line":110,"endLine":118,"hash":"aef80784a0cd49328a33346684764d4f74dc933fdcb30ae5869433bc0ba4661e"},{"id":"IsLoopOrComplex","line":120,"endLine":129,"hash":"5c6a25d0d0dc7a4df031ce8d4f21cc5e54e8a921656d16c0f1b711fd9b52289a"},{"id":"IsLoopStatement","line":131,"endLine":132,"hash":"284e8a2514317ecc4cf054b22ea12ea062a4d1cf6a5a8faa4ae2d7416c660552"},{"id":"IsComplexConditional","line":134,"endLine":135,"hash":"7df810d6d5997519434086fed133ed16856a04ed8cc474acd0948125a9fac0cc"}]}
// clj-mutate-manifest-end
