using ConfigLens.Domain;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConfigLens.Infrastructure.Scanners.Usage;

/// <summary>
/// Degraded analysis used when a project cannot be compiled (ADR-0003): pure
/// syntax pattern matching without type information. Everything it finds is
/// <see cref="Confidence.Low"/> because the receiver type is only guessed from
/// naming, and dynamic keys still become CL900 notes.
/// </summary>
public static class SyntaxOnlyUsageAnalyzer
{
    /// <summary>Analyzes one syntax tree without semantic information.</summary>
    /// <param name="tree">The tree to analyze.</param>
    /// <param name="cancellationToken">Cancels the analysis.</param>
    public static UsageAnalysisResult Analyze(SyntaxTree tree, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tree);

        var usages = new List<KeyUsage>();
        var findings = new List<Finding>();

        foreach (var node in tree.GetRoot(cancellationToken).DescendantNodes())
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (node)
            {
                case ElementAccessExpressionSyntax elementAccess when LooksLikeConfiguration(elementAccess.Expression):
                    AnalyzeKeyExpression(
                        elementAccess,
                        SingleArgument(elementAccess.ArgumentList.Arguments),
                        KeyUsageKind.IndexerAccess,
                        usages,
                        findings);
                    break;

                case InvocationExpressionSyntax invocation:
                    AnalyzeInvocation(invocation, usages, findings);
                    break;

                default:
                    break;
            }
        }

        return new UsageAnalysisResult(usages, findings);
    }

    private static void AnalyzeInvocation(InvocationExpressionSyntax invocation, List<KeyUsage> usages, List<Finding> findings)
    {
        var kind = MethodNameOf(invocation) switch
        {
            "GetSection" => KeyUsageKind.GetSection,
            "GetValue" => KeyUsageKind.GetValue,
            _ => (KeyUsageKind?)null,
        };
        if (kind is null)
        {
            return;
        }

        AnalyzeKeyExpression(
            invocation,
            invocation.ArgumentList.Arguments.Count >= 1 ? invocation.ArgumentList.Arguments[0].Expression : null,
            kind.Value,
            usages,
            findings);
    }

    private static void AnalyzeKeyExpression(
        SyntaxNode site,
        ExpressionSyntax? keyExpression,
        KeyUsageKind kind,
        List<KeyUsage> usages,
        List<Finding> findings)
    {
        switch (keyExpression)
        {
            case LiteralExpressionSyntax literal
                when literal.IsKind(SyntaxKind.StringLiteralExpression) && literal.Token.Value is string { Length: > 0 } key:
                usages.Add(new KeyUsage(ConfigKey.Parse(key), kind, Confidence.Low, LocationOf(site)));
                break;

            case InterpolatedStringExpressionSyntax interpolated
                when interpolated.Contents.Any(content => content is InterpolationSyntax):
                findings.Add(new Finding(
                    RuleIds.UnresolvableKeyAccess,
                    Severity.Info,
                    Confidence.Low,
                    "Configuration key access cannot be resolved statically: the key is built dynamically.",
                    LocationOf(site),
                    "Use a string literal or a single const/readonly field for configuration keys so ConfigLens can validate them."));
                break;

            default:
                break;
        }
    }

    /// <summary>Naming heuristic replacing the type check of the semantic path.</summary>
    private static bool LooksLikeConfiguration(ExpressionSyntax receiver)
        => receiver.ToString().Contains("config", StringComparison.OrdinalIgnoreCase);

    private static string? MethodNameOf(InvocationExpressionSyntax invocation)
        => invocation.Expression switch
        {
            MemberAccessExpressionSyntax { Name: GenericNameSyntax generic } => generic.Identifier.ValueText,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
            _ => null,
        };

    private static ExpressionSyntax? SingleArgument(SeparatedSyntaxList<ArgumentSyntax> arguments)
        => arguments.Count == 1 ? arguments[0].Expression : null;

    private static SourceLocation LocationOf(SyntaxNode node)
    {
        var lineSpan = node.GetLocation().GetLineSpan();
        return new SourceLocation(lineSpan.Path, lineSpan.StartLinePosition.Line + 1);
    }
}
