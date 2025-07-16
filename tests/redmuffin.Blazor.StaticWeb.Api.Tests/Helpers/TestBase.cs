using Microsoft.Extensions.Configuration;

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Helpers;

public class TestBase
{
    protected IConfiguration Configuration { get; } = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory) // Points to bin/debug/net8.0
        .AddJsonFile("local.settings.json", true, true)
        .Build();
}