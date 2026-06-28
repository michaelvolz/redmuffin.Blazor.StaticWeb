// Slopwatch instance-level suppression attribute.
// Defined locally per slopwatch convention — the global tool discovers
// this attribute by name. No NuGet dependency required.

namespace Slopwatch;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
internal sealed class SlopwatchSuppressAttribute : Attribute
{
    public SlopwatchSuppressAttribute(string ruleId, string justification)
    {
    }
}
