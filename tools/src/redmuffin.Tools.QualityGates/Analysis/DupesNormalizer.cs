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
    public static List<object> Normalize(SyntaxNode root)
    {
        return NormalizeNode(root);
    }

    /// <summary>
    ///     Computes a set of structural fingerprints by walking the
    ///     normalized tree and serializing every sub-form to a string.
    /// </summary>
    public static HashSet<string> ComputeFingerprints(List<object> normalized)
    {
        var fingerprints = new HashSet<string>(StringComparer.Ordinal);
        CollectFingerprints(normalized, fingerprints);
        return fingerprints;
    }

    private static List<object> NormalizeNode(SyntaxNode node)
    {
        switch (node)
        {
            case IdentifierNameSyntax:
                return ["symbol"];
            case LiteralExpressionSyntax literal:
                return [LiteralTag(literal)];
            case IfStatementSyntax ifStmt:
                return NormalizeIf(ifStmt);
            case BinaryExpressionSyntax binary:
                return ["binary", NormalizeNode(binary.Left), NormalizeNode(binary.Right)];
            case InvocationExpressionSyntax invoke:
                return NormalizeInvoke(invoke);
            case BlockSyntax block:
            {
                var result = new List<object> { "block" };
                foreach (var stmt in block.Statements)
                    result.Add(NormalizeNode(stmt));
                return result;
            }

            case ReturnStatementSyntax ret:
                return ret.Expression != null
                    ? ["return", NormalizeNode(ret.Expression)]
                    : ["return"];
            case LocalDeclarationStatementSyntax local:
                return NormalizeLocal(local);
            case PrefixUnaryExpressionSyntax unary:
                return ["unary", NormalizeNode(unary.Operand)];
            case PostfixUnaryExpressionSyntax postUnary:
                return ["unary", NormalizeNode(postUnary.Operand)];
            case AssignmentExpressionSyntax assign:
                return ["assign", NormalizeNode(assign.Left), NormalizeNode(assign.Right)];
            case ArgumentSyntax arg:
                return NormalizeNode(arg.Expression);
            case MemberAccessExpressionSyntax member:
                return ["member", NormalizeNode(member.Expression), NormalizeNode(member.Name)];
            case ObjectCreationExpressionSyntax creation:
            {
                var parts = new List<object> { "new" };
                if (creation.ArgumentList != null)
                {
                    foreach (var arg in creation.ArgumentList.Arguments)
                        parts.Add(NormalizeNode(arg));
                }

                return parts;
            }

            case VariableDeclarationSyntax:
                // Handled by LocalDeclarationStatement; skip standalone
                return ["declare"];
            case ExpressionStatementSyntax exprStmt:
                return [NormalizeNode(exprStmt.Expression)];
            case ParenthesizedExpressionSyntax paren:
                return [NormalizeNode(paren.Expression)];
            case ConditionalExpressionSyntax cond:
                return ["ternary", NormalizeNode(cond.Condition), NormalizeNode(cond.WhenTrue), NormalizeNode(cond.WhenFalse)];
            case ForStatementSyntax forStmt:
            {
                var parts = new List<object> { "for" };
                parts.Add(NormalizeNode(forStmt.Statement));
                return parts;
            }

            case ForEachStatementSyntax forEach:
            {
                var parts = new List<object> { "foreach" };
                parts.Add(NormalizeNode(forEach.Statement));
                return parts;
            }

            case WhileStatementSyntax whileStmt:
                return ["while", NormalizeNode(whileStmt.Condition), NormalizeNode(whileStmt.Statement)];
            case SwitchStatementSyntax switchStmt:
            {
                var parts = new List<object> { "switch", NormalizeNode(switchStmt.Expression) };
                foreach (var section in switchStmt.Sections)
                {
                    var caseParts = new List<object> { "case" };
                    foreach (var label in section.Labels)
                        caseParts.Add(NormalizeNode(label));
                    foreach (var stmt in section.Statements)
                        caseParts.Add(NormalizeNode(stmt));
                    parts.Add(caseParts);
                }

                return parts;
            }

            case ThrowStatementSyntax thr:
                return thr.Expression != null ? ["throw", NormalizeNode(thr.Expression)] : ["throw"];
            case TryStatementSyntax tryStmt:
            {
                var parts = new List<object> { "try", NormalizeNode(tryStmt.Block) };
                foreach (var catchClause in tryStmt.Catches)
                    parts.Add(NormalizeNode(catchClause.Block));
                if (tryStmt.Finally != null)
                    parts.Add(NormalizeNode(tryStmt.Finally.Block));
                return parts;
            }

            case MethodDeclarationSyntax method:
                return NormalizeMethod(method);
            case ClassDeclarationSyntax cls:
            {
                var parts = new List<object> { "class" };
                foreach (var member in cls.Members)
                    parts.Add(NormalizeNode(member));
                return parts;
            }

            case CompilationUnitSyntax unit:
            {
                var parts = new List<object> { "unit" };
                foreach (var member in unit.Members)
                    parts.Add(NormalizeNode(member));
                return parts;
            }

            case NamespaceDeclarationSyntax ns:
            {
                var parts = new List<object> { "namespace" };
                foreach (var member in ns.Members)
                    parts.Add(NormalizeNode(member));
                return parts;
            }

            case FileScopedNamespaceDeclarationSyntax fileNs:
            {
                var parts = new List<object> { "namespace" };
                foreach (var member in fileNs.Members)
                    parts.Add(NormalizeNode(member));
                return parts;
            }

            default:
                return WalkChildren(node);
        }
    }

    private static List<object> WalkChildren(SyntaxNode node)
    {
        var children = new List<object>();
        foreach (var child in node.ChildNodes())
            children.Add(NormalizeNode(child));
        return children.Count > 0 ? children : ["unknown"];
    }

    private static string LiteralTag(LiteralExpressionSyntax literal)
    {
        return literal.Kind() switch
        {
            SyntaxKind.StringLiteralExpression => "string",
            SyntaxKind.NumericLiteralExpression => "number",
            SyntaxKind.TrueLiteralExpression or SyntaxKind.FalseLiteralExpression => "bool",
            SyntaxKind.NullLiteralExpression => "null",
            SyntaxKind.DefaultLiteralExpression => "default",
            _ => "literal"
        };
    }

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
    public static string SerializeNormalized(List<object> normalized)
    {
        return Serialize(normalized);
    }

    private static string Serialize(object node)
    {
        return node switch
        {
            string s => s,
            List<object> list => "(" + string.Join(" ", list.Select(Serialize)) + ")",
            _ => node.ToString() ?? "?"
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
