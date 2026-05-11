namespace redmuffin.Tools.QualityGates.Analysis;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
///     Normalizes C# syntax trees into structural fingerprint trees,
///     following the dry4clj algorithm: replace identifiers with :symbol,
///     literals with type tags, and preserve structural shape.
/// </summary>
public static class DupesNormalizer
{
    /// <summary>
    ///     Normalizes a syntax node into a structural tree.
    /// </summary>
    /// <returns></returns>
    public static IReadOnlyList<object> Normalize(SyntaxNode root)
    {
        return NormalizeNode(root);
    }

    /// <summary>
    ///     Computes a set of structural fingerprints by walking the
    ///     normalized tree and serializing every sub-form to a string.
    /// </summary>
    /// <returns></returns>
    public static ISet<string> ComputeFingerprints(IReadOnlyList<object> normalized)
    {
        var fingerprints = new HashSet<string>(StringComparer.Ordinal);
        CollectFingerprints(normalized, fingerprints);
        return fingerprints;
    }

    private static List<object> NormalizeNode(SyntaxNode node)
    {
        return node switch
        {
            ExpressionSyntax e => NormalizeExpression(e),
            StatementSyntax s => NormalizeStatement(s),
            MethodDeclarationSyntax m => NormalizeMethod(m),
            ClassDeclarationSyntax c => NormalizeMemberContainer("class", c.Members),
            CompilationUnitSyntax u => NormalizeMemberContainer("unit", u.Members),
            NamespaceDeclarationSyntax ns => NormalizeMemberContainer("namespace", ns.Members),
            FileScopedNamespaceDeclarationSyntax fns => NormalizeMemberContainer("namespace", fns.Members),
            VariableDeclarationSyntax => ["declare"],
            ArgumentSyntax a => NormalizeNode(a.Expression),
            _ => WalkChildren(node),
        };
    }

    private static List<object> NormalizeExpression(ExpressionSyntax node)
    {
        return node switch
        {
            IdentifierNameSyntax => ["symbol"],
            LiteralExpressionSyntax literal => [LiteralTag(literal)],
            _ => NormalizeComplexExpression(node),
        };
    }

    private static List<object> NormalizeComplexExpression(ExpressionSyntax node) => node switch
    {
        BinaryExpressionSyntax binary => ["binary", NormalizeNode(binary.Left), NormalizeNode(binary.Right)],
        AssignmentExpressionSyntax assign => ["assign", NormalizeNode(assign.Left), NormalizeNode(assign.Right)],
        _ => NormalizeUnaryExpression(node),
    };

    private static List<object> NormalizeUnaryExpression(ExpressionSyntax node) => node switch
    {
        PrefixUnaryExpressionSyntax unary => ["unary", NormalizeNode(unary.Operand)],
        PostfixUnaryExpressionSyntax postUnary => ["unary", NormalizeNode(postUnary.Operand)],
        ParenthesizedExpressionSyntax paren => [NormalizeNode(paren.Expression)],
        _ => NormalizeNestedExpression(node),
    };

    private static List<object> NormalizeNestedExpression(ExpressionSyntax node) => node switch
    {
        InvocationExpressionSyntax invoke => NormalizeInvoke(invoke),
        MemberAccessExpressionSyntax member => ["member", NormalizeNode(member.Expression), NormalizeNode(member.Name)],
        _ => NormalizeFinalExpression(node),
    };

    private static List<object> NormalizeFinalExpression(ExpressionSyntax node) => node switch
    {
        ObjectCreationExpressionSyntax creation => NormalizeCreation(creation),
        ConditionalExpressionSyntax cond => ["ternary", NormalizeNode(cond.Condition), NormalizeNode(cond.WhenTrue), NormalizeNode(cond.WhenFalse)],
        _ => WalkChildren(node),
    };

