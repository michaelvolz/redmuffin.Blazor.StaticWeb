using System.Net;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NSubstitute;
using redmuffin.Blazor.StaticWeb.Api.Core;
using redmuffin.Blazor.StaticWeb.Api.Functions;
using redmuffin.Blazor.StaticWeb.Api.Tests.Helpers;

#pragma warning disable MA0004
#pragma warning disable CA1707, VSTHRD200

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Functions;

public class RaindropListVideosTests : TestBase
{
	[Test]
	public async Task Run_ReturnsOkResponse_WhenApiCallSucceeds()
	{
		var testToken = Configuration["Values:RainDropTestToken"];
		if (string.IsNullOrWhiteSpace(testToken)) Assert.Fail("RainDropTestToken is null or whitespace.");

		var settings = Options.Create(new Settings { RainDropTestToken = testToken });
		var logger = Substitute.For<ILogger<RaindropListVideos>>();
		var function = new RaindropListVideos(logger, settings);

		var request = JsonConvert.SerializeObject("dummy-text");
		var body = new MemoryStream(Encoding.ASCII.GetBytes(request));
		var context = Substitute.For<FunctionContext>();
		var requestData = new FakeHttpRequestData(
			context,
			new Uri("http://localhost:7044/SubscribeFunc"),
			body);

		// TODO: Find a way to mock the WriteAsJsonAsync method in HttpResponseData, or use a different approach to handle the response serialization.

		var result = await function.Run(requestData);

		result.Body.Position = 0; // Reset the position to the beginning of the stream
		using var reader = new StreamReader(result.Body);
		var responseBody = await reader.ReadToEndAsync();

		await Assert.That(result.StatusCode).IsEqualTo(HttpStatusCode.OK);
		await Assert.That(responseBody.Length > 0).IsTrue();
	}
}