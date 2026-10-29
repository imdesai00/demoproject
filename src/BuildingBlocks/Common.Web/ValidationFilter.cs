using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Common.Web;

public class ValidationErrorResponse
{
    public string Message { get; set; } = "One or more validation errors occurred.";
    public IDictionary<string, string[]> Errors { get; set; } = new Dictionary<string, string[]>();
}

/// <summary>
/// Runs any registered FluentValidation validator against the action's bound arguments before
/// the controller method executes. ASP.NET Core doesn't do this automatically for FluentValidation
/// (only for DataAnnotations), so every write endpoint gets real server-side validation this way.
/// </summary>
public class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext);

            if (!result.IsValid)
            {
                var errors = result.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                context.Result = new BadRequestObjectResult(new ValidationErrorResponse { Errors = errors });
                return;
            }
        }

        await next();
    }
}
