using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Claims;

using JetBrains.Annotations;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

#pragma warning disable MA0048

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Helpers;

[ExcludeFromCodeCoverage]
[UsedImplicitly]
public class FakeHttpRequestData : HttpRequestData
{
	public FakeHttpRequestData(FunctionContext functionContext, Uri url, Stream body = null!)
		: base(functionContext)
	{
		Url = url;
		Body = body ?? new MemoryStream();
	}

	public override Stream Body { get; } = new MemoryStream();
	public override HttpHeadersCollection Headers { get; } = new();
	public override IReadOnlyCollection<IHttpCookie> Cookies { get; } = null!;
	public override Uri Url { get; }
	public override IEnumerable<ClaimsIdentity> Identities { get; } = null!;
	public override string Method { get; } = null!;

	public override HttpResponseData CreateResponse()
	{
		return new FakeHttpResponseData(FunctionContext);
	}
}

[ExcludeFromCodeCoverage]
public class FakeHttpResponseData(FunctionContext functionContext) : HttpResponseData(functionContext)
{
	public override HttpStatusCode StatusCode { get; set; }
	public override HttpHeadersCollection Headers { get; set; } = new();
	public override Stream Body { get; set; } = new MemoryStream();
	public override HttpCookies Cookies { get; } = null!;

	public ValueTask WriteAsJsonAsync<T>(HttpResponseData response, T instance, CancellationToken cancellationToken = default)
	{
		// Use System.Text.Json directly for testing
		var json = System.Text.Json.JsonSerializer.Serialize(response);
		var bytes = System.Text.Encoding.UTF8.GetBytes(json);
		Body.Write(bytes, 0, bytes.Length);
		Body.Seek(0, SeekOrigin.Begin);
		return ValueTask.CompletedTask;
	}
}