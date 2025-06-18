using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using redmuffin.Blazor.StaticWeb.Api.Core;
using redmuffin.Blazor.StaticWeb.Api.Functions;
using redmuffin.Blazor.StaticWeb.Api.Tests.Helpers;
using Assembly = System.Reflection.Assembly;

// ReSharper disable All
// ReSharper disable InconsistentNaming
#pragma warning disable MA0002
#pragma warning disable MA0056
#pragma warning disable MA0048
#pragma warning disable CA1707
#pragma warning disable MA0004
#pragma warning disable VSTHRD200

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Functions;

public class RaindropListVideosTests_Grok : TestBase
{
	[Test]
	public async Task Run_ReturnsOkWithJsonResponse()
	{
		// Arrange
		var logger = NullLogger<RaindropListVideos>.Instance;

		var testToken = Configuration["Values:RainDropTestToken"];
		if (string.IsNullOrWhiteSpace(testToken)) Assert.Fail("RainDropTestToken is null or whitespace.");

		var settings = Options.Create(new Settings { RainDropTestToken = testToken });

		var function = new RaindropListVideos(logger, settings);
		var functionContext = new TestFunctionContext();
		var request = new TestHttpRequestData(functionContext);

		// Act
		var response = await function.Run(request);

		// Assert
		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
		var responseBody = ((TestHttpResponseData)response).GetBodyAsString();
		JsonDocument.Parse(responseBody); // Verify response is valid JSON
		await Assert.That(responseBody).Contains("youtube");
	}
}

public class TestHttpRequestData(FunctionContext functionContext) : HttpRequestData(functionContext)
{
	public override IEnumerable<ClaimsIdentity> Identities { get; } = null!;
	public override string Method => HttpMethod.Get.ToString();
	public override Uri Url => new("http://localhost");
	public override Stream Body => Stream.Null;
	public override IReadOnlyCollection<IHttpCookie> Cookies => [];
	public override HttpHeadersCollection Headers => new();

	public override HttpResponseData CreateResponse()
	{
		return new TestHttpResponseData(FunctionContext);
	}
}

public class TestHttpResponseData(FunctionContext functionContext) : HttpResponseData(functionContext)
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
}

public class TestFunctionContext : FunctionContext
{
	public TestFunctionContext()
	{
		var jsonSerializerOptions = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = true,
		};

		var workerOptions = new WorkerOptions
		{
			Serializer = new JsonObjectSerializer(),
		};

		var serviceCollection = new ServiceCollection()
			.AddSingleton<IOptions<WorkerOptions>>(new OptionsWrapper<WorkerOptions>(workerOptions))
			.AddSingleton(jsonSerializerOptions);

		serviceCollection.AddFunctionsWorkerDefaults();

		InstanceServices = serviceCollection.BuildServiceProvider();

		var serializer = GetObjectSerializer(InstanceServices);

		Debug.Assert(serializer is not null, "Serializer not found!");
	}

	public override FunctionDefinition FunctionDefinition => new TestFunctionDefinition();
	public override IDictionary<object, object> Items { get; set; } = null!;
	public override IInvocationFeatures Features { get; } = null!;
	public override string InvocationId => Guid.NewGuid().ToString();
	public override string FunctionId => "RaindropListVideos";
	public override TraceContext TraceContext => new TestTraceContext();
	public override BindingContext BindingContext => new TestBindingContext();
	public override RetryContext RetryContext => null!;
	public override IServiceProvider InstanceServices { get; set; }

	private static ObjectSerializer GetObjectSerializer(IServiceProvider instanceServices)
	{
		return instanceServices.GetService<IOptions<WorkerOptions>>()?.Value?.Serializer
		       ?? throw new InvalidOperationException("A serializer is not configured for the worker.");
	}
}

public class TestFunctionDefinition : FunctionDefinition
{
	public override IImmutableDictionary<string, BindingMetadata> InputBindings => ImmutableDictionary<string, BindingMetadata>.Empty;
	public override IImmutableDictionary<string, BindingMetadata> OutputBindings => ImmutableDictionary<string, BindingMetadata>.Empty;
	public override ImmutableArray<FunctionParameter> Parameters { get; }
	public override string PathToAssembly => Assembly.GetExecutingAssembly().Location;
	public override string EntryPoint => typeof(RaindropListVideos).FullName!;
	public override string Id => "RaindropListVideos";
	public override string Name => "RaindropListVideos";
}

public class TestTraceContext : TraceContext
{
	public override string TraceParent => null!;
	public override string TraceState => null!;
}

public class TestBindingContext : BindingContext
{
	public override IReadOnlyDictionary<string, object?> BindingData => new Dictionary<string, object>()!;
}