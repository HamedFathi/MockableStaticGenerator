using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeGen
{

    public class StaticMethodTypeParameterInfo
    {
        public string Name { get; set; }
        public IEnumerable<string> Constraints { get; set; }
        public StaticMethodTypeParameterInfo()
        {
            Constraints = new List<string>();
        }
    }
}
