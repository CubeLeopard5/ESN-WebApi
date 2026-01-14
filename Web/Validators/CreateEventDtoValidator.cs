using Dto.Event;
using FluentValidation;
using System.Text.Json;

namespace Web.Validators;

public class CreateEventDtoValidator : AbstractValidator<CreateEventDto>
{
    public CreateEventDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Le titre est requis")
            .MaximumLength(255)
            .WithMessage("Le titre ne peut pas dépasser 255 caractères");

        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage("La date de début est requise");

        RuleFor(x => x.EndDate)
            .Must((dto, endDate) => !endDate.HasValue || endDate.Value >= dto.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("La date de fin doit être postérieure ou égale à la date de début");

        RuleFor(x => x.MaxParticipants)
            .GreaterThan(0)
            .When(x => x.MaxParticipants.HasValue)
            .WithMessage("Le nombre maximum de participants doit être supérieur à 0");

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
