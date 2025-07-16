using System.Text.Json;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Helpers;

public class TestFunctionContext : FunctionContext
{
    public TestFunctionContext(string functionId)
    {
        FunctionId = functionId;

        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var workerOptions = new WorkerOptions { Serializer = new JsonObjectSerializer() };

        var serviceCollection = new ServiceCollection()
            .AddSingleton<IOptions<WorkerOptions>>(new OptionsWrapper<WorkerOptions>(workerOptions))
            .AddSingleton(jsonSerializerOptions)
            .AddHttpClient();

        serviceCollection.AddFunctionsWorkerDefaults();

        InstanceServices = serviceCollection.BuildServiceProvider();

        CheckObjectSerializer(InstanceServices);
    }

    public sealed override IServiceProvider InstanceServices { get; set; }

    public override FunctionDefinition FunctionDefinition => new TestFunctionDefinition(FunctionId);
    public override IDictionary<object, object> Items { get; set; } = null!;
    public override IInvocationFeatures Features { get; } = null!;
    public override string InvocationId => Guid.NewGuid().ToString();
    public override string FunctionId { get; }
    public override TraceContext TraceContext => new TestTraceContext();
    public override BindingContext BindingContext => new TestBindingContext();
    public override RetryContext RetryContext => null!;

    private static ObjectSerializer CheckObjectSerializer(IServiceProvider instanceServices)
    {
        return instanceServices.GetService<IOptions<WorkerOptions>>()?.Value?.Serializer
               ?? throw new InvalidOperationException("A serializer is not configured for the worker.");
    }
}