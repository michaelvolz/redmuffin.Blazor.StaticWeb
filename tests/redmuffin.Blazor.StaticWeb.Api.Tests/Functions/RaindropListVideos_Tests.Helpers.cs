using System.Collections.Immutable;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Functions;

public sealed partial class RaindropListVideos_Tests
{
    /// <summary>
    ///     Creates a new test scope for dependency management.
    /// </summary>
    private static TestScope CreateTestScope()
    {
        return new TestScope();
    }

    /// <summary>
    ///     Test scope for RaindropListVideos function tests.
    ///     Provides Azure Functions testing infrastructure including FunctionContext and HttpRequestData creation.
    /// </summary>
    public sealed class TestScope : IDisposable
    {
        public TestScope()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true)
                .AddJsonFile("local.settings.json", true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        /// <summary>
        ///     Creates a mock function context for Azure Functions testing.
        /// </summary>
        public static MockFunctionContext CreateFunctionContext(string functionName)
        {
            return new MockFunctionContext(functionName);
        }

        /// <summary>
        ///     Creates a mock HTTP request data for Azure Functions testing.
        /// </summary>
        public static MockHttpRequestData CreateHttpRequestData(MockFunctionContext functionContext)
        {
            return new MockHttpRequestData(functionContext);
        }

        public void Dispose()
        {
            // No resources to dispose in this implementation
        }
    }

    /// <summary>
    ///     Mock implementation of FunctionContext for Azure Functions testing.
    /// </summary>
    public sealed class MockFunctionContext : FunctionContext
    {
        public MockFunctionContext(string functionId)
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

        public override IServiceProvider InstanceServices { get; set; }

        public override FunctionDefinition FunctionDefinition => new MockFunctionDefinition(FunctionId);
        public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();
        public override IInvocationFeatures Features { get; } = null!;
        public override string InvocationId => Guid.NewGuid().ToString();
        public override string FunctionId { get; }
        public override TraceContext TraceContext => new MockTraceContext();
        public override BindingContext BindingContext => new MockBindingContext();
        public override RetryContext RetryContext => null!;

        private static ObjectSerializer CheckObjectSerializer(IServiceProvider instanceServices)
        {
            return instanceServices.GetService<IOptions<WorkerOptions>>()?.Value?.Serializer
                   ?? throw new InvalidOperationException("A serializer is not configured for the worker.");
        }
    }

    /// <summary>
    ///     Mock implementation of HttpRequestData for Azure Functions testing.
    /// </summary>
    public sealed class MockHttpRequestData(FunctionContext functionContext) : HttpRequestData(functionContext)
    {
        public override IEnumerable<ClaimsIdentity> Identities { get; } = [];
        public override string Method => HttpMethod.Get.ToString();
        public override Uri Url => new("http://localhost");
        public override Stream Body => Stream.Null;
        public override IReadOnlyCollection<IHttpCookie> Cookies => [];
        public override HttpHeadersCollection Headers => [];

        public override HttpResponseData CreateResponse()
        {
            return new MockHttpResponseData(FunctionContext);
        }
    }

    /// <summary>
    ///     Mock implementation of HttpResponseData for Azure Functions testing.
    /// </summary>
    public sealed class MockHttpResponseData(FunctionContext functionContext) : HttpResponseData(functionContext), IDisposable, IAsyncDisposable
    {
        private readonly MemoryStream _bodyStream = new();

        public override HttpStatusCode StatusCode { get; set; }
        public override HttpHeadersCollection Headers { get; set; } = [];

        public override Stream Body
        {
            get => _bodyStream;
            set => throw new NotSupportedException();
        }

        public override HttpCookies Cookies { get; } = null!;

        public string GetBodyAsString()
        {
            _bodyStream.Position = 0;
            using var reader = new StreamReader(_bodyStream);
            return reader.ReadToEnd();
        }

        public async ValueTask DisposeAsync()
        {
            await _bodyStream.DisposeAsync().ConfigureAwait(false);
        }

        public void Dispose()
        {
            _bodyStream.Dispose();
        }
    }

    /// <summary>
    ///     Mock implementation of FunctionDefinition for Azure Functions testing.
    /// </summary>
    public sealed class MockFunctionDefinition(string functionId) : FunctionDefinition
    {
        public override ImmutableArray<FunctionParameter> Parameters { get; } = [];
        public override string PathToAssembly => string.Empty;
        public override string EntryPoint => "RaindropListVideos";
        public override string Id => functionId;
        public override string Name => functionId;
        public override IImmutableDictionary<string, BindingMetadata> InputBindings { get; } = ImmutableDictionary<string, BindingMetadata>.Empty;
        public override IImmutableDictionary<string, BindingMetadata> OutputBindings { get; } = ImmutableDictionary<string, BindingMetadata>.Empty;
    }

    /// <summary>
    ///     Mock implementation of TraceContext for Azure Functions testing.
    /// </summary>
    public sealed class MockTraceContext : TraceContext
    {
        public override string TraceParent => string.Empty;
        public override string TraceState => string.Empty;
    }

    /// <summary>
    ///     Mock implementation of BindingContext for Azure Functions testing.
    /// </summary>
    public sealed class MockBindingContext : BindingContext
    {
        public override IReadOnlyDictionary<string, object?> BindingData { get; } = new Dictionary<string, object?>();
    }
}