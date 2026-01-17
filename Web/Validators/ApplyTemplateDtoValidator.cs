using Dto.EventTemplate;
using FluentValidation;
using System.Text.Json;

namespace Web.Validators;

/// <summary>
/// Validator for ApplyTemplateDto
/// </summary>
public class ApplyTemplateDtoValidator : AbstractValidator<ApplyTemplateDto>
{
    public ApplyTemplateDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Le titre est requis")
            .MaximumLength(255)
            .WithMessage("Le titre ne peut pas dépasser 255 caractères");

        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage("La date de début est requise")
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .WithMessage("La date de début ne peut pas être dans le passé");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("La date de fin doit être postérieure à la date de début");

        RuleFor(x => x.MaxParticipants)
            .GreaterThan(0)
            .When(x => x.MaxParticipants.HasValue)
            .WithMessage("Le nombre maximum de participants doit être supérieur à 0");

        RuleFor(x => x.SurveyJsData)
            .Must(BeValidJson)
            .When(x => !string.IsNullOrWhiteSpace(x.SurveyJsData))
            .WithMessage("Les données du formulaire doivent être au format JSON valide")
            .MaximumLength(50000)
            .When(x => !string.IsNullOrWhiteSpace(x.SurveyJsData))
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
