using System.Runtime.InteropServices;

namespace redmuffin.Blazor.StaticWeb.Models.PageLoadSpeed;

/// <summary>
///     Immutable calculated metrics record
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct CalculatedMetrics(
    double ServerResponseTime,
    double DomProcessingTime,
    double ResourceLoadTime);