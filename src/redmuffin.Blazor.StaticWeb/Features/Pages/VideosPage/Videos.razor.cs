using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.VideosPage;

public partial class Videos
{
	private static readonly string? RainDropClientId = "684ea82bb3333b01de5487c1";
	private string? _errorMessage;
	private List<RaindropItem>? _videoItems;

	[Inject]
	private IJSRuntime Js { get; set; } = null!;

	[Inject]
	private NavigationManager Navigation { get; set; } = null!;

	private async Task LoginWithRaindropAsync()
	{
		var redirectPath = "/redirect";
		var baseUri = Navigation.BaseUri.TrimEnd('/');
		var redirectUri = $"{baseUri}{redirectPath}";
		var authUrl =
			$"https://raindrop.io/oauth/authorize?client_id={RainDropClientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code";

		await Js.InvokeVoidAsync("open", authUrl, "_self");
	}

	private async Task FetchVideosAsync()
	{
		_errorMessage = null;
		_videoItems = null;
		try
		{
			var response = await Http.GetAsync("/api/RaindropListVideos");
			if (response.IsSuccessStatusCode)
			{
				var json = await response.Content.ReadAsStringAsync();
				Console.WriteLine("Raw JSON Response: " + json); // Log the raw JSON response

				// Use JsonTypeInfo for deserialization to avoid trimming issues
				_videoItems = JsonSerializer.Deserialize(json, RaindropJsonSerializerContext.Default.ListRaindropItem);
			}
			else
			{
				_errorMessage = $"Error fetching videos: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
			}
		}
		catch (Exception ex)
		{
			_errorMessage = $"Exception fetching videos: {ex.Message}";
		}

		StateHasChanged();
	}
}