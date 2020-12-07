using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.CodeAnalysis
{
    internal static class SourceGeneratorExtensions
    {
        internal static string ToStringValue(this RefKind refKind)
        {
            if (refKind == RefKind.RefReadOnly) return "ref readonly";
            switch (refKind)
            {
                case RefKind.Ref:
                    return "ref";
                case RefKind.Out:
                    return "out";
                case RefKind.In:
                    return "in";
                default:
                    return "";
            }
        }

        internal static bool IsPublic(this ISymbol symbol)
        {
            return string.Equals(symbol.DeclaredAccessibility.ToString(), "public", StringComparison.InvariantCultureIgnoreCase);
        }

        internal static string GetTypeParameters(this ClassDeclarationSyntax classDeclarationSyntax)
        {
            var result = classDeclarationSyntax.TypeParameterList?.ToFullString().Trim();
            return string.IsNullOrEmpty(result) ? null : result;
        }

        internal static string GetConstraintClauses(this ClassDeclarationSyntax classDeclarationSyntax)
        {
            var result = classDeclarationSyntax.ConstraintClauses.ToFullString().Trim();
            return string.IsNullOrEmpty(result) ? null : result;
        }

        internal static bool IsPublic(this MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return methodDeclarationSyntax.Modifiers.Select(x => x.ValueText).Contains("public");
        }
        internal static bool IsStatic(this MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return methodDeclarationSyntax.Modifiers.Select(x => x.ValueText).Contains("static");
        }

        internal static bool ReturnsVoid(this MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return methodDeclarationSyntax.ReturnType.ToFullString().Trim() == "void";
        }

        internal static string GetSignatureText(this MethodDeclarationSyntax methodDeclarationSyntax)
        {
            var name = methodDeclarationSyntax.Identifier.ValueText;
            var parameters = methodDeclarationSyntax.ParameterList?.ToFullString().Trim();
            var typeParameters = methodDeclarationSyntax.TypeParameterList?.ToFullString().Trim();
            var constraintClauses = methodDeclarationSyntax.ConstraintClauses.ToFullString().Replace(System.Environment.NewLine, "").Trim();
            var returnType = methodDeclarationSyntax.ReturnType.ToFullString().Trim();

            return $"{returnType} {name}{typeParameters}{parameters} {constraintClauses}".Trim();
        }


        internal static string GetParametersText(this ParameterListSyntax parameterListSyntax)
        {
            if (parameterListSyntax == null || parameterListSyntax.Parameters.Count == 0) return "()";
            var result = new List<string>();
            foreach (var item in parameterListSyntax.Parameters)
            {
                var variableName = item.Identifier;
                var modifiers = item.Modifiers.Select(x => x.ValueText).ToList();
                var modifiersText = modifiers.Count == 0 ? "" : modifiers.Aggregate((a, b) => a + " " + b);
                result.Add($"{modifiersText} {variableName}");
            }
            return result.Count == 0 ? "()" : $"({result.Aggregate((a, b) => a + ", " + b).Trim()})";
        }

        internal static string GetCallableSignatureText(this MethodDeclarationSyntax methodDeclarationSyntax)
        {
            var name = methodDeclarationSyntax.Identifier.ValueText;
            var parameters = methodDeclarationSyntax.ParameterList.GetParametersText();
            var typeParameters = methodDeclarationSyntax.TypeParameterList?.ToFullString().Trim();

            return $"{name}{typeParameters}{parameters}".Trim();
        }

        internal static bool TryGetObsoleteAttribute(this MethodDeclarationSyntax methodDeclarationSyntax, out string text)
        {
            var attr = methodDeclarationSyntax.AttributeLists.Where(x => x is not null && IsObsolete(x.GetText().ToString())).Select(x => x.GetText().ToString()).ToList();

            text = attr.Count != 0 ? ReplaceFirst(attr[0].Trim(), "Obsolete", "System.Obsolete") : "";
            return attr.Count != 0;

            bool IsObsolete(string text)
            {
                Match match = Regex.Match(text, @"\[\s*Obsolete[Attribute]*\s*\(");
                return match.Success;
            }
            string ReplaceFirst(string text, string search, string replace)
            {
                int pos = text.IndexOf(search);
                if (pos < 0)
                {
                    return text;
                }
                return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
            }
        }

        internal static string GetNamespace(this SyntaxNode syntaxNode)
        {
            return syntaxNode.Parent switch
            {
                NamespaceDeclarationSyntax namespaceDeclarationSyntax => namespaceDeclarationSyntax.Name.ToString(),
                null => string.Empty,
                _ => GetNamespace(syntaxNode.Parent)
            };
        }

        internal static string GetTypeParameters(this INamedTypeSymbol namedTypeSymbol)
        {
            return namedTypeSymbol.TypeParameters.Length == 0 ? ""
                : $"<{namedTypeSymbol.TypeParameters.Select(x => x.Name).Aggregate((a, b) => $"{a}, {b}")}>";
        }

        internal static string GetConstraintClauses(this INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol.TypeParameters.Length == 0) return "";
            var result = new List<string>();
            foreach (var item in namedTypeSymbol.TypeParameters)
            {
                var constraintType = item.ToDisplayString();
                var constraintItems = item.ConstraintTypes.Select(x => x.ToDisplayString()).Aggregate((a, b) => $"{a}, {b}").Trim();
                result.Add($"where {constraintType} : {constraintItems}".Trim());
            }

            return result.Aggregate((a, b) => $"{a} {b}").Trim();
        }

        internal static string GetBaseList(this INamedTypeSymbol namedTypeSymbol, params string[] others)
        {
            var result = new List<string>();
            if (namedTypeSymbol.BaseType != null && !string.Equals(namedTypeSymbol.BaseType.Name, "object", StringComparison.InvariantCultureIgnoreCase))
                result.Add(namedTypeSymbol.BaseType.Name);
            if (namedTypeSymbol.AllInterfaces.Length != 0)
            {
                foreach (var item in namedTypeSymbol.AllInterfaces)
                {
                    result.Add(item.Name);
                }
            }
            if (others != null && others.Length != 0)
            {
                foreach (var item in others)
                {
                    if (!string.IsNullOrEmpty(item))
                        result.Add(item);
                }
            }
            return result.Count == 0 ? "" : $": {result.Aggregate((a, b) => $"{a}, {b}")}".Trim();
        }

        private static string getKind(IParameterSymbol parameterSymbol)
        {
            return parameterSymbol.IsParams ? "params" : parameterSymbol.RefKind.ToStringValue();
        }

        private static string getDefaultValue(IParameterSymbol parameterSymbol)
        {
            if (parameterSymbol.HasExplicitDefaultValue)
            {
                if (parameterSymbol.ExplicitDefaultValue == null)
                    return $" = null";
                if (parameterSymbol.ExplicitDefaultValue is bool)
                    return $" = {parameterSymbol.ExplicitDefaultValue.ToString().ToLowerInvariant()}";
                if (parameterSymbol.ExplicitDefaultValue is string)
                    return $" = \"{parameterSymbol.ExplicitDefaultValue}\"";
                else
                    return $" = {parameterSymbol.ExplicitDefaultValue}";
            }
            return "";
        }

        private static string getConstraintClauses(ITypeParameterSymbol typeParameterSymbol)
        {
            if (typeParameterSymbol.ConstraintTypes.Length > 0)
            {
                var constraintType = typeParameterSymbol.ToDisplayString();
                var constraintItems = typeParameterSymbol.ConstraintTypes.Select(x => x.ToDisplayString()).Aggregate((a, b) => $"{a}, {b}").Trim();
                return $"where {constraintType} : {constraintItems}".Trim();
            }
            return "";
        }
        internal static string GetSignatureText(this IMethodSymbol methodSymbol)
        {

            var name = methodSymbol.Name;

            var parametersText = methodSymbol.Parameters.Length == 0 ? "()"
                : "(" + methodSymbol.Parameters.Select(x => getKind(x) + $" {x.Type} " + x.Name + getDefaultValue(x))
                                  .Aggregate((a, b) => a + ", " + b).Trim() + ")";

            var returnType = methodSymbol.ReturnsVoid ? "void" : methodSymbol.ReturnType.ToDisplayString();
            var typeParameters = methodSymbol.TypeParameters.Length == 0
                ? ""
                : "<" + methodSymbol.TypeParameters.Select(x => x.Name).Aggregate((a, b) => $"{a}, {b}").Trim() + ">";
            var constraintClauses = methodSymbol.TypeParameters.Length == 0
                ? ""
                : methodSymbol.TypeParameters.Select(x => getConstraintClauses(x)).Aggregate((a, b) => $"{a} {b}")
            ;

            return $"{returnType} {name}{typeParameters}{parametersText} {constraintClauses}".Trim();
        }

        internal static string GetCallableSignatureText(this IMethodSymbol methodSymbol)
        {
            var name = methodSymbol.Name;

            var parametersText = methodSymbol.Parameters.Length == 0 ? "()"
                : "(" + methodSymbol.Parameters.Select(x => $"{getKind(x)} {x.Name}")
                                  .Aggregate((a, b) => $"{a}, {b}").Trim() + ")";
            var typeParameters = methodSymbol.TypeParameters.Length == 0
                ? ""
                : "<" + methodSymbol.TypeParameters.Select(x => x.Name).Aggregate((a, b) => $"{a}, {b}").Trim() + ">";

            return $"{name}{typeParameters}{parametersText}".Trim();
        }
    }
}