using ConfigLens.Domain;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConfigLens.Infrastructure.Scanners.Usage;

/// <summary>
/// Semantic analysis of configuration reads in one document: indexer access,
/// <c>GetValue</c>, <c>GetSection</c> (including composed chains) and options
/// binding via <c>Configure&lt;T&gt;</c>/<c>Bind</c>/<c>Get&lt;T&gt;</c>.
/// Resolution strength maps to confidence per ADR-0002: string literals are
/// High, one level of indirection (const, <c>nameof</c>, readonly field) is
/// Medium, and anything unresolvable becomes a CL900 finding — never a guess.
/// </summary>
public static class ConfigUsageAnalyzer
{
    private const string ConfigurationNamespace = "Microsoft.Extensions.Configuration";
    private const string DependencyInjectionNamespace = "Microsoft.Extensions.DependencyInjection";

    /// <summary>Analyzes one document and returns usages plus CL900 findings.</summary>
    /// <param name="semanticModel">Semantic model of the document to analyze.</param>
    /// <param name="cancellationToken">Cancels the analysis.</param>
    public static UsageAnalysisResult Analyze(SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(semanticModel);

        var usages = new List<KeyUsage>();
        var findings = new List<Finding>();
        var root = semanticModel.SyntaxTree.GetRoot(cancellationToken);

        foreach (var node in root.DescendantNodes())
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (node)
            {
                case ElementAccessExpressionSyntax elementAccess:
                    AnalyzeIndexer(semanticModel, elementAccess, usages, findings, cancellationToken);
                    break;

                case InvocationExpressionSyntax invocation:
                    AnalyzeInvocation(semanticModel, invocation, usages, findings, cancellationToken);
                    break;

                default:
                    break;
            }
        }

