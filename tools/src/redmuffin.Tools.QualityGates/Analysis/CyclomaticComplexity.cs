namespace redmuffin.Tools.QualityGates.Analysis;

using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class CyclomaticComplexity
{
    public static IReadOnlyList<MethodComplexity> Analyze(string projectPath)
    {
        var results = new List<MethodComplexity>();
        var csFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);

        foreach (var file in csFiles)
        {
            var source = File.ReadAllText(file);
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var root = syntaxTree.GetCompilationUnitRoot();
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                var cc = ComputeCyclomaticComplexity(method);
                var lineSpan = method.GetLocation().GetLineSpan();
                results.Add(new MethodComplexity(
                    method.Identifier.Text,
                    Path.GetFullPath(file),
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.EndLinePosition.Line + 1,
                    cc));
            }
        }

        return results.AsReadOnly();
    }

    private static int ComputeCyclomaticComplexity(MethodDeclarationSyntax method)
    {
        var walker = new CcWalker();
        walker.Visit(method);
        return walker.DecisionPoints + 1;
    }

    private sealed class CcWalker : CSharpSyntaxWalker
    {
        public int DecisionPoints { get; private set; }

        public override void VisitIfStatement(IfStatementSyntax node)
        {
            DecisionPoints++;
            base.VisitIfStatement(node);
        }

        public override void VisitWhileStatement(WhileStatementSyntax node)
        {
            DecisionPoints++;
            base.VisitWhileStatement(node);
        }

        public override void VisitForStatement(ForStatementSyntax node)
        {
            DecisionPoints++;
            base.VisitForStatement(node);
        }

        public override void VisitForEachStatement(ForEachStatementSyntax node)
        {
            DecisionPoints++;
            base.VisitForEachStatement(node);
        }

        public override void VisitSwitchStatement(SwitchStatementSyntax node)
        {
            // Each case is a decision point; the switch itself is not counted.
            // We count case labels in VisitCaseSwitchLabel.
            base.VisitSwitchStatement(node);
        }

        public override void VisitCaseSwitchLabel(CaseSwitchLabelSyntax node)
        {
            DecisionPoints++;
            base.VisitCaseSwitchLabel(node);
        }

        public override void VisitCatchClause(CatchClauseSyntax node)
        {
            DecisionPoints++;
            base.VisitCatchClause(node);
        }

        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            if (node.IsKind(SyntaxKind.LogicalAndExpression) ||
                node.IsKind(SyntaxKind.LogicalOrExpression) ||
                node.IsKind(SyntaxKind.CoalesceExpression))
            {
                DecisionPoints++;
            }

            base.VisitBinaryExpression(node);
        }

        public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
        {
            DecisionPoints++;
            base.VisitConditionalExpression(node);
        }

        public override void VisitSwitchExpressionArm(SwitchExpressionArmSyntax node)
        {
            DecisionPoints++;
            base.VisitSwitchExpressionArm(node);
        }

        public override void VisitBinaryPattern(BinaryPatternSyntax node)
        {
            if (node.IsKind(SyntaxKind.AndPattern) ||
                node.IsKind(SyntaxKind.OrPattern))
            {
                DecisionPoints++;
            }

            base.VisitBinaryPattern(node);
        }

        public override void VisitUnaryPattern(UnaryPatternSyntax node)
        {
            if (node.IsKind(SyntaxKind.NotPattern))
            {
                DecisionPoints++;
            }

            base.VisitUnaryPattern(node);
        }

        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            if (node.IsKind(SyntaxKind.SuppressNullableWarningExpression))
            {
                DecisionPoints++;
            }

            base.VisitPostfixUnaryExpression(node);
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            if (node.IsKind(SyntaxKind.CoalesceAssignmentExpression))
            {
                DecisionPoints++;
            }

            base.VisitAssignmentExpression(node);
        }

        public override void VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
        {
            DecisionPoints++;
            base.VisitConditionalAccessExpression(node);
        }
    }
}
