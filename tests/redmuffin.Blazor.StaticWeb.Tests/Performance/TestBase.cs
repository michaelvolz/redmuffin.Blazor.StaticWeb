using Microsoft.Extensions.Configuration;

namespace redmuffin.Blazor.StaticWeb.Tests.Performance;

public class TestBase
{
    protected IConfiguration Configuration { get; } = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .Build();
}
