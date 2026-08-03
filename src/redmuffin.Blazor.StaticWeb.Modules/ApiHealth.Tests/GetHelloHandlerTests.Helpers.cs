using Microsoft.Extensions.DependencyInjection;
using redmuffin.Blazor.StaticWeb.Common;
using redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Tests;

[Category("Feature:ApiHealth")]
public sealed partial class GetHelloHandlerTests
{
    private static TestScope CreateScope(string response = "Hello!")
    {
        return new TestScope(response);
    }

    public sealed class TestScope : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly IHealthCheckService_Fake _healthCheckService;

        public TestScope(string response)
        {
            _healthCheckService = new IHealthCheckService_Fake(response);

            var services = new ServiceCollection();
            services.AddSingleton<IHealthCheckService>(_healthCheckService);
            services.AddSingleton<GetHelloHandler>();
            _serviceProvider = services.BuildServiceProvider();
        }

        public IHealthCheckService_Fake HealthCheckService => _healthCheckService;

        public GetHelloHandler Handler => _serviceProvider.GetRequiredService<GetHelloHandler>();

        public void Dispose() => _serviceProvider.Dispose();
    }

    public sealed class IHealthCheckService_Fake : IHealthCheckService
    {
        private readonly string _response;

        public IHealthCheckService_Fake(string response)
        {
            _response = response;
        }

        public string? FailureError { get; set; }

        public Task<Result<string>> GetHelloAsync(CancellationToken cancellationToken = default)
        {
            if (FailureError is not null)
            {
                return Task.FromResult(Result.Failure<string>(FailureError));
            }

            return Task.FromResult(Result.Success(_response));
        }
    }
}
