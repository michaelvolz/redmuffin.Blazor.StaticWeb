using Microsoft.AspNetCore.Components;

namespace redmuffin.Blazor.StaticWeb.Pages.Counter;

#pragma warning disable MA0049 // Type name matches namespace — standard Blazor component pattern
public partial class Counter : ComponentBase
#pragma warning restore MA0049
{
    private int _currentCount;

    private static void ThrowException()
    {
        throw new InvalidOperationException();
    }

    private void IncrementCount()
    {
        _currentCount++;
    }
}