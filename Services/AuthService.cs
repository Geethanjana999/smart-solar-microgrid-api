/*
 * File        : AuthService.cs
 * Description : Validates credentials and issues signed JWT tokens.
 */
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.Data;
using SmartSolarMicrogrid.Api.DTOs;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Services.Interfaces;

namespace SmartSolarMicrogrid.Api.Services;

public sealed class AuthService(MongoContext db, IOptions<JwtOptions> jwt) : IAuthService
{
    // Verifies email/password against the Users collection and returns a JWT carrying role and NIC claims.
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var identifier = request.Username.Trim().ToLowerInvariant();
        var user = await db.Users.Find(x => x.Email == identifier || x.Username == identifier).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Invalid username or password.");

        var passwordOk = new PasswordHasher<User>()
            .VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Success;
        if (!passwordOk)
        {
            throw new InvalidOperationException("Invalid username or password.");
        }

        // Only reached with a correct password, so this does not reveal which usernames exist.
        if (!user.IsActive)
        {
            throw new InvalidOperationException("Account is not active. It may be awaiting Backoffice activation or has been deactivated.");
        }

        var options = jwt.Value;
        var expires = DateTime.UtcNow.AddMinutes(options.ExpiryMinutes);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("nic", user.ProsumerNic ?? "")
        };

        var token = new JwtSecurityToken(
            options.Issuer,
            options.Audience,
            claims,
            expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
                SecurityAlgorithms.HmacSha256));

        var userResponse = new UserResponse(user.Username, user.Email, user.Role, user.ProsumerNic);
        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expires, userResponse);
    }
}
