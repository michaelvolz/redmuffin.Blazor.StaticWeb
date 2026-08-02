namespace redmuffin.Tools.QualityGates.Analysis;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class MutationRules
{
    private static readonly HashSet<string> RandomMethodNames = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "Random", "Next", "Seed", "RandomRange", "GenerateSeed",
        "NewSeed", "CreateSeed", "GetRandom", "NextRandom", "RandomSeed",
    };

    public static IReadOnlyList<MutationRule> All { get; } =
    [
        // Arithmetic
        new(
            Category: MutationCategory.Arithmetic,
            OriginalKind: SyntaxKind.AddExpression,
            MutantKind: SyntaxKind.SubtractExpression,
            Description: "Replace addition with subtraction"),

        new(
            Category: MutationCategory.Arithmetic,
            OriginalKind: SyntaxKind.SubtractExpression,
            MutantKind: SyntaxKind.AddExpression,
            Description: "Replace subtraction with addition"),

        new(
            Category: MutationCategory.Arithmetic,
            OriginalKind: SyntaxKind.MultiplyExpression,
            MutantKind: SyntaxKind.DivideExpression,
            Description: "Replace multiplication with division"),

        new(
            Category: MutationCategory.Arithmetic,
            OriginalKind: SyntaxKind.PreIncrementExpression,
            MutantKind: SyntaxKind.PreDecrementExpression,
            Description: "Replace pre-increment with pre-decrement"),

        new(
            Category: MutationCategory.Arithmetic,
            OriginalKind: SyntaxKind.PreDecrementExpression,
            MutantKind: SyntaxKind.PreIncrementExpression,
            Description: "Replace pre-decrement with pre-increment"),

        new(
            Category: MutationCategory.Arithmetic,
            OriginalKind: SyntaxKind.PostIncrementExpression,
            MutantKind: SyntaxKind.PostDecrementExpression,
            Description: "Replace post-increment with post-decrement"),

        new(
            Category: MutationCategory.Arithmetic,
            OriginalKind: SyntaxKind.PostDecrementExpression,
            MutantKind: SyntaxKind.PostIncrementExpression,
            Description: "Replace post-decrement with post-increment"),

        // Comparison
        new(
            Category: MutationCategory.Comparison,
            OriginalKind: SyntaxKind.GreaterThanExpression,
            MutantKind: SyntaxKind.GreaterThanOrEqualExpression,
            Description: "Replace greater-than with greater-than-or-equal",
            SuppressionPredicate: ShouldSuppressComparisonBoundary),

        new(
            Category: MutationCategory.Comparison,
            OriginalKind: SyntaxKind.GreaterThanOrEqualExpression,
            MutantKind: SyntaxKind.GreaterThanExpression,
            Description: "Replace greater-than-or-equal with greater-than",
            SuppressionPredicate: ShouldSuppressComparisonBoundary),

        new(
            Category: MutationCategory.Comparison,
            OriginalKind: SyntaxKind.LessThanExpression,
            MutantKind: SyntaxKind.LessThanOrEqualExpression,
            Description: "Replace less-than with less-than-or-equal",
            SuppressionPredicate: ShouldSuppressComparisonBoundary),

        new(
            Category: MutationCategory.Comparison,
            OriginalKind: SyntaxKind.LessThanOrEqualExpression,
            MutantKind: SyntaxKind.LessThanExpression,
            Description: "Replace less-than-or-equal with less-than",
            SuppressionPredicate: ShouldSuppressComparisonBoundary),

        // Equality
        new(
            Category: MutationCategory.Equality,
            OriginalKind: SyntaxKind.EqualsExpression,
            MutantKind: SyntaxKind.NotEqualsExpression,
            Description: "Replace equals with not-equals"),

        new(
            Category: MutationCategory.Equality,
            OriginalKind: SyntaxKind.NotEqualsExpression,
            MutantKind: SyntaxKind.EqualsExpression,
            Description: "Replace not-equals with equals"),

        // Boolean
        new(
            Category: MutationCategory.Boolean,
            OriginalKind: SyntaxKind.TrueLiteralExpression,
            MutantKind: SyntaxKind.FalseLiteralExpression,
            Description: "Replace true with false"),

        new(
            Category: MutationCategory.Boolean,
            OriginalKind: SyntaxKind.FalseLiteralExpression,
            MutantKind: SyntaxKind.TrueLiteralExpression,
            Description: "Replace false with true"),

        // Conditional — match IfStatementSyntax, site is on condition
        new(
            Category: MutationCategory.Conditional,
            OriginalKind: SyntaxKind.IfStatement,
            MutantKind: SyntaxKind.IfStatement,
            Description: "Negate condition expression"),

        // Constant
        new(
            Category: MutationCategory.Constant,
            OriginalKind: SyntaxKind.NumericLiteralExpression,
            MutantKind: SyntaxKind.NumericLiteralExpression,
            Description: "Replace 0 with 1",
            MatchPredicate: IsLiteralZero,
            SuppressionPredicate: ShouldSuppressConstantInRandomMethod),

        new(
            Category: MutationCategory.Constant,
            OriginalKind: SyntaxKind.NumericLiteralExpression,
            MutantKind: SyntaxKind.NumericLiteralExpression,
            Description: "Replace 1 with 0",
            MatchPredicate: IsLiteralOne,
            SuppressionPredicate: ShouldSuppressConstantInRandomMethod),

        // Logical (mutate4java CONDITIONAL_AND / CONDITIONAL_OR)
        new(
            Category: MutationCategory.Logical,
            OriginalKind: SyntaxKind.LogicalAndExpression,
            MutantKind: SyntaxKind.LogicalOrExpression,
            Description: "Replace && with ||"),

        new(
            Category: MutationCategory.Logical,
            OriginalKind: SyntaxKind.LogicalOrExpression,
            MutantKind: SyntaxKind.LogicalAndExpression,
            Description: "Replace || with &&"),

        // Unary strip (mutate4java removablePrefix for ! and -)
        new(
            Category: MutationCategory.Unary,
            OriginalKind: SyntaxKind.LogicalNotExpression,
            MutantKind: SyntaxKind.IdentifierName,
            Description: "Strip logical not"),

        new(
            Category: MutationCategory.Unary,
            OriginalKind: SyntaxKind.UnaryMinusExpression,
            MutantKind: SyntaxKind.IdentifierName,
            Description: "Strip unary minus"),
    ];

    public static IReadOnlyList<MutationRule> GetByCategory(MutationCategory category) =>
        All.Where(r => r.Category == category).ToList();

    private static bool IsLiteralZero(SyntaxNode node) =>
        node is LiteralExpressionSyntax literal
        && literal.Kind() == SyntaxKind.NumericLiteralExpression
        && literal.Token.Value is int value
        && value == 0;

    private static bool IsLiteralOne(SyntaxNode node) =>
        node is LiteralExpressionSyntax literal
        && literal.Kind() == SyntaxKind.NumericLiteralExpression
        && literal.Token.Value is int value
        && value == 1;

    private static bool ShouldSuppressComparisonBoundary(
        SyntaxNode node, SyntaxNode parent)
    {
        if (node is not BinaryExpressionSyntax binary)
        {
            return false;
        }

        var left = binary.Left;
        if (!IsCountOrLengthAccess(left))
        {
            return false;
        }

        return IsLiteralZeroOrOne(binary.Right);
    }

    private static bool IsCountOrLengthAccess(ExpressionSyntax expression)
    {
        if (expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        var name = memberAccess.Name.Identifier.Text;
        return string.Equals(name, "Count", StringComparison.Ordinal)
            || string.Equals(name, "Length", StringComparison.Ordinal);
    }

    private static bool IsLiteralZeroOrOne(ExpressionSyntax expression)
    {
        return expression is LiteralExpressionSyntax literal
            && literal.Kind() == SyntaxKind.NumericLiteralExpression
            && literal.Token.Value is int value
            && (value == 0 || value == 1);
    }

    private static bool ShouldSuppressConstantInRandomMethod(
        SyntaxNode node, SyntaxNode parent)
    {
        for (var current = parent; current is not null; current = current.Parent)
        {
            if (current is MethodDeclarationSyntax method
                && RandomMethodNames.Contains(method.Identifier.Text))
            {
                return true;
            }
        }

        return false;
    }
}
