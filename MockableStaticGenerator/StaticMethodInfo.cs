using System.Collections.Generic;

namespace MockableStaticGenerator
{
    public class StaticMethodInfo
    {
        public string Name { get; set; }
        public string ClassName { get; set; }
        public string ClassNameWithNamespace { get; set; }
        public string ReturnType { get; set; }
        public IEnumerable<StaticMethodParameterInfo> Parameters { get; set; }
        public IEnumerable<StaticMethodTypeParameterInfo> TypeParameters { get; set; }
        public bool IsAsync { get; set; }
        public bool IsAbstract { get; set; }
        public bool IsGenericMethod { get; set; }
        public bool IsExtensionMethod { get; set; }
        public bool IsReturnsVoid { get; set; }
        public string ObsoleteInfo { get; set; }

        public StaticMethodInfo()
        {
            Parameters = new List<StaticMethodParameterInfo>();
            TypeParameters = new List<StaticMethodTypeParameterInfo>();
        }
    }
}
