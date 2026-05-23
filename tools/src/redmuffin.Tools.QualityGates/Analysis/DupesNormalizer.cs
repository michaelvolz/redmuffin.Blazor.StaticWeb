namespace redmuffin.Tools.QualityGates.Analysis;

using System.Globalization;
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
    public static NormalizedNode Normalize(SyntaxNode root)
    {
        return NormalizeNode(root);
    }

    /// <summary>
    ///     Computes a set of structural fingerprints by walking the
    ///     normalized tree and serializing every sub-form to a string.
    /// </summary>
    /// <returns></returns>
    public static ISet<string> ComputeFingerprints(NormalizedNode normalized)
    {
        var fingerprints = new HashSet<string>(StringComparer.Ordinal);
        CollectFingerprints(normalized, fingerprints);
        return fingerprints;
    }

    private static NormalizedNode NormalizeNode(SyntaxNode node)
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
            VariableDeclarationSyntax => new NormalizedNode("declare"),
            ArgumentSyntax a => NormalizeNode(a.Expression),
            _ => WalkChildren(node),
        };
    }

    private static NormalizedNode NormalizeExpression(ExpressionSyntax node)
    {
        return node switch
        {
            IdentifierNameSyntax => new NormalizedNode("symbol"),
            LiteralExpressionSyntax literal => new NormalizedNode(LiteralTag(literal)),
            _ => NormalizeComplexExpression(node),
        };
    }

    private static NormalizedNode NormalizeComplexExpression(ExpressionSyntax node) => node switch
    {
        BinaryExpressionSyntax binary => Node("binary", NormalizeNode(binary.Left), NormalizeNode(binary.Right)),
        AssignmentExpressionSyntax assign => Node("assign", NormalizeNode(assign.Left), NormalizeNode(assign.Right)),
        _ => NormalizeUnaryExpression(node),
    };

    private static NormalizedNode NormalizeUnaryExpression(ExpressionSyntax node) => node switch
    {
        PrefixUnaryExpressionSyntax unary => Node("unary", NormalizeNode(unary.Operand)),
        PostfixUnaryExpressionSyntax postUnary => Node("unary", NormalizeNode(postUnary.Operand)),
        ParenthesizedExpressionSyntax paren => NormalizeNode(paren.Expression),
        _ => NormalizeNestedExpression(node),
    };

    private static NormalizedNode NormalizeNestedExpression(ExpressionSyntax node) => node switch
    {
        InvocationExpressionSyntax invoke => NormalizeInvoke(invoke),
        MemberAccessExpressionSyntax member => Node("member", NormalizeNode(member.Expression), NormalizeNode(member.Name)),
        _ => NormalizeFinalExpression(node),
    };

    private static NormalizedNode NormalizeFinalExpression(ExpressionSyntax node) => node switch
    {
        ObjectCreationExpressionSyntax creation => NormalizeCreation(creation),
        ConditionalExpressionSyntax cond => Node("ternary", NormalizeNode(cond.Condition), NormalizeNode(cond.WhenTrue), NormalizeNode(cond.WhenFalse)),
        _ => WalkChildren(node),
    };

    private static NormalizedNode NormalizeStatement(StatementSyntax node)
    {
        return node switch
        {
            IfStatementSyntax or SwitchStatementSyntax or WhileStatementSyntax => NormalizeControlFlowStatement(node),
            BlockSyntax or TryStatementSyntax => NormalizeBlockStatement(node),
            ReturnStatementSyntax or ThrowStatementSyntax or LocalDeclarationStatementSyntax or ExpressionStatementSyntax => NormalizeLeafStatement(node),
            ForStatementSyntax or ForEachStatementSyntax => NormalizeLoopStatement(node),
            _ => WalkChildren(node),
        };
    }

    private static NormalizedNode NormalizeControlFlowStatement(StatementSyntax node) => node switch
    {
        IfStatementSyntax ifStmt => NormalizeIf(ifStmt),
        SwitchStatementSyntax switchStmt => NormalizeSwitch(switchStmt),
        WhileStatementSyntax whileStmt => Node("while", NormalizeNode(whileStmt.Condition), NormalizeNode(whileStmt.Statement)),
        _ => WalkChildren(node),
    };

    private static NormalizedNode NormalizeBlockStatement(StatementSyntax node) => node switch
    {
        BlockSyntax block => NormalizeMemberContainer("block", block.Statements),
        TryStatementSyntax tryStmt => NormalizeTry(tryStmt),
        _ => WalkChildren(node),
    };

    private static NormalizedNode NormalizeLeafStatement(StatementSyntax node) => node switch
    {
        ReturnStatementSyntax ret => NormalizeReturn(ret),
        ThrowStatementSyntax thr => NormalizeThrow(thr),
        LocalDeclarationStatementSyntax local => NormalizeLocal(local),
        ExpressionStatementSyntax exprStmt => NormalizeNode(exprStmt.Expression),
        _ => WalkChildren(node),
    };

    private static NormalizedNode NormalizeLoopStatement(StatementSyntax node) => node switch
    {
        ForStatementSyntax forStmt => Node("for", NormalizeNode(forStmt.Statement)),
        ForEachStatementSyntax forEachStmt => Node("foreach", NormalizeNode(forEachStmt.Statement)),
        _ => WalkChildren(node),
    };

    private static NormalizedNode NormalizeReturn(ReturnStatementSyntax ret) =>
        ret.Expression != null
            ? Node("return", NormalizeNode(ret.Expression))
            : new NormalizedNode("return");

    private static NormalizedNode NormalizeThrow(ThrowStatementSyntax thr) =>
        thr.Expression != null
            ? Node("throw", NormalizeNode(thr.Expression))
            : new NormalizedNode("throw");

    private static NormalizedNode WalkChildren(SyntaxNode node)
    {
        var children = new List<NormalizedNode>();
        foreach (var child in node.ChildNodes())
            children.Add(NormalizeNode(child));
        return children.Count > 0
            ? new NormalizedNode("unknown", children)
            : new NormalizedNode("unknown");
    }

    private static NormalizedNode NormalizeMemberContainer(string tag, SyntaxList<StatementSyntax> statements) =>
        NormalizeMemberList(tag, statements);

    private static NormalizedNode NormalizeMemberContainer(string tag, SyntaxList<MemberDeclarationSyntax> members) =>
        NormalizeMemberList(tag, members);

    private static NormalizedNode NormalizeMemberList(string tag, SyntaxList<SyntaxNode> nodes)
    {
        var children = new List<NormalizedNode>();
        foreach (var node in nodes)
            children.Add(NormalizeNode(node));
        return new NormalizedNode(tag, children);
    }

    private static NormalizedNode NormalizeCreation(ObjectCreationExpressionSyntax creation)
    {
        var children = new List<NormalizedNode>();
        if (creation.ArgumentList is not null)
        {
            foreach (var arg in creation.ArgumentList.Arguments)
                children.Add(NormalizeNode(arg));
        }

        return new NormalizedNode("new", children);
    }

    private static NormalizedNode NormalizeSwitch(SwitchStatementSyntax switchStmt)
    {
        var children = new List<NormalizedNode> { NormalizeNode(switchStmt.Expression) };
        foreach (var section in switchStmt.Sections)
            children.Add(BuildCaseNode(section));
        return new NormalizedNode("switch", children);
    }

    private static NormalizedNode BuildCaseNode(SwitchSectionSyntax section)
    {
        var children = new List<NormalizedNode>();
        foreach (var label in section.Labels)
            children.Add(NormalizeNode(label));
        foreach (var stmt in section.Statements)
            children.Add(NormalizeNode(stmt));
        return new NormalizedNode("case", children);
    }

    private static NormalizedNode NormalizeTry(TryStatementSyntax tryStmt)
    {
        var children = new List<NormalizedNode> { NormalizeNode(tryStmt.Block) };
        foreach (var catchClause in tryStmt.Catches)
            children.Add(NormalizeNode(catchClause.Block));
        if (tryStmt.Finally != null)
            children.Add(NormalizeNode(tryStmt.Finally.Block));
        return new NormalizedNode("try", children);
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

    private static NormalizedNode NormalizeIf(IfStatementSyntax node)
    {
        var children = new List<NormalizedNode> { NormalizeNode(node.Condition), NormalizeNode(node.Statement) };
        if (node.Else != null)
        {
            children.Add(new NormalizedNode("else"));
            children.Add(NormalizeNode(node.Else.Statement));
        }

        return new NormalizedNode("if", children);
    }

    private static NormalizedNode NormalizeInvoke(InvocationExpressionSyntax node)
    {
        var children = new List<NormalizedNode> { NormalizeNode(node.Expression) };
        foreach (var arg in node.ArgumentList.Arguments)
            children.Add(NormalizeNode(arg));
        return new NormalizedNode("invoke", children);
    }

    private static NormalizedNode NormalizeLocal(LocalDeclarationStatementSyntax node)
    {
        var children = new List<NormalizedNode>();
        foreach (var variable in node.Declaration.Variables)
        {
            if (variable.Initializer?.Value != null)
                children.Add(NormalizeNode(variable.Initializer.Value));
        }

        return new NormalizedNode("local", children);
    }

    private static NormalizedNode NormalizeMethod(MethodDeclarationSyntax method)
    {
        if (method.Body != null)
            return new NormalizedNode("method", [NormalizeNode(method.Body)]);

        if (method.ExpressionBody != null)
            return new NormalizedNode("method", [NormalizeNode(method.ExpressionBody.Expression)]);

        return new NormalizedNode("method");
    }

    /// <summary>
    ///     Serializes a normalized tree to a string for inspection or comparison.
    /// </summary>
    /// <returns></returns>
    public static string SerializeNormalized(NormalizedNode normalized)
    {
        return Serialize(normalized);
    }

    private static string Serialize(NormalizedNode node)
    {
        if (node.Children.Count == 0)
            return node.Kind;

        var childStrings = node.Children.Select(Serialize);
        return string.Create(CultureInfo.InvariantCulture, $"({node.Kind} {string.Join(' ', childStrings)})");
    }

    private static void CollectFingerprints(NormalizedNode node, HashSet<string> fingerprints)
    {
        fingerprints.Add(Serialize(node));
        foreach (var child in node.Children)
            CollectFingerprints(child, fingerprints);
    }

    private static NormalizedNode Node(string kind, params NormalizedNode[] children) =>
        new(kind, children);
}
