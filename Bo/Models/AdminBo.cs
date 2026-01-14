namespace Bo.Models;

public partial class AdminBo
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
