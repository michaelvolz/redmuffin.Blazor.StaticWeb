using System.Net;
using System.Text;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Common.Abstractions;
using redmuffin.Blazor.StaticWeb.Core.Services;
using HomePage = redmuffin.Blazor.StaticWeb.Pages.Home.Home;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Home;

[Category("Feature:Home")]
public sealed partial class HomeTests
{
    [Test]
    public async Task Home_AdvancedErrorHandling_MixedFailureScenarios()
    {
        // Arrange
        using var scope = CreateFailingHttpTestScope();
        var delayProvider = new ControllableDelayProvider();
        scope.BUnitContext.Services.AddSingleton<IDelayProvider>(delayProvider);
        var component = scope.BUnitContext.Render<HomePage>();

        // Act - several API failures in sequence
        for (var i = 0; i < 2; i++)
        {
            var button = component.Find("button.primary-button");
            delayProvider.Arm();
            var clickTask = button.ClickAsync(new MouseEventArgs());
            await Assert
                .That(component.Find("#alert-region").TextContent)
                .Contains("API call failed");
            delayProvider.Release();
            await clickTask.ConfigureAwait(false);
        }

        // Assert - the button stays enabled for a retry
        await Assert
            .That(component.Find("button.primary-button").HasAttribute("disabled"))
            .IsFalse();
    }

    [Test]
    [Category("Smoke")]
    public async Task Home_AdvancedScenarios_AsyncExceptionPropagation_DoesNotCrashComponent()
    {
        // Arrange
        using var scope = CreateFailingHttpTestScope();
        var delayProvider = new ControllableDelayProvider();
        scope.BUnitContext.Services.AddSingleton<IDelayProvider>(delayProvider);
        var component = scope.BUnitContext.Render<HomePage>();

        // Act - first click hits the throwing API path
        delayProvider.Arm();
        var firstClick = component.Find("button.primary-button").ClickAsync(new MouseEventArgs());
        await Assert.That(component.Find("#alert-region").TextContent).Contains("API call failed");
        delayProvider.Release();
        await firstClick.ConfigureAwait(false);

        // A subsequent click still produces the alert update
        delayProvider.Arm();
        var secondClick = component.Find("button.primary-button").ClickAsync(new MouseEventArgs());
        await Assert.That(component.Find("#alert-region").TextContent).Contains("API call failed");
        delayProvider.Release();
        await secondClick.ConfigureAwait(false);

        // Assert - the component still renders and the exception was logged, not surfaced to the renderer
        using (Assert.Multiple())
        {
            await Assert.That(component.FindAll("main").Count).IsEqualTo(1);
            await Assert
                .That(
                    scope.Logger.LogEntries.Any(entry =>
                        entry.Level == LogLevel.Error
                        && entry.Message.Contains("Dummy API call failed")
                    )
                )
                .IsTrue();
        }
    }

    [Test]
    public async Task Home_Authorization_NullAuthenticationState_HandlesGracefully()
    {
        // Arrange
        using var scope = CreateTestScope();
        // Don't provide AuthenticationState cascading value (it will be null)

        // Act
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - Verify null authentication state is handled gracefully
        using (Assert.Multiple())
        {
            await Assert.That(component.Instance.IsAuthenticated).IsFalse();
            await Assert.That(component.Instance.CurrentUserName).IsNull();
            // Component should still render successfully
            await Assert.That(component.Markup).IsNotNull().And.Contains("redmuffin.StaticWeb");
        }
    }

