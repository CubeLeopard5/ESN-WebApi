using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Controllers;
using Serilog;
using System.Text.Json;
using System.Reflection;

namespace Web.Middlewares;

public class RequestLoggingActionFilter : IAsyncActionFilter
{
    private static readonly string[] SensitiveProperties = new[]
    {
        "password",
        "newpassword",
        "oldpassword",
        "confirmpassword",
        "secret",
        "token",
        "apikey",
        "authorization"
    };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
        var controllerName = descriptor?.ControllerName ?? "UnknownController";
        var actionName = descriptor?.ActionName ?? "UnknownAction";

        var userEmail = context.HttpContext.User?.Identity?.Name ?? "Anonymous";

        // Gather all parameters with sensitive data filtering
        var parameters = context.ActionArguments.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value != null ? SanitizeAndSerialize(kvp.Value) : "null"
        );

        Log.Information("📥 {Controller}/{Action} called by {User} with parameters: {Parameters}",
            controllerName, actionName, userEmail, parameters);

        // Proceed to next middleware/action
        await next();

        Log.Information("📤 {Controller}/{Action} executed with status code {StatusCode}",
            controllerName, actionName, context.HttpContext.Response?.StatusCode);
    }

    private static string SanitizeAndSerialize(object obj)
    {
        // Clone the object and redact sensitive properties
        var sanitized = RedactSensitiveData(obj);
        return JsonSerializer.Serialize(sanitized, new JsonSerializerOptions { WriteIndented = true });
    }

    private static object RedactSensitiveData(object obj)
    {
        if (obj == null) return "null";

        var type = obj.GetType();

        // If it's a primitive type or string, return as is
        if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || type == typeof(Guid))
            return obj;

        // Create a dictionary to hold the sanitized data
        var sanitized = new Dictionary<string, object>();

        // Get all properties
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            var propName = prop.Name;
            var value = prop.GetValue(obj);

            // Check if this property is sensitive
            if (SensitiveProperties.Any(s => propName.Equals(s, StringComparison.OrdinalIgnoreCase)))
            {
                sanitized[propName] = "***REDACTED***";
            }
            else if (value != null)
            {
                sanitized[propName] = value;
            }
            else
            {
                sanitized[propName] = null!;
            }
        }

        return sanitized;
    }
}
