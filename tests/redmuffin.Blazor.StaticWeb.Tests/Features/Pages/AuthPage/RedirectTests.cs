using TUnit.Core;
using P = redmuffin.Blazor.StaticWeb.Features.Pages.AuthPage;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Pages.AuthPage;

public class RedirectTests
{
    [Test]
    [MethodDataSource(nameof(ParseAccessTokenTestData))]
    public async Task ParseAccessToken_ShouldExtractToken(
        P.Redirect.ApiExchangeResponse? response, bool expectedSuccess, string expectedToken, string? expectedError)
    {
        var result = P.Redirect.ParseAccessToken(response, out var token, out var error);

        await Assert.That(result).IsEqualTo(expectedSuccess);
        await Assert.That(token).IsEqualTo(expectedToken);
        await Assert.That(error).IsEqualTo(expectedError);
    }

    public static IEnumerable<Func<(P.Redirect.ApiExchangeResponse?, bool, string, string?)>> ParseAccessTokenTestData()
    {
        // Happy path: valid token
        yield return () => (new P.Redirect.ApiExchangeResponse { AccessToken = "abc123" }, true, "abc123", null);

        // Null response
        yield return () => (null, false, string.Empty, "Failed to retrieve access token from API: No token in response.");

        // Empty token
        yield return () => (new P.Redirect.ApiExchangeResponse { AccessToken = "" }, false, string.Empty, "Failed to retrieve access token from API: No token in response.");

        // Error from API
        yield return () => (new P.Redirect.ApiExchangeResponse { Error = "invalid_grant" }, false, string.Empty, "invalid_grant");

        // Null token, no error
        yield return () => (new P.Redirect.ApiExchangeResponse { AccessToken = null, Error = null }, false, string.Empty, "Failed to retrieve access token from API: No token in response.");
    }
}
