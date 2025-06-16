using System.Net;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Helpers;

public class CustomHttpRequestData(FunctionContext functionContext) : HttpRequestData(functionContext)
{
	public override Stream Body { get; } = new MemoryStream();

	public override HttpHeadersCollection Headers { get; } = new();

	public override IReadOnlyCollection<IHttpCookie> Cookies { get; } = new List<IHttpCookie>();

	public override Uri Url { get; } = new("http://localhost");

	public override IEnumerable<ClaimsIdentity> Identities { get; } = new List<ClaimsIdentity>();

	public override string Method { get; } = HttpMethod.Get.Method;

	public override HttpResponseData CreateResponse()
	{
		return new CustomHttpResponseData(FunctionContext, HttpStatusCode.OK);
	}

	public virtual HttpResponseData CreateResponse(HttpStatusCode statusCode)
	{
		return new CustomHttpResponseData(FunctionContext, statusCode);
	}
}

public class CustomHttpResponseData : HttpResponseData
{
	public CustomHttpResponseData(FunctionContext functionContext, HttpStatusCode statusCode)
		: base(functionContext)
	{
		StatusCode = statusCode;
	}

	public override HttpHeadersCollection Headers { get; set; } = new();

	public override Stream Body { get; set; } = new MemoryStream();

	public override HttpCookies Cookies { get; } = null!;

	public override HttpStatusCode StatusCode { get; set; }
}