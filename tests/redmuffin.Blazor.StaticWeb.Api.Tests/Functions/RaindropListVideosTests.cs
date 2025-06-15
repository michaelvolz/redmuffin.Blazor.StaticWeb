using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using redmuffin.Blazor.StaticWeb.Api.Core;
using redmuffin.Blazor.StaticWeb.Api.Functions;

#pragma warning disable CA1707, VSTHRD200

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Functions;

public class RaindropListVideosTests
{
	[Test]
	public async Task Run_ReturnsOkResponse_WhenApiCallSucceeds()
	{
		// Arrange
		var logger = Substitute.For<ILogger<RaindropListVideos>>();
		var settings = Options.Create(new Settings { RainDropTestToken = "dummy-token" });
		var function = new RaindropListVideos(logger, settings);
		var context = Substitute.For<FunctionContext>();
		var req = Substitute.For<HttpRequestData>(context);
		var response = Substitute.For<HttpResponseData>(context);
		req.CreateResponse(HttpStatusCode.OK).Returns(response);

		// Act
		var result = await function.Run(req).ConfigureAwait(false);

		// Assert
		await Assert.That(result.Body.Length > 0).IsTrue();
	}
}