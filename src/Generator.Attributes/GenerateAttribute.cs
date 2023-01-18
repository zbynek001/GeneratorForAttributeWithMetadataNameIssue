using System;

namespace Generator
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, Inherited = false)]
    public class GenerateAttribute : Attribute
    {
    }
}
