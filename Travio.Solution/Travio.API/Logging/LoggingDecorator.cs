using System.Diagnostics;
using System.Reflection;

namespace Travio.API.Logging;

public class LoggingDecorator<T> : DispatchProxy
{
    private T _target = default!;
    private ILogger _logger = default!;

    public static T Create(T target, ILogger logger)
    {
        var proxy = Create<T, LoggingDecorator<T>>();
        var decorator = (LoggingDecorator<T>)(object)proxy!;
        decorator._target = target;
        decorator._logger = logger;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod == null) return null;

        var methodName = targetMethod.Name;
        var className = typeof(T).Name;

        // Build a dictionary of arguments while redacting sensitive fields
        var parameters = targetMethod.GetParameters();
        var argumentsDict = new Dictionary<string, object?>();
        if (args != null && parameters.Length == args.Length)
        {
            for (int i = 0; i < args.Length; i++)
            {
                var paramName = parameters[i].Name ?? $"arg{i}";
                var value = args[i];

                if (paramName.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                    paramName.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                    paramName.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
                    paramName.Contains("key", StringComparison.OrdinalIgnoreCase))
                {
                    argumentsDict[paramName] = "[REDACTED]";
                }
                else
                {
                    argumentsDict[paramName] = value;
                }
            }
        }

        _logger.LogInformation("Calling service method {Service}.{Method} with args: {@Args}", 
            className, methodName, argumentsDict);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = targetMethod.Invoke(_target, args);

            if (result is Task task)
            {
                var returnType = targetMethod.ReturnType;
                if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var resultType = returnType.GetGenericArguments()[0];
                    var handleMethod = typeof(LoggingDecorator<T>)
                        .GetMethod(nameof(HandleAsyncGeneric), BindingFlags.NonPublic | BindingFlags.Static)
                        ?.MakeGenericMethod(resultType);

                    if (handleMethod != null)
                    {
                        return handleMethod.Invoke(null, new object[] { task, className, methodName, stopwatch, _logger });
                    }
                }
                
                return HandleAsyncVoid(task, className, methodName, stopwatch, _logger);
            }

            stopwatch.Stop();
            _logger.LogInformation("Service method {Service}.{Method} succeeded in {ElapsedMs}ms", 
                className, methodName, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (TargetInvocationException ex)
        {
            stopwatch.Stop();
            var innerEx = ex.InnerException ?? ex;
            _logger.LogError(innerEx, "Service method {Service}.{Method} failed after {ElapsedMs}ms with error: {Message}", 
                className, methodName, stopwatch.ElapsedMilliseconds, innerEx.Message);
            throw innerEx;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Service method {Service}.{Method} failed after {ElapsedMs}ms with error: {Message}", 
                className, methodName, stopwatch.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }

    private static async Task HandleAsyncVoid(Task task, string className, string methodName, Stopwatch stopwatch, ILogger logger)
    {
        try
        {
            await task.ConfigureAwait(false);
            stopwatch.Stop();
            logger.LogInformation("Service method {Service}.{Method} succeeded in {ElapsedMs}ms", 
                className, methodName, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Service method {Service}.{Method} failed after {ElapsedMs}ms with error: {Message}", 
                className, methodName, stopwatch.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }

    private static async Task<TResult> HandleAsyncGeneric<TResult>(Task<TResult> task, string className, string methodName, Stopwatch stopwatch, ILogger logger)
    {
        try
        {
            var result = await task.ConfigureAwait(false);
            stopwatch.Stop();
            logger.LogInformation("Service method {Service}.{Method} succeeded in {ElapsedMs}ms", 
                className, methodName, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Service method {Service}.{Method} failed after {ElapsedMs}ms with error: {Message}", 
                className, methodName, stopwatch.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }
}
