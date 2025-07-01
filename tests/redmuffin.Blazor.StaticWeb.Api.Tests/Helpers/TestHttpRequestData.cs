using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Helpers;

public class TestHttpRequestData(FunctionContext functionContext) : HttpRequestData(functionContext)
{
	public override IEnumerable<ClaimsIdentity> Identities { get; } = null!;
	public override string Method => HttpMethod.Get.ToString();
	public override Uri Url => new("http://localhost");
	public override Stream Body => Stream.Null;
	public override IReadOnlyCollection<IHttpCookie> Cookies => [];
	public override HttpHeadersCollection Headers => [];

	public override HttpResponseData CreateResponse()
	{
		return new TestHttpResponseData(FunctionContext);
	}
}

public class TestHttpRequestDataWithBody(FunctionContext functionContext, Stream body) : HttpRequestData(functionContext)
{
	public override IEnumerable<ClaimsIdentity> Identities { get; } = null!;
	public override string Method => HttpMethod.Post.ToString();
	public override Uri Url => new("http://localhost");
	public override Stream Body => body;
	public override IReadOnlyCollection<IHttpCookie> Cookies => [];
	public override HttpHeadersCollection Headers => [];

	public override HttpResponseData CreateResponse()
	{
		return new TestHttpResponseData(FunctionContext);
	}
}
