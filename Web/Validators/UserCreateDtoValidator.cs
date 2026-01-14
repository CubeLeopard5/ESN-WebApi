using Dto.User;
using FluentValidation;

namespace Web.Validators;

public class UserCreateDtoValidator : AbstractValidator<UserCreateDto>
{
    public UserCreateDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("L'email est requis")
            .EmailAddress()
            .WithMessage("L'email n'est pas valide")
            .MaximumLength(255)
            .WithMessage("L'email ne peut pas dépasser 255 caractères");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Le mot de passe est requis")
            .MinimumLength(8)
            .WithMessage("Le mot de passe doit contenir au moins 8 caractères")
            .Matches(@"[A-Z]")
            .WithMessage("Le mot de passe doit contenir au moins une lettre majuscule")
            .Matches(@"[a-z]")
            .WithMessage("Le mot de passe doit contenir au moins une lettre minuscule")
            .Matches(@"[0-9]")
            .WithMessage("Le mot de passe doit contenir au moins un chiffre")
            .Matches(@"[\W_]")
            .WithMessage("Le mot de passe doit contenir au moins un caractère spécial");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("Le prénom est requis")
            .MaximumLength(100)
            .WithMessage("Le prénom ne peut pas dépasser 100 caractères");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Le nom est requis")
            .MaximumLength(100)
            .WithMessage("Le nom ne peut pas dépasser 100 caractères");

        RuleFor(x => x.BirthDate)
            .NotEmpty()
            .WithMessage("La date de naissance est requise")
            .Must(BeAValidAge)
            .WithMessage("Vous devez avoir au moins 13 ans");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20)
            .WithMessage("Le numéro de téléphone ne peut pas dépasser 20 caractères");

        RuleFor(x => x.StudentType)
            .NotEmpty()
            .WithMessage("Le type d'étudiant est requis")
            .Must(BeAValidStudentType)
            .WithMessage("Le type d'étudiant doit être 'exchange', 'local' ou 'esn_member'");
    }

    private bool BeAValidAge(DateTime birthDate)
    {
        var age = DateTime.Today.Year - birthDate.Year;
        if (birthDate > DateTime.Today.AddYears(-age))
            age--;

        return age >= 13;
    }

    private bool BeAValidStudentType(string studentType)
    {
        var validTypes = new[] { "exchange", "local", "esn_member" };
        return validTypes.Contains(studentType?.ToLowerInvariant());
    }
}
