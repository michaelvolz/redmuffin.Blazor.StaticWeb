// Slopwatch instance-level suppression attribute.
// Owned by Common; the global tool discovers this attribute by name.
// No NuGet dependency required.

namespace Slopwatch;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class SlopwatchSuppressAttribute : Attribute
{
    public SlopwatchSuppressAttribute(string ruleId, string justification)
    {
    }
}
