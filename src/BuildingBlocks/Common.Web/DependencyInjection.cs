using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Web;

public static class DependencyInjection
{
    /// <summary>
    /// Wires FluentValidation + the ValidationFilter into MVC for this service. Call from
    /// builder.Services.AddControllers(options => ...) chain: options.Filters.Add&lt;ValidationFilter&gt;();
    /// then services.AddValidatorsFromAssembly(serviceAssembly).
    /// </summary>
    public static IMvcBuilder AddCommonWeb(this IMvcBuilder builder, System.Reflection.Assembly validatorAssembly)
    {
        builder.Services.AddValidatorsFromAssembly(validatorAssembly);
        return builder;
    }
}
