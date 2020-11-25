using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MockableStaticGenerator
{
    [Generator]
    public class MockStaticGenerator : ISourceGenerator
    {
        private static readonly List<StaticMethodInfo> _methodInfos = new List<StaticMethodInfo>();
        public class MethodSymbolVisitor : SymbolVisitor
        {
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

            private string getRefKind(RefKind refKind)
            {
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

            public override void VisitMethod(IMethodSymbol symbol)
            {
                var cls = symbol.ReceiverType;
                var isClass = symbol.ReceiverType.TypeKind == TypeKind.Class;

                var isPublic = string.Equals(symbol.ReceiverType.DeclaredAccessibility.ToString(), "public", StringComparison.InvariantCultureIgnoreCase);

                if (isClass && isPublic && symbol.IsStatic && symbol.MethodKind == MethodKind.Ordinary)
                {
                    var className = cls.Name;
                    var classNameWithNs = cls.ToDisplayString();

                    var classTypeConstraints = new StringBuilder();
                    var typeArgs = ((INamedTypeSymbol)cls).TypeArguments;

                    if (typeArgs.Length != 0)
                    {
                        foreach (var ta in typeArgs)
                        {
                            // TODO: Class with constraints
                            // public class Sample<T,U,V> where U : class, new() {}
                            /*
                            if (((INamedTypeSymbol)ta.co != null)
                            {
                                constraints.AppendLine($"{tpc.Name}: {tpc.Constraints.Aggregate((a, b) => a + ", " + b)}");
                            }
                            */
                        }
                    }

                    var classTypeArgs = typeArgs.Length == 0 ? "" : "<" + ((INamedTypeSymbol)cls).TypeArguments.Select(x => x.Name).Aggregate((a, b) => a + ", " + b) + ">";

                    _methodInfos.Add(new StaticMethodInfo()
                    {
                        ClassName = className + classTypeArgs + (string.IsNullOrEmpty(classTypeConstraints.ToString()) ? "" : " where " + classTypeConstraints.ToString()).Trim(),
                        ClassNameWithNamespace = classNameWithNs,
                        IsAbstract = symbol.IsAbstract,
                        IsAsync = symbol.IsAsync,
                        IsExtensionMethod = symbol.IsExtensionMethod,
                        IsGenericMethod = symbol.IsGenericMethod,
                        IsReturnsVoid = symbol.ReturnsVoid,
                        Name = symbol.Name,
                        ReturnType = symbol.ReturnType.ToString(),
                        Parameters = symbol.Parameters.Length == 0 ? null : symbol.Parameters.Select(x => new StaticMethodParameterInfo() { Name = x.Name, Type = x.Type.ToString(), RefKind = getRefKind(x.RefKind) }).ToList(),
                        TypeParameters = symbol.TypeParameters.Length == 0 ? null : symbol.TypeParameters.Select(x => new StaticMethodTypeParameterInfo() { Name = x.Name, Constraints = x.ConstraintTypes.Length == 0 ? null : x.ConstraintTypes.Select(y => y.ToDisplayString()) }).ToList(),
                        ObsoleteInfo = symbol.GetAttributes().FirstOrDefault(x => x.ToString().StartsWith("System.ObsoleteAttribute("))?.ToString(),
                    });
                }
            }
        }

        private const string AttributeText = @"
        namespace System
        {
            [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
            public sealed class MockableStaticAttribute : Attribute
            {
                public MockableStaticAttribute(Type type)
                {
                }
            }
        }"
        ;

        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource("MockableStaticAttribute", SourceText.From(AttributeText, Encoding.UTF8));

            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                return;

            CSharpParseOptions options = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
            Compilation compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(AttributeText, Encoding.UTF8), options));

            INamedTypeSymbol attributeSymbol = compilation.GetTypeByMetadataName("System.MockableStaticAttribute");

            var visitor = new MethodSymbolVisitor();
            var assemblyName = "";
            foreach (var cls in receiver.CandidateClasses)
            {
                SemanticModel model = compilation.GetSemanticModel(cls.SyntaxTree);
                var clsSymbol = model.GetDeclaredSymbol(cls);
                var attr = clsSymbol.GetAttributes().FirstOrDefault(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));
                if (attr == null) return;
                assemblyName = ((INamedTypeSymbol)attr?.ConstructorArguments[0].Value).ContainingAssembly.Identity.Name;
                var assemblySymbol = ((INamedTypeSymbol)attr?.ConstructorArguments[0].Value).ContainingAssembly.GlobalNamespace;

                visitor.Visit(assemblySymbol);

                var sbInterface = new StringBuilder();
                var sbClassWapper = new StringBuilder();
                var sources = new List<string>();

                foreach (var item in _methodInfos.GroupBy(x => x.ClassName))
                {
                    sbInterface.Clear();
                    sbClassWapper.Clear();

                    var className = item.Key;

                    var fileName = !className.Contains('<') ? className + "Wrapper" : className.Replace("<", "Wrapper<");
                    sbInterface.AppendLine($"\tpublic interface I{fileName} {{");
                    sbClassWapper.AppendLine($"\tpublic partial class {fileName} : I{fileName} {{");

                    foreach (var m in item)
                    {
                        var tp = m.TypeParameters == null ? "" : "<" + m.TypeParameters.Select(x => x.Name).Aggregate((a, b) => a + ", " + b) + ">";
                        var p = m.Parameters == null ? "" : m.Parameters.Select(x => x.Type + " " + x.Name).Aggregate((a, b) => a + ", " + b);
                        var pv = m.Parameters == null ? "" : m.Parameters.Select(x => (x.RefKind + " " + x.Name).Trim()).Aggregate((a, b) => a + ", " + b);
                        var constraints = new StringBuilder();
                        if (m.TypeParameters != null)
                        {
                            foreach (var tpc in m.TypeParameters)
                            {
                                if (tpc.Constraints != null)
                                {
                                    constraints.AppendLine($"{tpc.Name}: {tpc.Constraints.Aggregate((a, b) => a + ", " + b)}");
                                }
                            }
                        }
                        var sig = $"{m.ReturnType} {m.Name}{tp}({p})";
                        var sigWithConstraint = (sig + (string.IsNullOrEmpty(constraints.ToString()) ? "" : " where " + constraints.ToString())).Trim();
                        var returnKeyword = m.IsReturnsVoid ? "" : "return ";

                        if (!sbInterface.ToString().Contains(sigWithConstraint))
                            sbInterface.AppendLine($"\t\t{sigWithConstraint};");

                        if (!sbClassWapper.ToString().Contains(sigWithConstraint))
                        {
                            if (!string.IsNullOrEmpty(m.ObsoleteInfo))
                            {
                                sbClassWapper.AppendLine($"\t\t[{m.ObsoleteInfo}]");
                            }
                            sbClassWapper.AppendLine($"\t\tpublic {sigWithConstraint} {{");
                            sbClassWapper.AppendLine($"\t\t\t{returnKeyword}{m.ClassNameWithNamespace}.{m.Name}{tp}({pv});");
                            sbClassWapper.AppendLine($"\t\t}}");
                        }
                    }

                    sbInterface.AppendLine($"\t}}");
                    sbClassWapper.AppendLine($"\t}}");
                    var interfaceWrapper = sbInterface.ToString();
                    var classWrapper = sbClassWapper.ToString();
                    sources.Add(interfaceWrapper);
                    sources.Add(classWrapper);
                }


                var ns = new StringBuilder();
                ns.AppendLine($"namespace {assemblyName}.MockableGenerated {{");
                ns.AppendLine(sources.Aggregate((a, b) => a + Environment.NewLine + b));
                ns.AppendLine("}");

                var src = ns.ToString();

                context.AddSource($"{assemblyName}MockableGenerated", SourceText.From(src, Encoding.UTF8));

            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // System.Diagnostics.Debugger.Launch();

            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }
    }
}
