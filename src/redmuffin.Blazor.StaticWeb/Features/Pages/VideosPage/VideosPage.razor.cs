using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.VideosPage;

public partial class VideosPage
{
	private static readonly string? RainDropClientId = "684ea82bb3333b01de5487c1";
	private string? _videoData;
	private string? _errorMessage;

	private void LoginWithRaindrop()
	{
		var redirectPath = "/redirect";
		var baseUri = Navigation.BaseUri.TrimEnd('/');
		var redirectUri = $"{baseUri}{redirectPath}";
		var authUrl =
			$"https://raindrop.io/oauth/authorize?client_id=684ea82bb3333b01de5487c1&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code";

		JS.InvokeVoidAsync("open", authUrl, "_self");
	}

	private async Task FetchVideos()
	{
		_errorMessage = null;
		_videoData = null;
		try
		{
			var response = await Http.GetAsync("/api/RaindropListVideos");
			if (response.IsSuccessStatusCode)
			{
				_videoData = await response.Content.ReadAsStringAsync();
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

	[Inject]
	private IJSRuntime JS { get; set; } = default!;

	[Inject]
	private NavigationManager Navigation { get; set; } = default!;
}