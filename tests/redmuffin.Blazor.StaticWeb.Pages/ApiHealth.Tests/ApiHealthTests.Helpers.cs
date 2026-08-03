using Bunit;
using Mediator;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using redmuffin.Blazor.StaticWeb.Common;
using redmuffin.Blazor.StaticWeb.Modules.AzureHealthCheck.Contracts;

namespace redmuffin.Blazor.StaticWeb.Pages.ApiHealth.Tests;

[Category("Feature:ApiHealth")]
public sealed partial class ApiHealthTests
{
    private static TestScope CreateTestScope(string response = "Mock response")
    {
        return new TestScope(response).WithStandardServices();
    }

    private static TestScope CreateFailingTestScope()
    {
        return new TestScope("ignored").WithFailingMediator();
    }

    public sealed class TestScope(string response) : IDisposable
    {
        public BunitContext BUnitContext { get; } = new();
        public NavigationManager_Mock NavigationManager { get; } = new("http://localhost:5000/");
        public IMediator_Mock Mediator { get; } = new(response);

        public TestScope WithStandardServices()
        {
            BUnitContext.Services.AddSingleton<NavigationManager>(NavigationManager);
            BUnitContext.Services.AddSingleton<IMediator>(Mediator);
            BUnitContext.JSInterop.Mode = JSRuntimeMode.Loose;
            return this;
        }

        public TestScope WithFailingMediator()
        {
            var failingMediator = new IMediator_FailingMock();
            BUnitContext.Services.AddSingleton<NavigationManager>(NavigationManager);
            BUnitContext.Services.AddSingleton<IMediator>(failingMediator);
            BUnitContext.JSInterop.Mode = JSRuntimeMode.Loose;
            return this;
        }

        public void Dispose()
        {
            BUnitContext?.Dispose();
        }
    }

    public sealed class NavigationManager_Mock : NavigationManager
    {
        public NavigationManager_Mock(string baseUri)
        {
            Initialize(baseUri, baseUri);
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
        }
    }

    public sealed class IMediator_Mock : IMediator
    {
        private readonly string _response;
        public int SendCount { get; private set; }

        public IMediator_Mock(string response)
        {
            _response = response;
        }

        public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            SendCount++;
            if (request is GetHelloQuery)
            {
                return new ValueTask<TResponse>((TResponse)(object)Result.Success(new HelloResponse(_response)));
            }

            throw new InvalidOperationException($"Unexpected request type: {request.GetType().Name}");
        }

        public ValueTask<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public ValueTask<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
        {
            SendCount++;
            return new ValueTask<TResponse>((TResponse)(object)Result.Success(new HelloResponse(_response)));
        }

        public ValueTask<object?> Send(object message, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamCommand<TResponse> command, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamQuery<TResponse> query, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object message, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public ValueTask Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
            => throw new NotSupportedException();

        public ValueTask Publish(object notification, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    public sealed class IMediator_FailingMock : IMediator
    {
        public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            => new((TResponse)(object)Result.Failure<HelloResponse>("Simulated mediator error"));

        public ValueTask<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public ValueTask<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
            => new((TResponse)(object)Result.Failure<HelloResponse>("Simulated mediator error"));

        public ValueTask<object?> Send(object message, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamCommand<TResponse> command, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamQuery<TResponse> query, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object message, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public ValueTask Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
            => throw new NotSupportedException();

        public ValueTask Publish(object notification, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
