using Dto.Event;
using FluentValidation;
using System.Text.Json;

namespace Web.Validators;

public class RegisterEventDtoValidator : AbstractValidator<RegisterEventDto>
{
    public RegisterEventDtoValidator()
    {
        RuleFor(x => x.SurveyJsData)
            .Must(BeValidJson)
            .When(x => !string.IsNullOrEmpty(x.SurveyJsData))
            .WithMessage("Les données du formulaire doivent être au format JSON valide")
            .MaximumLength(50000)
            .When(x => !string.IsNullOrEmpty(x.SurveyJsData))
            .WithMessage("Les données du formulaire ne peuvent pas dépasser 50 000 caractères");
    }

    private bool BeValidJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return true;

        try
        {
            using var doc = JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
