using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using redmuffin.Blazor.StaticWeb.Core;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.CounterPage;

public partial class Counter : ComponentBase
{
    private int _currentCount;

    private static void ThrowException()
    {
        throw new InvalidOperationException();
    }

    [UsedImplicitly]
    private static void ReverseStringExample()
    {
        const string original = "Blazor";
        var unused = original.ReverseString() ?? string.Empty; // Fix for CS8600: Ensure reversed is not null
    }

    private void IncrementCount()
    {
        _currentCount++;
    }
}