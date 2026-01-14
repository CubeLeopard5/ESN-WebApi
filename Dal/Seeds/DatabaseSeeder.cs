using Bo.Constants;
using Bo.Enums;
using Bo.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dal.Seeds;

/// <summary>
/// Service pour initialiser les données de base au démarrage de l'application
/// </summary>
public class DatabaseSeeder
{
    private readonly EsnDevContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(EsnDevContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seed les rôles et le premier utilisateur admin si nécessaire
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            // Vérifier si les rôles existent déjà
            var rolesExist = await _context.Roles.AnyAsync();

            if (!rolesExist)
            {
                _logger.LogInformation("Seeding roles...");

                var roles = new List<RoleBo>
                {
                    new RoleBo
                    {
                        Name = UserRole.Admin,
                        CanCreateEvents = true,
                        CanModifyEvents = true,
                        CanDeleteEvents = true,
                        CanCreateUsers = true,
                        CanModifyUsers = true,
                        CanDeleteUsers = true
                    },
                    new RoleBo
                    {
                        Name = UserRole.User,
                        CanCreateEvents = false,
                        CanModifyEvents = false,
                        CanDeleteEvents = false,
                        CanCreateUsers = false,
                        CanModifyUsers = false,
                        CanDeleteUsers = false
                    },
                    new RoleBo
                    {
                        Name = UserRole.Moderator,
                        CanCreateEvents = true,
                        CanModifyEvents = true,
                        CanDeleteEvents = false,
                        CanCreateUsers = false,
                        CanModifyUsers = true,
                        CanDeleteUsers = false
                    }
                };

                await _context.Roles.AddRangeAsync(roles);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Roles seeded successfully");
            }
            else
            {
                _logger.LogInformation("Roles already exist, skipping seed");
            }

            // Vérifier si l'admin par défaut existe déjà
            var adminExists = await _context.Users
                .AnyAsync(u => u.Email == "admin@esn.ch");

            if (!adminExists)
            {
                _logger.LogInformation("Seeding default admin user...");

                // Récupérer le rôle Admin
                var adminRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name == UserRole.Admin);

                if (adminRole == null)
                {
                    _logger.LogError("Admin role not found, cannot create admin user");
                    return;
                }

                // Hash du mot de passe "Admin123!"
                var hasher = new PasswordHasher<UserBo>();
                var passwordHash = hasher.HashPassword(null!, "Admin123!");

                var adminUser = new UserBo
                {
                    Email = "admin@esn.ch",
                    FirstName = "Admin",
                    LastName = "ESN",
                    BirthDate = new DateTime(1990, 1, 1),
                    PhoneNumber = "+41 00 000 00 00",
                    StudentType = Bo.Constants.StudentType.EsnMember,
                    RoleId = adminRole.Id,
                    Status = UserStatus.Approved,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow,
                    PasswordHash = passwordHash
                };

                await _context.Users.AddAsync(adminUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Default admin user created successfully with email: admin@esn.ch");
                _logger.LogWarning("IMPORTANT: Change the default password 'Admin123!' immediately after first login!");
            }
            else
            {
                _logger.LogInformation("Default admin user already exists, skipping seed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while seeding database");
            throw;
        }
    }
}
