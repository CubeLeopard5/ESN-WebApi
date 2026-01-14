using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bo.Enums;

namespace Bo.Models;

[Table("Users")]
public partial class UserBo
{
    [Key]
    public int Id { get; set; }

    [Required]
    [EmailAddress(ErrorMessage = "L'email n'est pas valide.")]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public DateTime BirthDate { get; set; }

    [Phone(ErrorMessage = "Le numéro de téléphone n'est pas valide.")]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [Column("ESNCardNumber")]
    [StringLength(50)]
    public string? EsnCardNumber { get; set; }

    [StringLength(255)]
    public string? UniversityName { get; set; }

    [Required]
    [RegularExpression("exchange|local|esn_member", ErrorMessage = "Le type d'étudiant doit être 'exchange', 'local' ou 'esn_member'.")]
    [StringLength(50)]
    public string StudentType { get; set; } = string.Empty;

    [StringLength(100)]
    public string? TransportPass { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Statut du compte utilisateur (Pending, Approved, Rejected)
    /// </summary>
    [Required]
    public UserStatus Status { get; set; } = UserStatus.Pending;

    public int? RoleId { get; set; }

    public RoleBo? Role { get; set; }

    // 🔗 Navigation property
    public virtual ICollection<EventRegistrationBo> EventRegistrations { get; set; } = [];
    public virtual ICollection<PropositionBo> Propositions { get; set; } = [];
    public virtual ICollection<EventBo> Events { get; set; } = new List<EventBo>();
}
