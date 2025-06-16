using Microsoft.Extensions.Configuration;

public class TestBase
{
    protected IConfiguration Configuration { get; private set; }

    public TestBase()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory) // Points to bin/debug/net8.0
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .Build();
    }
}