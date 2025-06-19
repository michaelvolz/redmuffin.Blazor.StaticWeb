using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Api.Functions;

public partial class HelloWorld(ILogger<HelloWorld> logger)
{
	[LoggerMessage(1, LogLevel.Information, "C# HTTP trigger function processed a request. {Class}!", EventName = "Log_TriggerProcessed")]
	public static partial void Log_TriggerProcessed(ILogger logger, string @class);

	[Function("HelloWorld")]
	public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest request)
	{
		Log_TriggerProcessed(logger, nameof(HelloWorld));
		return new OkObjectResult("Welcome to Azure Functions!");
	}
}