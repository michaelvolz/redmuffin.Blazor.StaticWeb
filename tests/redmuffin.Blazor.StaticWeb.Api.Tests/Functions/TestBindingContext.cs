using Microsoft.Azure.Functions.Worker;

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Functions;

public class TestBindingContext : BindingContext
{
	public override IReadOnlyDictionary<string, object?> BindingData => new Dictionary<string, object>()!;
}