        return new UsageAnalysisResult(usages, findings);
    }

    private static void AnalyzeIndexer(
        SemanticModel semanticModel,
        ElementAccessExpressionSyntax elementAccess,
        List<KeyUsage> usages,
        List<Finding> findings,
        CancellationToken cancellationToken)
    {
        var receiverType = semanticModel.GetTypeInfo(elementAccess.Expression, cancellationToken).Type;
        if (!IsConfigurationType(receiverType))
        {
            return;
        }

        var argument = elementAccess.ArgumentList.Arguments.Count == 1
            ? elementAccess.ArgumentList.Arguments[0].Expression
            : null;
        if (argument is null)
        {
            return;
        }

        var prefix = ResolveSectionPrefix(semanticModel, elementAccess.Expression, cancellationToken);
        if (!prefix.Resolved)
        {
            if (!prefix.AlreadyReported)
            {
                findings.Add(Unresolvable(elementAccess, "the configuration section it is read from cannot be traced to a GetSection call"));
            }

            return;
        }

        var key = ResolveKey(semanticModel, argument, cancellationToken);
        if (key is null)
        {
            findings.Add(Unresolvable(elementAccess, "the key is built dynamically"));
            return;
        }

        usages.Add(new KeyUsage(
            Combine(prefix.Segments, key.Value.Value),
            KeyUsageKind.IndexerAccess,
            Min(prefix.Confidence, key.Value.Confidence),
            LocationOf(elementAccess)));
    }

    private static void AnalyzeInvocation(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        List<KeyUsage> usages,
        List<Finding> findings,
        CancellationToken cancellationToken)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(invocation, cancellationToken);
        var method = symbolInfo.Symbol as IMethodSymbol
            ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
        if (method is null)
        {
            return;
        }

        switch (method.Name)
        {
            case "GetSection" when IsInNamespace(method, ConfigurationNamespace):
                AnalyzeKeyedRead(semanticModel, invocation, method, KeyUsageKind.GetSection, usages, findings, cancellationToken);
                break;

            case "GetValue" when IsInNamespace(method, ConfigurationNamespace):
                AnalyzeKeyedRead(semanticModel, invocation, method, KeyUsageKind.GetValue, usages, findings, cancellationToken);
                break;

            case "GetConnectionString" when IsInNamespace(method, ConfigurationNamespace):
                AnalyzeKeyedRead(semanticModel, invocation, method, KeyUsageKind.GetValue, usages, findings, cancellationToken, "ConnectionStrings", parameterName: "name");
                break;

            case "Bind" when IsInNamespace(method, ConfigurationNamespace):
                AnalyzeBinding(semanticModel, invocation, BoundTypeFromInstanceArgument(semanticModel, invocation, cancellationToken), usages, findings, cancellationToken);
                break;

            case "Get" when IsInNamespace(method, ConfigurationNamespace) && method.TypeArguments.Length == 1:
                AnalyzeBinding(semanticModel, invocation, method.TypeArguments[0], usages, findings, cancellationToken);
                break;

            case "Configure" when IsInNamespace(method, DependencyInjectionNamespace) && method.TypeArguments.Length == 1:
                AnalyzeConfigureRegistration(semanticModel, invocation, method, usages, findings, cancellationToken);
                break;

            default:
                break;
        }
    }

    /// <summary>Handles <c>GetSection("…")</c>, <c>GetValue&lt;T&gt;("…")</c> and <c>GetConnectionString("…")</c> including section chains.</summary>
    private static void AnalyzeKeyedRead(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        KeyUsageKind kind,
        List<KeyUsage> usages,
        List<Finding> findings,
        CancellationToken cancellationToken,
        string? impliedSection = null,
        string parameterName = "key")
    {
        var receiver = ReceiverOf(invocation);
        var prefix = receiver is null
            ? PrefixResult.Root
            : ResolveSectionPrefix(semanticModel, receiver, cancellationToken);
        if (!prefix.Resolved)
        {
            if (!prefix.AlreadyReported)
            {
                findings.Add(Unresolvable(invocation, "the configuration section it is read from cannot be traced to a GetSection call"));
            }

            return;
        }

        var keyArgument = FindArgument(invocation, method, parameterName);
        if (keyArgument is null)
        {
            return;
        }

        var key = ResolveKey(semanticModel, keyArgument, cancellationToken);
        if (key is null)
        {
            findings.Add(Unresolvable(invocation, "the key is built dynamically"));
            return;
        }

        var fullKey = impliedSection is null ? key.Value.Value : $"{impliedSection}{ConfigKey.Separator}{key.Value.Value}";
        usages.Add(new KeyUsage(
            Combine(prefix.Segments, fullKey),
            kind,
            Min(prefix.Confidence, key.Value.Confidence),
            LocationOf(invocation)));
    }

    /// <summary>Handles <c>section.Bind(instance)</c> and <c>section.Get&lt;T&gt;()</c>.</summary>
    private static void AnalyzeBinding(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        ITypeSymbol? boundType,
        List<KeyUsage> usages,
        List<Finding> findings,
        CancellationToken cancellationToken)
    {
        var receiver = ReceiverOf(invocation);
        if (receiver is null)
        {
            return;
        }

        var section = ResolveSectionPrefix(semanticModel, receiver, cancellationToken);
        if (!section.Resolved || section.Segments.Count == 0)
        {
            if (section is { Resolved: false, AlreadyReported: false })
            {
                findings.Add(Unresolvable(invocation, "the bound configuration section cannot be traced to a GetSection call"));
            }

            return;
        }

        usages.Add(new KeyUsage(
            ConfigKey.FromSegments(section.Segments),
            KeyUsageKind.OptionsBinding,
            section.Confidence,
            LocationOf(invocation),
            boundType?.ToDisplayString(),
            ExtractBindableProperties(boundType)));
    }

    /// <summary>Handles <c>services.Configure&lt;TOptions&gt;(configuration.GetSection("…"))</c>.</summary>
    private static void AnalyzeConfigureRegistration(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        List<KeyUsage> usages,
        List<Finding> findings,
        CancellationToken cancellationToken)
    {
        var sectionArgument = invocation.ArgumentList.Arguments
            .Select(argument => argument.Expression)
            .FirstOrDefault(expression => IsConfigurationType(semanticModel.GetTypeInfo(expression, cancellationToken).Type));
        if (sectionArgument is null)
        {
            return;
        }

        var section = ResolveSectionPrefix(semanticModel, sectionArgument, cancellationToken);
        if (!section.Resolved || section.Segments.Count == 0)
        {
            // Binding the configuration root has no section key to validate;
            // an unresolvable chain was already reported at the GetSection call.
            if (section is { Resolved: false, AlreadyReported: false })
            {
                findings.Add(Unresolvable(invocation, "the bound configuration section cannot be traced to a GetSection call"));
            }

            return;
        }

        usages.Add(new KeyUsage(
            ConfigKey.FromSegments(section.Segments),
            KeyUsageKind.OptionsBinding,
            section.Confidence,
            LocationOf(invocation),
            method.TypeArguments[0].ToDisplayString(),
            ExtractBindableProperties(method.TypeArguments[0])));
    }

    /// <summary>
    /// Resolves the section path an expression evaluates to by walking
    /// <c>GetSection</c> chains. A plain configuration (root) resolves to the
    /// empty prefix; a section arriving from elsewhere (parameter, local) is
    /// unresolvable because its absolute path is unknown.
    /// </summary>
    private static PrefixResult ResolveSectionPrefix(SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken)
    {
        if (expression is InvocationExpressionSyntax invocation
            && semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol { Name: "GetSection" } method
            && IsInNamespace(method, ConfigurationNamespace))
        {
            var receiver = ReceiverOf(invocation);
            var inner = receiver is null
                ? PrefixResult.Root
                : ResolveSectionPrefix(semanticModel, receiver, cancellationToken);
            if (!inner.Resolved)
            {
                return inner;
            }

            var keyArgument = FindArgument(invocation, method, "key");
            var key = keyArgument is null ? null : ResolveKey(semanticModel, keyArgument, cancellationToken);
            if (key is null)
            {
                // The GetSection invocation itself is analyzed separately and
                // reports the CL900 for its dynamic key.
                return PrefixResult.UnresolvableReported;
            }

            return new PrefixResult(
                true,
                [.. inner.Segments, .. key.Value.Value.Split(ConfigKey.Separator)],
                Min(inner.Confidence, key.Value.Confidence),
                false);
        }

        var type = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
        return IsSectionType(type) ? PrefixResult.UnresolvableUnknownOrigin : PrefixResult.Root;
    }

    /// <summary>
    /// Resolves a key expression to its string value. Literals are High;
    /// constants reached through one indirection (const field, <c>nameof</c>,
    /// constant folding, readonly field with literal initializer) are Medium.
    /// </summary>
    private static (string Value, Confidence Confidence)? ResolveKey(SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken)
    {
        var constant = semanticModel.GetConstantValue(expression, cancellationToken);
        if (constant is { HasValue: true, Value: string constantValue } && constantValue.Length > 0)
        {
            var confidence = expression is LiteralExpressionSyntax ? Confidence.High : Confidence.Medium;
            return (constantValue, confidence);
        }

        // Readonly fields are not compile-time constants, but a literal
        // initializer is still one honest level of indirection.
        if (semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol is IFieldSymbol { IsReadOnly: true } field)
        {
            var initializer = field.DeclaringSyntaxReferences
                .Select(reference => reference.GetSyntax(cancellationToken))
                .OfType<VariableDeclaratorSyntax>()
                .Select(declarator => declarator.Initializer?.Value)
                .OfType<LiteralExpressionSyntax>()
                .FirstOrDefault();
            if (initializer?.Token.Value is string readonlyValue && readonlyValue.Length > 0)
            {
                return (readonlyValue, Confidence.Medium);
            }
        }

        return null;
    }

    private static ExpressionSyntax? ReceiverOf(InvocationExpressionSyntax invocation)
        => invocation.Expression is MemberAccessExpressionSyntax memberAccess ? memberAccess.Expression : null;

    /// <summary>Finds the argument bound to the parameter with the given name.</summary>
    private static ExpressionSyntax? FindArgument(InvocationExpressionSyntax invocation, IMethodSymbol method, string parameterName)
    {
        var arguments = invocation.ArgumentList.Arguments;

        foreach (var argument in arguments)
        {
            if (string.Equals(argument.NameColon?.Name.Identifier.ValueText, parameterName, StringComparison.Ordinal))
            {
                return argument.Expression;
            }
        }

        for (var index = 0; index < method.Parameters.Length && index < arguments.Count; index++)
        {
            if (string.Equals(method.Parameters[index].Name, parameterName, StringComparison.Ordinal)
                && arguments[index].NameColon is null)
            {
                return arguments[index].Expression;
            }
        }

        return null;
    }

    private static ITypeSymbol? BoundTypeFromInstanceArgument(SemanticModel semanticModel, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
    {
        var instance = invocation.ArgumentList.Arguments.Count == 1
            ? invocation.ArgumentList.Arguments[0].Expression
            : null;
        return instance is null ? null : semanticModel.GetTypeInfo(instance, cancellationToken).Type;
    }

    /// <summary>
    /// Captures the publicly settable instance properties of an options type
    /// (including inherited ones) as plain data for the type-mismatch rule.
    /// Nullable value types are unwrapped; enum member names are recorded so
    /// values can be validated without symbol access.
    /// </summary>
    private static List<BoundProperty>? ExtractBindableProperties(ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol)
        {
            return null;
        }

        var properties = new List<BoundProperty>();
        for (var current = type; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            foreach (var property in current.GetMembers().OfType<IPropertySymbol>())
            {
                if (property.IsStatic
                    || property.DeclaredAccessibility != Accessibility.Public
                    || property.SetMethod is not { DeclaredAccessibility: Accessibility.Public })
                {
                    continue;
                }

                var propertyType = property.Type;
                if (propertyType is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
                {
                    propertyType = nullable.TypeArguments[0];
                }

                var enumMembers = propertyType.TypeKind == TypeKind.Enum
                    ? propertyType.GetMembers().OfType<IFieldSymbol>()
                        .Where(member => member.HasConstantValue)
                        .Select(member => member.Name)
                        .ToArray()
                    : null;

                properties.Add(new BoundProperty(
                    property.Name,
                    propertyType.ToDisplayString(TypeNameFormat),
                    enumMembers));
            }
        }

        return properties;
    }

    /// <summary>Stable type identifiers like <c>System.Int32</c> instead of keyword forms like <c>int</c>.</summary>
    private static readonly SymbolDisplayFormat TypeNameFormat =
        new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    private static bool IsConfigurationType(ITypeSymbol? type)
        => type is not null && (IsConfigurationInterface(type) || type.AllInterfaces.Any(IsConfigurationInterface));

    private static bool IsSectionType(ITypeSymbol? type)
        => type is not null
            && (IsInterfaceNamed(type, "IConfigurationSection")
                || type.AllInterfaces.Any(i => IsInterfaceNamed(i, "IConfigurationSection")));

    private static bool IsConfigurationInterface(ITypeSymbol type)
        => IsInterfaceNamed(type, "IConfiguration")
            || IsInterfaceNamed(type, "IConfigurationRoot")
            || IsInterfaceNamed(type, "IConfigurationSection");

    private static bool IsInterfaceNamed(ITypeSymbol type, string name)
        => string.Equals(type.Name, name, StringComparison.Ordinal)
            && string.Equals(type.ContainingNamespace?.ToDisplayString(), ConfigurationNamespace, StringComparison.Ordinal);

    private static bool IsInNamespace(IMethodSymbol method, string namespaceName)
        => string.Equals(method.ContainingType?.ContainingNamespace?.ToDisplayString(), namespaceName, StringComparison.Ordinal);

    private static ConfigKey Combine(IReadOnlyList<string> prefixSegments, string key)
        => ConfigKey.FromSegments([.. prefixSegments, .. key.Split(ConfigKey.Separator)]);

    private static Confidence Min(Confidence left, Confidence right) => left < right ? left : right;

    private static Finding Unresolvable(SyntaxNode node, string reason)
        => new(
            RuleIds.UnresolvableKeyAccess,
            Severity.Info,
            Confidence.Low,
            $"Configuration key access cannot be resolved statically: {reason}.",
            LocationOf(node),
            "Use a string literal or a single const/readonly field for configuration keys so ConfigLens can validate them.");

    private static SourceLocation LocationOf(SyntaxNode node)
    {
        var lineSpan = node.GetLocation().GetLineSpan();
        return new SourceLocation(lineSpan.Path, lineSpan.StartLinePosition.Line + 1);
    }

    /// <summary>Result of resolving a section chain to an absolute key prefix.</summary>
    private readonly record struct PrefixResult(bool Resolved, IReadOnlyList<string> Segments, Confidence Confidence, bool AlreadyReported)
    {
        /// <summary>The configuration root: empty prefix, fully trusted.</summary>
        public static PrefixResult Root { get; } = new(true, [], Confidence.High, false);

        /// <summary>Unresolvable, but the inner GetSection already produced a CL900.</summary>
        public static PrefixResult UnresolvableReported { get; } = new(false, [], Confidence.Low, true);

        /// <summary>Unresolvable section of unknown origin (parameter, field, local).</summary>
        public static PrefixResult UnresolvableUnknownOrigin { get; } = new(false, [], Confidence.Low, false);
    }
}
