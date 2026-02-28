using EventFlow.CoreService.Application.DTOs;
using FluentValidation;

namespace EventFlow.CoreService.Application.Validators;

public class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Location).NotEmpty().MaximumLength(300);
        RuleFor(x => x.StartDate).GreaterThan(DateTime.UtcNow).WithMessage("Start date must be in the future.");
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).WithMessage("End date must be after start date.");
    }
}

public class UpdateEventRequestValidator : AbstractValidator<UpdateEventRequest>
{
    public UpdateEventRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Location).NotEmpty().MaximumLength(300);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).WithMessage("End date must be after start date.");
    }
}
