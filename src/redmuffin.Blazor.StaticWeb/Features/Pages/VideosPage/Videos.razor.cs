using System.Text.Json;
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

				try
				{
					// Use JsonTypeInfo for deserialization to avoid trimming issues
					_videoItems = JsonSerializer.Deserialize(json, RaindropJsonSerializerContext.Default.RaindropItemList);
				}
				catch (JsonException jsonEx)
				{
					Console.WriteLine("JSON Deserialization Error: " + jsonEx.Message);
					Console.WriteLine("Path: " + jsonEx.Path);
					Console.WriteLine("LineNumber: " + jsonEx.LineNumber);
					Console.WriteLine("BytePositionInLine: " + jsonEx.BytePositionInLine);
					_errorMessage = "Error deserializing JSON: " + jsonEx.Message;
					return;
				}

				// Add checks for _videoItems
				if (_videoItems == null)
				{
					_errorMessage = "Deserialization resulted in null.";
					Console.WriteLine(_errorMessage);
				}
				else if (_videoItems.Count == 0)
				{
					_errorMessage = "Deserialization resulted in an empty list.";
					Console.WriteLine(_errorMessage);
				}
				else
				{
					Console.WriteLine($"Deserialization successful. {_videoItems.Count} items loaded.");

					// Probe deeper into the data
					foreach (var item in _videoItems)
					{
						Console.WriteLine($"Item ID: {item.Id}, Title: {item.Title}, Link: {item.Link}");
						Console.WriteLine($"Tags: {string.Join(", ", item.Tags ?? new List<string>())}");
						Console.WriteLine($"Media Count: {item.Media?.Count ?? 0}");
						Console.WriteLine($"Highlights Count: {item.Highlights?.Count ?? 0}");

						// Validate individual properties
						foreach (var highlight in item.Highlights ?? new List<Highlight>())
						{
							if (highlight.CreatorRef == null)
							{
								Console.WriteLine("Warning: CreatorRef is null.");
							}
							else
							{
								Console.WriteLine($"CreatorRef Raw JSON: {JsonSerializer.Serialize(highlight.CreatorRef)}");
								Console.WriteLine($"CreatorRef ID: {highlight.CreatorRef.Id}, Name: {highlight.CreatorRef.Name}");
							}
						}
					}
				}
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