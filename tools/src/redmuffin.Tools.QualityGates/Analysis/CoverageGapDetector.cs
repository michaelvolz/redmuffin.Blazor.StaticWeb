namespace redmuffin.Tools.QualityGates.Analysis;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class CoverageGapDetector
{
    public static IReadOnlyList<MethodCrap> ClassifyCoverageGaps(
        IReadOnlyList<MethodCrap> methods, string projectPath)
    {
        return methods.Select(m => ClassifyOne(m, projectPath)).ToList();
    }

    private static MethodCrap ClassifyOne(MethodCrap m, string projectPath)
    {
        if (TryClassifyAsConductor(m, projectPath, out var result)) return result;
        if (TryClassifyAsSwitchDispatcher(m, projectPath, out result)) return result;
        return m;
    }

    private static bool TryClassifyAsConductor(MethodCrap m, string projectPath, out MethodCrap result)
    {
        result = m;
        if (m.Complexity > 4) return false;
        if (m.Complexity <= 3 && m.Coverage >= 0.01) return false;

        var filePath = ResolvePath(m.FilePath, projectPath);
        if (!File.Exists(filePath)) return false;

        var sourceCode = File.ReadAllText(filePath);
        result = m with { IsCoverageGap = IsCoverageGap(sourceCode, m.MethodName, m.Complexity) };
        return result.IsCoverageGap;
    }

    private static bool TryClassifyAsSwitchDispatcher(MethodCrap m, string projectPath, out MethodCrap result)
    {
        result = m;
        if (m.Complexity <= 3 || m.Coverage <= 0.5 || m.CrapScore <= 8) return false;

        var filePath = ResolvePath(m.FilePath, projectPath);
        if (!File.Exists(filePath)) return false;

        var sourceCode = File.ReadAllText(filePath);
        result = m with { IsCoverageGap = IsSwitchDispatcher(sourceCode, m.MethodName) };
        return result.IsCoverageGap;
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
