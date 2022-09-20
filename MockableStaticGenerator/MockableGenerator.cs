using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MockableStaticGenerator
{
    [Generator]
    public class MockableGenerator : ISourceGenerator
    {
        private static readonly List<string> _interfaces = new List<string>();
        private static readonly List<string> _classes = new List<string>();
        public class MethodSymbolVisitor : SymbolVisitor
        {
            private readonly string _typeName;

            public MethodSymbolVisitor(string typeName)
            {
                _typeName = typeName;
            }
            public override void VisitNamespace(INamespaceSymbol symbol)
            {
                foreach (var child in symbol.GetMembers())
                {
                    child.Accept(this);
                }
            }

            public override void VisitNamedType(INamedTypeSymbol symbol)
            {
                foreach (var child in symbol.GetMembers())
                {
                    child.Accept(this);
                }
            }

            public override void VisitMethod(IMethodSymbol symbol)
            {
                var cls = symbol.ReceiverType;
                var isClass = symbol.ReceiverType != null && symbol.ReceiverType.TypeKind == TypeKind.Class;
                var isPublic = symbol.ReceiverType != null && string.Equals(symbol.ReceiverType.DeclaredAccessibility.ToString().ToLowerInvariant(), "public", StringComparison.InvariantCultureIgnoreCase);
                if (isClass && isPublic && symbol.IsStatic && symbol.MethodKind == MethodKind.Ordinary)
                {
                    var className = cls.Name;
                    var classNameWithNs = cls.ToDisplayString();
                    if (classNameWithNs != _typeName) return;

                    var wrapperClassName = !className.Contains('<') ? className + "Wrapper" : className.Replace("<", "Wrapper<");
                    var classTypeParameters = ((INamedTypeSymbol)cls).GetTypeParameters();
                    var wrapperInterfaceName = $"I{wrapperClassName}{classTypeParameters}";
                    var constraintClauses = ((INamedTypeSymbol)cls).GetConstraintClauses();
                    var baseList = ((INamedTypeSymbol)cls).GetBaseList(wrapperInterfaceName);
                    var returnKeyword = symbol.ReturnsVoid ? "" : "return ";
                    var methodSignature = symbol.GetSignatureText();
                    var callableMethodSignature = symbol.GetCallableSignatureText();
                    var obsoleteAttribute = symbol.GetAttributes().FirstOrDefault(x => x.ToString().StartsWith("System.ObsoleteAttribute("))?.ToString();

                    var interfaceSource = $"\tpublic interface I{wrapperClassName}{classTypeParameters} {constraintClauses} {{";
                    var classSource = $"\tpublic class {wrapperClassName}{classTypeParameters} {baseList} {constraintClauses} {{";


                    if (!_interfaces.Contains(interfaceSource))
                        _interfaces.Add(interfaceSource);

                    if (!_classes.Contains(classSource))
                        _classes.Add(classSource);

                    if (!_interfaces.Contains(methodSignature))
                    {
                        _interfaces.Add($"\t\t{methodSignature};");
                    }

                    if (!_classes.Contains(methodSignature))
                    {
                        var obs = !string.IsNullOrEmpty(obsoleteAttribute) ? $"\t\t[{obsoleteAttribute}]" : "\t\t";
                        _classes.Add($"{obs} public {methodSignature} {{ {returnKeyword}{classNameWithNs}.{callableMethodSignature}; }}");
                    }
                }
            }
        }

        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource(nameof(Constants.MockableStaticAttribute), SourceText.From(Constants.MockableStaticAttribute, Encoding.UTF8));

            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
                return;

            CSharpParseOptions options = (context.Compilation as CSharpCompilation)?.SyntaxTrees[0].Options as CSharpParseOptions;
            Compilation compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(Constants.MockableStaticAttribute, Encoding.UTF8), options));
            INamedTypeSymbol attributeSymbol = compilation.GetTypeByMetadataName($"System.{nameof(Constants.MockableStaticAttribute)}");

            var sources = new HashSet<string>();
            sources.Clear();
            var assemblyName = "";
            foreach (var cls in receiver.Classes)
            {
                if (_interfaces.Count > 0)
                {
                    _interfaces.Add($"\t}}");
                }
                if (_classes.Count > 0)
                {
                    _classes.Add($"\t}}");
                }


                SemanticModel model = compilation.GetSemanticModel(cls.SyntaxTree);
                var clsSymbol = model.GetDeclaredSymbol(cls);

                var attr = clsSymbol.GetAttributes().FirstOrDefault(ad => ad.AttributeClass != null && ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));

                if (attr == null) continue;
                var isParameterlessCtor = attr?.ConstructorArguments.Length == 0;

                var sbInterface = new StringBuilder();
                var sbClass = new StringBuilder();

                if (isParameterlessCtor)
                {
                    var methods = cls.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(x => x.IsPublic() && x.IsStatic()).ToList();
                    if (methods.Count == 0) continue;

                    var className = clsSymbol.Name;
                    var ns = string.IsNullOrEmpty(cls.GetNamespace()) ? "" : cls.GetNamespace() + ".";
                    var baseList = string.IsNullOrEmpty(cls.BaseList?.ToFullString()) ? ":" : cls.BaseList?.ToFullString().Trim() + ",";
                    assemblyName = clsSymbol.ContainingAssembly.Identity.Name;
                    var wrapperClassName = !className.Contains('<') ? className + "Wrapper" : className.Replace("<", "Wrapper<");
                    var classTypeParameters = cls.GetTypeParameters() ?? "";
                    var constraintClauses = cls.GetConstraintClauses() ?? "";
                    sbInterface.AppendLine($"\tpublic interface I{wrapperClassName}{classTypeParameters} {constraintClauses} {{");
                    sbClass.AppendLine($"\tpublic class {wrapperClassName}{classTypeParameters} {baseList} I{wrapperClassName}{classTypeParameters} {constraintClauses} {{");

                    foreach (MethodDeclarationSyntax method in methods)
                    {
                        var text = method.GetSignatureText();

                        if (!sbInterface.ToString().Contains(text))
                            sbInterface.AppendLine($"\t\t{text};");

                        if (!sbClass.ToString().Contains(text))
                        {
                            var returnKeyword = method.ReturnsVoid() ? "" : "return ";
                            var isObsolete = method.TryGetObsoleteAttribute(out var obsoleteAttrText);
                            if (isObsolete)
                                sbClass.AppendLine($"\t\t{obsoleteAttrText}");

                            sbClass.AppendLine($"\t\tpublic {method.GetSignatureText()} {{");
                            sbClass.AppendLine($"\t\t\t{returnKeyword}{ns}{className}{classTypeParameters}.{method.GetCallableSignatureText()};");
                            sbClass.AppendLine($"\t\t}}");
                        }
                    }

                    sbInterface.AppendLine($"\t}}");
                    sbClass.AppendLine($"\t}}");
                }
                else
                {
                    var ctor = (INamedTypeSymbol)attr?.ConstructorArguments[0].Value;
                    if (ctor != null)
                    {
                        var assemblySymbol = ctor.ContainingAssembly.GlobalNamespace;
                        assemblyName = ctor.ContainingAssembly.Identity.Name;
                        var visitor = new MethodSymbolVisitor(ctor.ToDisplayString());
                        visitor.Visit(assemblySymbol);
                    }

                    sbInterface.AppendLine(_interfaces.Distinct().Aggregate((a, b) => a + Environment.NewLine + b) + Environment.NewLine + "\t}");
                    sbClass.AppendLine(_classes.Distinct().Aggregate((a, b) => a + Environment.NewLine + b) + Environment.NewLine + "\t}");
                }

                var interfaceWrapper = sbInterface.ToString();
                var classWrapper = sbClass.ToString();

                sources.Add(interfaceWrapper);
                sources.Add(classWrapper);
            }

            if (_interfaces[_interfaces.Count - 1].Trim() != "}")
            {
                _interfaces.Add($"\t}}");
            }
            if (_classes[_classes.Count - 1].Trim() != "}")
            {
                _classes.Add($"\t}}");
            }

            var defaultUsings = new StringBuilder();
            defaultUsings.AppendLine("using System;");
            defaultUsings.AppendLine("using System.Collections.Generic;");
            defaultUsings.AppendLine("using System.Linq;");
            defaultUsings.AppendLine("using System.Text;");
            defaultUsings.AppendLine("using System.Threading.Tasks;");
            var usings = defaultUsings.ToString();

            var src = sources.TakeLast(2).Aggregate((x, y) => x + Environment.NewLine + y).ToString();
            var @namespace = new StringBuilder();
            @namespace.AppendLine(usings);
            @namespace.AppendLine($"namespace MockableGenerated {{");
            @namespace.AppendLine(src);
            @namespace.Append("}");
            var result = @namespace.ToString();
            context.AddSource($"MockableGenerated", SourceText.From(result, Encoding.UTF8));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            //System.Diagnostics.Debugger.Launch();
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }


    }
}
