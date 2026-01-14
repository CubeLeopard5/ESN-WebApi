using Dto.EventTemplate;
using FluentValidation;
using System.Text.Json;

namespace Web.Validators;

public class EventTemplateDtoValidator : AbstractValidator<EventTemplateDto>
{
    public EventTemplateDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Le titre est requis")
            .MaximumLength(255)
            .WithMessage("Le titre ne peut pas dépasser 255 caractères");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("La description est requise");

        RuleFor(x => x.SurveyJsData)
            .NotEmpty()
            .WithMessage("Les données du formulaire sont requises")
            .Must(BeValidJson)
            .WithMessage("Les données du formulaire doivent être au format JSON valide")
            .MaximumLength(50000)
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