    private static List<object> NormalizeStatement(StatementSyntax node)
    {
        return node switch
        {
            IfStatementSyntax or SwitchStatementSyntax or WhileStatementSyntax
                => NormalizeControlFlowStatement(node),
            BlockSyntax or TryStatementSyntax
                => NormalizeBlockStatement(node),
            ReturnStatementSyntax or ThrowStatementSyntax or LocalDeclarationStatementSyntax or ExpressionStatementSyntax
                => NormalizeLeafStatement(node),
            ForStatementSyntax or ForEachStatementSyntax
                => NormalizeLoopStatement(node),
            _ => WalkChildren(node),
        };
    }

    private static List<object> NormalizeControlFlowStatement(StatementSyntax node) => node switch
    {
        IfStatementSyntax ifStmt => NormalizeIf(ifStmt),
        SwitchStatementSyntax switchStmt => NormalizeSwitch(switchStmt),
        WhileStatementSyntax whileStmt => ["while", NormalizeNode(whileStmt.Condition), NormalizeNode(whileStmt.Statement)],
        _ => WalkChildren(node),
    };

    private static List<object> NormalizeBlockStatement(StatementSyntax node) => node switch
    {
        BlockSyntax block => NormalizeMemberContainer("block", block.Statements),
        TryStatementSyntax tryStmt => NormalizeTry(tryStmt),
        _ => WalkChildren(node),
    };

    private static List<object> NormalizeLeafStatement(StatementSyntax node) => node switch
    {
        ReturnStatementSyntax ret => NormalizeReturn(ret),
        ThrowStatementSyntax thr => NormalizeThrow(thr),
        LocalDeclarationStatementSyntax local => NormalizeLocal(local),
        ExpressionStatementSyntax exprStmt => [NormalizeNode(exprStmt.Expression)],
        _ => WalkChildren(node),
    };

    private static List<object> NormalizeLoopStatement(StatementSyntax node) => node switch
    {
        ForStatementSyntax => NormalizeLoopTagged("for", ((ForStatementSyntax)node).Statement),
        ForEachStatementSyntax => NormalizeLoopTagged("foreach", ((ForEachStatementSyntax)node).Statement),
        _ => WalkChildren(node),
    };

    private static List<object> NormalizeReturn(ReturnStatementSyntax ret) =>
        ret.Expression != null ? ["return", NormalizeNode(ret.Expression)] : ["return"];

    private static List<object> NormalizeThrow(ThrowStatementSyntax thr) =>
        thr.Expression != null ? ["throw", NormalizeNode(thr.Expression)] : ["throw"];

    private static List<object> NormalizeLoopTagged(string tag, StatementSyntax body) =>
        [tag, NormalizeNode(body)];

    private static List<object> WalkChildren(SyntaxNode node)
    {
        var children = new List<object>();
        foreach (var child in node.ChildNodes())
            children.Add(NormalizeNode(child));
        return children.Count > 0 ? children : ["unknown"];
    }

    private static List<object> NormalizeMemberContainer(string tag, SyntaxList<StatementSyntax> statements) =>
        NormalizeMemberList(tag, statements);

    private static List<object> NormalizeMemberContainer(string tag, SyntaxList<MemberDeclarationSyntax> members) =>
        NormalizeMemberList(tag, members);

    private static List<object> NormalizeMemberList(string tag, SyntaxList<SyntaxNode> nodes)
    {
        var result = new List<object> { tag };
        foreach (var node in nodes)
            result.Add(NormalizeNode(node));
        return result;
    }

    private static List<object> NormalizeCreation(ObjectCreationExpressionSyntax creation)
    {
        var parts = new List<object> { "new" };
        AddCreationArguments(creation, parts);
        return parts;
    }

    private static void AddCreationArguments(ObjectCreationExpressionSyntax creation, List<object> parts)
    {
        if (creation.ArgumentList is null) return;
        foreach (var arg in creation.ArgumentList.Arguments)
            parts.Add(NormalizeNode(arg));
    }

