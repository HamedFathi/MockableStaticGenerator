using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeGen
{
    public class StaticMethodParameterInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string RefKind { get; set; }
    }
}
