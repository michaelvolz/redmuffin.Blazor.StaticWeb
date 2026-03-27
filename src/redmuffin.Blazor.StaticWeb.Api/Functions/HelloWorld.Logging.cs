using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Api.Functions;

public sealed partial class HelloWorld
{
    [LoggerMessage(1, LogLevel.Information, "C# HTTP trigger function processed a request. {Class}!", EventName = "Log_TriggerProcessed")]
    public static partial void Log_TriggerProcessed(ILogger logger, string @class);
}