    private static List<object> NormalizeSwitch(SwitchStatementSyntax switchStmt)
    {
        var parts = new List<object> { "switch", NormalizeNode(switchStmt.Expression) };
        foreach (var section in switchStmt.Sections)
            parts.Add(BuildCaseParts(section));
        return parts;
    }

    private static List<object> BuildCaseParts(SwitchSectionSyntax section)
    {
        var caseParts = new List<object> { "case" };
        foreach (var label in section.Labels)
            caseParts.Add(NormalizeNode(label));
        foreach (var stmt in section.Statements)
            caseParts.Add(NormalizeNode(stmt));
        return caseParts;
    }

    private static List<object> NormalizeTry(TryStatementSyntax tryStmt)
    {
        var parts = new List<object> { "try", NormalizeNode(tryStmt.Block) };
        foreach (var catchClause in tryStmt.Catches)
            parts.Add(NormalizeNode(catchClause.Block));
        if (tryStmt.Finally != null)
            parts.Add(NormalizeNode(tryStmt.Finally.Block));
        return parts;
    }

    private static string LiteralTag(LiteralExpressionSyntax literal)
    {
        return literal.Kind() switch
        {
            SyntaxKind.StringLiteralExpression => "string",
            SyntaxKind.NullLiteralExpression => "null",
            SyntaxKind.DefaultLiteralExpression => "default",
            _ => LiteralNumericOrBoolTag(literal),
        };
    }

    private static string LiteralNumericOrBoolTag(LiteralExpressionSyntax literal) => literal.Kind() switch
    {
        SyntaxKind.NumericLiteralExpression => "number",
        SyntaxKind.TrueLiteralExpression or SyntaxKind.FalseLiteralExpression => "bool",
        _ => "literal",
    };

    private static List<object> NormalizeIf(IfStatementSyntax node)
    {
        var result = new List<object> { "if", NormalizeNode(node.Condition), NormalizeNode(node.Statement) };
        if (node.Else != null)
        {
            // Include the else keyword and the else statement
            result.Add("else");
            result.Add(NormalizeNode(node.Else.Statement));
        }

        return result;
    }

    private static List<object> NormalizeInvoke(InvocationExpressionSyntax node)
    {
        var parts = new List<object> { "invoke", NormalizeNode(node.Expression) };
        foreach (var arg in node.ArgumentList.Arguments)
        {
            parts.Add(NormalizeNode(arg));
        }

        return parts;
    }

    private static List<object> NormalizeLocal(LocalDeclarationStatementSyntax node)
    {
        var parts = new List<object> { "local" };
        foreach (var variable in node.Declaration.Variables)
        {
            if (variable.Initializer?.Value != null)
            {
                parts.Add(NormalizeNode(variable.Initializer.Value));
            }
        }

        return parts;
    }

    private static List<object> NormalizeMethod(MethodDeclarationSyntax method)
    {
        var parts = new List<object> { "method" };
        if (method.Body != null)
        {
            parts.Add(NormalizeNode(method.Body));
        }
        else if (method.ExpressionBody != null)
        {
            parts.Add(NormalizeNode(method.ExpressionBody.Expression));
        }

        return parts;
    }

    /// <summary>
    ///     Serializes a normalized tree to a string for inspection or comparison.
    /// </summary>
    /// <returns></returns>
    public static string SerializeNormalized(IReadOnlyList<object> normalized)
    {
        return Serialize(normalized);
    }

    private static string Serialize(object node)
    {
        return node switch
        {
            string s => s,
            List<object> list => "(" + string.Join(' ', list.Select(Serialize)) + ")",
            _ => node.ToString() ?? "?",
        };
    }

    private static void CollectFingerprints(object node, HashSet<string> fingerprints)
    {
        fingerprints.Add(Serialize(node));

        if (node is List<object> list)
        {
            foreach (var child in list)
            {
                CollectFingerprints(child, fingerprints);
            }
        }
    }
}
