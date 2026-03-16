using Bo.Models;
using Business.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Business.Auth;

/// <summary>
/// Service dédié à la génération des tokens JWT
/// </summary>
public class JwtTokenService(
    IConfiguration configuration,
    ILogger<JwtTokenService> logger)
    : IJwtTokenService
{
    /// <inheritdoc />
    public string GenerateToken(UserBo user)
    {
        logger.LogInformation("JwtTokenService.GenerateToken called for {Email}", user.Email);

        var jwtConfig = configuration.GetSection("Jwt");
        var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? jwtConfig["Key"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Email),
            new("userId", user.Id.ToString()),
            new("name", $"{user.FirstName} {user.LastName}"),
            new("studentType", user.StudentType)
        };

        if (user.Role != null)
        {
            claims.Add(new Claim(ClaimTypes.Role, user.Role.Name));

            // Add role permissions as claims
            claims.Add(new Claim("CanCreateEvents", user.Role.CanCreateEvents.ToString()));
            claims.Add(new Claim("CanModifyEvents", user.Role.CanModifyEvents.ToString()));
            claims.Add(new Claim("CanDeleteEvents", user.Role.CanDeleteEvents.ToString()));

            claims.Add(new Claim("CanCreateUsers", user.Role.CanCreateUsers.ToString()));
            claims.Add(new Claim("CanModifyUsers", user.Role.CanModifyUsers.ToString()));
            claims.Add(new Claim("CanDeleteUsers", user.Role.CanDeleteUsers.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: jwtConfig["Issuer"],
            audience: jwtConfig["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtConfig["ExpireMinutes"])),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
