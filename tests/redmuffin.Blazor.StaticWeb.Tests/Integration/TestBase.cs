using Microsoft.Extensions.Configuration;

namespace redmuffin.Blazor.StaticWeb.Tests.Integration;

public class TestBase
{
    protected IConfiguration Configuration { get; } = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", true, true)
        .Build();
}