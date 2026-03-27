using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Api.Functions;

public sealed partial class HelloWorld(ILogger<HelloWorld> logger)
{
    [Function("HelloWorld")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest request)
    {
        Log_TriggerProcessed(logger, nameof(HelloWorld));
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}