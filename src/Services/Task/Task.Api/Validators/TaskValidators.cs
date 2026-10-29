using FluentValidation;
using Task.Api.Controllers;
using Task.Api.Models.DTOs;

namespace Task.Api.Validators;

public class CreateTaskRequestBodyValidator : AbstractValidator<CreateTaskRequestBody>
{
    public CreateTaskRequestBodyValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Priority).Must(p => p is "Low" or "Medium" or "High").WithMessage("Priority must be Low, Medium or High.");
    }
}

public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Priority).Must(p => p is "Low" or "Medium" or "High").WithMessage("Priority must be Low, Medium or High.");
    }
}

public class UpdateTaskStatusRequestValidator : AbstractValidator<UpdateTaskStatusRequest>
{
    public UpdateTaskStatusRequestValidator()
    {
        RuleFor(x => x.Status).Must(s => s is "Todo" or "InProgress" or "Done").WithMessage("Status must be Todo, InProgress or Done.");
    }
}
