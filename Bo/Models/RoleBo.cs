namespace Bo.Models;
public partial class RoleBo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public bool CanCreateEvents { get; set; }
    public bool CanModifyEvents { get; set; }
    public bool CanDeleteEvents { get; set; }

    public bool CanCreateUsers { get; set; }
    public bool CanModifyUsers { get; set; }
    public bool CanDeleteUsers { get; set; }

    public ICollection<UserBo> Users { get; set; } = [];
}
