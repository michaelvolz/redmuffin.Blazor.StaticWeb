using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

#pragma warning disable CA1816

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Helpers;

public class TestHttpResponseData(FunctionContext functionContext) : HttpResponseData(functionContext), IDisposable, IAsyncDisposable
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

	public async ValueTask DisposeAsync()
	{
		await _bodyStream.DisposeAsync().ConfigureAwait(false);
	}

	public void Dispose()
	{
		_bodyStream.Dispose();
	}

	public string GetBodyAsString()
	{
		_bodyStream.Position = 0;
		using var reader = new StreamReader(_bodyStream);
		return reader.ReadToEnd();
	}
}