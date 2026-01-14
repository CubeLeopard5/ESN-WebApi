using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Web.Attributes;

/// <summary>
/// Attribut de validation pour les ID de route
/// Valide que l'ID est strictement positif (> 0)
/// </summary>
public class ValidateIdAttribute : ActionFilterAttribute
{
    private readonly string _parameterName;

    public ValidateIdAttribute(string parameterName = "id")
    {
        _parameterName = parameterName;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ActionArguments.TryGetValue(_parameterName, out var value))
        {
            if (value is int id && id <= 0)
            {
                context.Result = new BadRequestObjectResult(new
                {
                    error = $"Invalid {_parameterName}",
                    message = $"The {_parameterName} must be a positive integer greater than zero."
                });
                return;
            }
        }

        base.OnActionExecuting(context);
    }
}
