using System.Text.Json.Serialization;
using redmuffin.Blazor.StaticWeb.Common.Models;

namespace redmuffin.Blazor.StaticWeb.Services;

[JsonSerializable(typeof(BatchImageRequest))]
[JsonSerializable(typeof(BatchImageResponse))]
[JsonSerializable(typeof(ArticleImageRequest))]
[JsonSerializable(typeof(ArticleImageResponse))]
[JsonSerializable(typeof(CachedImageData))]
[JsonSerializable(typeof(ImageValidationResult))]
[JsonSerializable(typeof(List<ArticleImageRequest>))]
[JsonSerializable(typeof(List<ArticleImageResponse>))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
public partial class OpenGraphJsonSerializerContext : JsonSerializerContext
{
}
