namespace MockableStaticGenerator
{
    internal static class Constants
    {
        internal const string MockableStaticAttribute = @"
            namespace System
            {
                [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
                public sealed class MockableStaticAttribute : Attribute
                {
                    public MockableStaticAttribute(Type type)
                    {
                    }
                    public MockableStaticAttribute()
                    {
                    }
                }
            }";
    }
}
