using System.Collections.Generic;

namespace MockableStaticGenerator
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
