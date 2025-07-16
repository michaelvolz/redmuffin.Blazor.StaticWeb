using System.Runtime.InteropServices;

namespace redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Core;

/// <summary>
///     Immutable size metrics record
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct SizeMetrics(
    double TransferSize,
    double EncodedSize,
    double DecodedSize,
    string TransferSizeFormatted,
    string EncodedSizeFormatted,
    string DecodedSizeFormatted)
{
    public double CompressionRatio => DecodedSize > 0 && EncodedSize > 0
        ? Math.Round((DecodedSize - EncodedSize) / DecodedSize * 100, 1)
        : 0;
}