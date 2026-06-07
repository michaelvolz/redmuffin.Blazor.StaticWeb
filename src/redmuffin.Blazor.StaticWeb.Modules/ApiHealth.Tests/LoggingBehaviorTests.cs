using Mediator;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Common.PipelineBehaviors;
using redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Tests;

[Category("Feature:ApiHealth")]
public sealed partial class LoggingBehaviorTests
{
    [Test]
    public async Task Logs_before_and_after_handler_execution()
    {
        var logger = new Logger_Spy<LoggingBehavior<GetHelloQuery, HelloResponse>>();
        var behavior = new LoggingBehavior<GetHelloQuery, HelloResponse>(logger);
        var query = new GetHelloQuery();
        var response = new HelloResponse("test");

        var result = await behavior.Handle(
            query,
            (_, _) => new ValueTask<HelloResponse>(response),
            CancellationToken.None).ConfigureAwait(false);

        await Assert.That(result.Message).IsEqualTo("test");
        await Assert.That(logger.LogEntries.Count).IsEqualTo(2);
        await Assert.That(logger.LogEntries[0].Message).IsEqualTo("Handling GetHelloQuery");
        await Assert.That(logger.LogEntries[1].Message).IsEqualTo("Handled GetHelloQuery");
    }
}
