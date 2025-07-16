using Microsoft.Azure.Functions.Worker;

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Helpers;

public class TestBindingContext : BindingContext
{
    public override IReadOnlyDictionary<string, object?> BindingData =>
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
}