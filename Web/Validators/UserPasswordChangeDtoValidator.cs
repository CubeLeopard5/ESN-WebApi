using Dto.User;
using FluentValidation;

namespace Web.Validators;

public class UserPasswordChangeDtoValidator : AbstractValidator<UserPasswordChangeDto>
{
    public UserPasswordChangeDtoValidator()
    {
        RuleFor(x => x.OldPassword)
            .NotEmpty()
            .WithMessage("L'ancien mot de passe est requis")
            .MaximumLength(255)
            .WithMessage("L'ancien mot de passe ne peut pas dépasser 255 caractères");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("Le nouveau mot de passe est requis")
            .MinimumLength(8)
            .WithMessage("Le nouveau mot de passe doit contenir au moins 8 caractères")
            .Matches(@"[A-Z]")
            .WithMessage("Le nouveau mot de passe doit contenir au moins une lettre majuscule")
            .Matches(@"[a-z]")
            .WithMessage("Le nouveau mot de passe doit contenir au moins une lettre minuscule")
            .Matches(@"[0-9]")
            .WithMessage("Le nouveau mot de passe doit contenir au moins un chiffre")
            .Matches(@"[\W_]")
            .WithMessage("Le nouveau mot de passe doit contenir au moins un caractère spécial")
            .MaximumLength(255)
            .WithMessage("Le nouveau mot de passe ne peut pas dépasser 255 caractères")
            .NotEqual(x => x.OldPassword)
            .WithMessage("Le nouveau mot de passe doit être différent de l'ancien");
    }
}
