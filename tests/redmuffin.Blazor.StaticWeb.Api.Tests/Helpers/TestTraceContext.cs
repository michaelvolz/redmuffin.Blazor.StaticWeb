using Microsoft.Azure.Functions.Worker;

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Helpers;

public class TestTraceContext : TraceContext
{
    public override string TraceParent => null!;
    public override string TraceState => null!;
}