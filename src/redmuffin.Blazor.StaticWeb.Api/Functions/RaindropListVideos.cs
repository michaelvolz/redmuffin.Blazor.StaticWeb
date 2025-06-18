using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using redmuffin.Blazor.StaticWeb.Api.Core;

#pragma warning disable CA1848, CA1001, MA0004

namespace redmuffin.Blazor.StaticWeb.Api.Functions;

public class RaindropListVideos(ILogger<RaindropListVideos> logger, IOptions<Settings> settings)
{
	private const string TargetCollectionId = "56109697";

	private readonly HttpClient _httpClient = new();
	private readonly Settings _settings = settings.Value;

	/// <summary>
	/// This method handles HTTP GET requests to fetch a list of videos from the Raindrop API.
	/// It uses an HttpClient to send a request to the API, retrieves the response, and processes the JSON data.
	/// If the request is successful, it returns the list of videos in the response.
	/// If the request fails or an error occurs, it returns an appropriate error message.
	/// Key steps:
	/// 1. Set up authorization headers using a token from the settings.
	/// 2. Send a GET request to the Raindrop API endpoint.
	/// 3. Parse the JSON response and extract video items.
	/// 4. Handle errors gracefully with proper logging and HTTP status codes.
	/// </summary>
	[Function("RaindropListVideos")]
	public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
	{
		logger.LogInformation("Videos function processed a request.");

		_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.RainDropTestToken);

		try
		{
			// API endpoint to get raindrops from a specific collection, sorted by creation date in descending order
			var apiUrl = $"https://api.raindrop.io/rest/v1/raindrops/{TargetCollectionId}?sort=-created";
			logger.LogInformation("Fetching videos from Raindrop API: {ApiUrl}", apiUrl);

			var response = await _httpClient.GetAsync(apiUrl);
			var json = await response.Content.ReadAsStringAsync();

			if (response.IsSuccessStatusCode)
			{
				logger.LogInformation("Successfully received response from Raindrop API.");
				// Assuming the response is a JSON array of items.
				// You might need to adjust the deserialization based on the actual Raindrop API response structure.
				using var jsonDoc = JsonDocument.Parse(json);
				var items = jsonDoc.RootElement.TryGetProperty("items", out var itemsElement) ? itemsElement.Clone() : jsonDoc.RootElement.Clone();

				var okResp = req.CreateResponse(HttpStatusCode.OK);
				await okResp.WriteAsJsonAsync(items);
				return okResp;
			}

			logger.LogWarning("Raindrop API request failed with status code: {StatusCode}. Response: {Response}", response.StatusCode, json);
			var errResp = req.CreateResponse(HttpStatusCode.BadRequest);
			await errResp.WriteAsJsonAsync(new { Error = $"Raindrop API request failed: {response.StatusCode}", Details = json });
			return errResp;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "An error occurred while fetching videos from Raindrop.");
			var errResp = req.CreateResponse(HttpStatusCode.InternalServerError);
			await errResp.WriteAsJsonAsync(new { Error = ex.Message });
			return errResp;
		}
	}
}