    [Test]
    [Category("Smoke")]
    public async Task Home_ErrorRecovery_AfterHttpFailure_RendersSuccessWhenApiRecovers()
    {
        // Arrange
        using var scope = CreateFailingHttpTestScope();
        using var handler = new SwitchableHttpMessageHandler();
        scope.BUnitContext.Services.AddSingleton<IHttpClientFactory>(
            new HttpClientFactory_Stub(() => handler)
        );
        var delayProvider = new ControllableDelayProvider();
        scope.BUnitContext.Services.AddSingleton<IDelayProvider>(delayProvider);
        var component = scope.BUnitContext.Render<HomePage>();

        // Act - the first click fails
        delayProvider.Arm();
        var failingClick = component.Find("button.primary-button").ClickAsync(new MouseEventArgs());
        await Assert.That(component.Find("#alert-region").TextContent).Contains("API call failed");
        delayProvider.Release();
        await failingClick.ConfigureAwait(false);

        // The API recovers, then the next click succeeds
        handler.Fail = false;
        delayProvider.Arm();
        var recoveredClick = component
            .Find("button.primary-button")
            .ClickAsync(new MouseEventArgs());
        await Assert
            .That(component.Find("#status-region").TextContent)
            .Contains("API call completed");
        delayProvider.Release();
        await recoveredClick.ConfigureAwait(false);

        // Assert - the failure was logged once and the component stayed alive
        using (Assert.Multiple())
        {
            await Assert
                .That(
                    scope.Logger.LogEntries.Any(entry =>
                        entry.Level == LogLevel.Error
                        && entry.Message.Contains("Dummy API call failed")
                    )
                )
                .IsTrue();
            await Assert.That(component.FindAll("main").Count).IsEqualTo(1);
        }
    }

    [Test]
    public async Task Home_JSInterop_HandlesThrowingJSCalls_WithoutComponentFailure()
    {
        // Arrange
        using var scope = CreateTestScope().WithJSInterop();
        var jsRuntime = scope.BUnitContext.JSInterop.JSRuntime;
        var loader = new PageAssemblyLoader(
            new LazyAssemblyLoader(jsRuntime),
            jsRuntime,
            NullLogger<PageAssemblyLoader>.Instance
        );
        scope.BUnitContext.Services.AddSingleton<IPageAssemblyLoader>(loader);
        scope
            .BUnitContext.JSInterop.Setup<bool>("eval")
            .SetException(new JSException("JS unavailable"));

        // Act
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - the throwing JS probe is contained: the page renders and no fallback alert appears
        using (Assert.Multiple())
        {
            await Assert.That(component.FindAll("main").Count).IsEqualTo(1);
            await Assert.That(component.Find("#alert-region").TextContent.Trim()).IsEmpty();
            await Assert
                .That(
                    scope.BUnitContext.JSInterop.Invocations.Any(invocation =>
                        invocation.Identifier == "eval"
                    )
                )
                .IsTrue();
        }
    }

    [Test]
    public async Task Home_JSInterop_HandlesTimeoutScenarios_GracefulDegradation()
    {
        // Arrange
        using var scope = CreateTestScope().WithJSInterop();
        var jsRuntime = scope.BUnitContext.JSInterop.JSRuntime;
        scope.BUnitContext.Services.AddSingleton<IPageAssemblyLoader>(
            new PageAssemblyLoader(
                new LazyAssemblyLoader(jsRuntime),
                jsRuntime,
                NullLogger<PageAssemblyLoader>.Instance
            )
        );

        // The eval probe never completes, so the prefetch stays pending.
        scope.BUnitContext.JSInterop.Setup<bool>("eval");

        // Act
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - the hung JS call does not block rendering or interaction
        var button = component.Find("button.primary-button");
        await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        using (Assert.Multiple())
        {
            await Assert.That(component.FindAll("main").Count).IsEqualTo(1);
            await Assert.That(component.Find("#status-region").TextContent.Trim()).IsEmpty();
            await Assert
                .That(
                    scope.Logger.LogEntries.Any(entry => entry.Message.Contains("Button clicked"))
                )
                .IsTrue();
        }
    }

    private sealed class SwitchableHttpMessageHandler : HttpMessageHandler
    {
        public bool Fail { get; set; } = true;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            if (Fail)
                throw new HttpRequestException("Simulated network error");

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Mock response", Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(response);
        }
    }
}
