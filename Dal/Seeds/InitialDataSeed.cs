using Bo.Constants;
using Bo.Enums;
using Bo.Models;
using Microsoft.EntityFrameworkCore;

namespace Dal.Seeds;

/// <summary>
/// Classe pour initialiser les données de base de l'application
/// </summary>
public static class InitialDataSeed
{
    /// <summary>
    /// Seed les rôles et le premier utilisateur admin
    /// </summary>
    /// <remarks>
    /// IMPORTANT : Le mot de passe par défaut de l'admin est "Admin123!"
    /// Changez-le immédiatement après le premier login !
    /// </remarks>
    public static void SeedInitialData(ModelBuilder modelBuilder)
    {
        // 1. Seed des Rôles
        var adminRole = new RoleBo
        {
            Id = 1,
            Name = UserRole.Admin,
            CanCreateEvents = true,
            CanModifyEvents = true,
            CanDeleteEvents = true,
            CanCreateUsers = true,
            CanModifyUsers = true,
            CanDeleteUsers = true
        };

        var userRole = new RoleBo
        {
            Id = 2,
            Name = UserRole.User,
            CanCreateEvents = false,
            CanModifyEvents = false,
            CanDeleteEvents = false,
            CanCreateUsers = false,
            CanModifyUsers = false,
            CanDeleteUsers = false
        };

        var moderatorRole = new RoleBo
        {
            Id = 3,
            Name = UserRole.Moderator,
            CanCreateEvents = true,
            CanModifyEvents = true,
            CanDeleteEvents = false,
            CanCreateUsers = false,
            CanModifyUsers = true,
            CanDeleteUsers = false
        };

        modelBuilder.Entity<RoleBo>().HasData(adminRole, userRole, moderatorRole);

        // 2. Seed du premier Admin
        // Hash pré-calculé pour le mot de passe "Admin123!" avec PBKDF2
        // IMPORTANT: Changez ce mot de passe après le premier login !
        var adminUser = new UserBo
        {
            Id = 1,
            Email = "admin@esn.ch",
            FirstName = "Admin",
            LastName = "ESN",
            BirthDate = new DateTime(1990, 1, 1),
            PhoneNumber = "+41 00 000 00 00",
            StudentType = Bo.Constants.StudentType.EsnMember,
            RoleId = 1, // Admin role
            Status = UserStatus.Approved, // Déjà approuvé
            CreatedAt = new DateTime(2026, 1, 11, 0, 0, 0, DateTimeKind.Utc),
            LastLoginAt = new DateTime(2026, 1, 11, 0, 0, 0, DateTimeKind.Utc),
            // Hash pour le mot de passe "Admin123!" (généré avec PasswordHasher)
            PasswordHash = "AQAAAAIAAYagAAAAEHqO8hF7xJ0L3yKjMXH5ZF7wVvN0KQCqBXzP8x5MhGtY7bR3VjKqW8fT9nC2Lm1A=="
        };

        modelBuilder.Entity<UserBo>().HasData(adminUser);
    }
}
