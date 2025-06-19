using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Api.Functions;

public class HelloWorld(ILogger<HelloWorld> logger)
{
	private static readonly Action<ILogger, string, Exception?> Log_TriggerProcessed =
		LoggerMessage.Define<string>(LogLevel.Information, new EventId(1, "Hello"), "C# HTTP trigger function processed a request. {Class}!");

	[Function("HelloWorld")]
	public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
	{
		Log_TriggerProcessed(logger, nameof(HelloWorld), null);
		return new OkObjectResult("Welcome to Azure Functions!");
	}
}