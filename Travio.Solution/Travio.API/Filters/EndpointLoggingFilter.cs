using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace Travio.API.Filters;

public class EndpointLoggingFilter : IAsyncActionFilter
{
    private readonly ILogger<EndpointLoggingFilter> _logger;

    public EndpointLoggingFilter(ILogger<EndpointLoggingFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var controllerName = context.RouteData.Values["controller"]?.ToString() ?? "UnknownController";
        var actionName = context.RouteData.Values["action"]?.ToString() ?? "UnknownAction";
        var userId = context.HttpContext.User.Identity?.Name ?? "Anonymous";

        // Log input arguments safely, filtering out sensitive parameter values
        var arguments = context.ActionArguments
            .Where(a => !a.Key.Contains("password", StringComparison.OrdinalIgnoreCase) && 
                        !a.Key.Contains("token", StringComparison.OrdinalIgnoreCase) &&
                        !a.Key.Contains("secret", StringComparison.OrdinalIgnoreCase) &&
                        !a.Key.Contains("key", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(k => k.Key, v => v.Value);

        _logger.LogInformation("Endpoint executing: {Controller}.{Action} (User: {UserId}) with args: {@Args}", 
            controllerName, actionName, userId, arguments);

        var stopwatch = Stopwatch.StartNew();
        var executedContext = await next();
        stopwatch.Stop();

        if (executedContext.Exception != null)
        {
            _logger.LogError(executedContext.Exception, 
                "Endpoint execution failed: {Controller}.{Action} (User: {UserId}) in {ElapsedMs}ms", 
                controllerName, actionName, userId, stopwatch.ElapsedMilliseconds);
        }
        else
        {
            var statusCode = executedContext.HttpContext.Response.StatusCode;
            _logger.LogInformation(
                "Endpoint executed successfully: {Controller}.{Action} (User: {UserId}) returning status {StatusCode} in {ElapsedMs}ms", 
                controllerName, actionName, userId, statusCode, stopwatch.ElapsedMilliseconds);
        }
    }
}
