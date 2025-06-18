using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using redmuffin.Blazor.StaticWeb.Api.Core;
using redmuffin.Blazor.StaticWeb.Api.Functions;
using redmuffin.Blazor.StaticWeb.Api.Tests.Helpers;

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Functions;

public class RaindropListVideos_Tests : TestBase
{
	[Test]
	public async Task Run_ReturnsOkWithJsonResponse_Async()
	{
		// Arrange
		var logger = NullLogger<RaindropListVideos>.Instance;

		var testToken = Configuration["Values:RainDropTestToken"];
		if (string.IsNullOrWhiteSpace(testToken)) Assert.Fail("RainDropTestToken is null or whitespace.");

		var settings = Options.Create(new Settings { RainDropTestToken = testToken });

		var function = new RaindropListVideos(logger, settings);
		var functionContext = new TestFunctionContext();
		var request = new TestHttpRequestData(functionContext);

		TestHttpResponseData? response = null;
		try
		{
			// Act
			response = (TestHttpResponseData)await function.Run(request).ConfigureAwait(false);

			// Assert
			await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
			var responseBody = response.GetBodyAsString();
			JsonDocument.Parse(responseBody); // Verify response is valid JSON

			await Assert.That(responseBody).Contains("youtube");
		}
		finally
		{
			// Cleanup
			if (response is IAsyncDisposable asyncDisposableResponse)
			{
				await asyncDisposableResponse.DisposeAsync().ConfigureAwait(false);
			}
		}
	}
}