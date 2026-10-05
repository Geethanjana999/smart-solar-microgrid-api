/*
 * File        : AuthService.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Validates credentials and issues signed JWT tokens.
 */
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.Data;
using SmartSolarMicrogrid.Api.DTOs;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Services.Interfaces;

namespace SmartSolarMicrogrid.Api.Services;

public sealed class AuthService(MongoContext db, IOptions<JwtOptions> jwt, ILogger<AuthService> logger) : IAuthService
{
    private const string InvalidCredentials = "Invalid username or password.";

    // Verifies email/NIC + password against the Users collection and returns a JWT carrying role and NIC claims.
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var identifier = request.Identifier;
        if (identifier.Length == 0 || string.IsNullOrEmpty(request.Password))
        {
            throw new InvalidOperationException(InvalidCredentials);
        }

        // Emails are stored lower-case; usernames (NICs) are stored exactly as registered,
        // so try the original, upper-case and lower-case forms.
        var email = identifier.ToLowerInvariant();
        var upper = identifier.ToUpperInvariant();
        var user = await db.Users
            .Find(x => x.Email == email
                       || x.Username == identifier
                       || x.Username == upper
                       || x.Username == email)
            .FirstOrDefaultAsync();

        if (user is null)
        {
            // Server log only - the client still gets the generic message.
            logger.LogWarning("Login failed: no user found for identifier '{Identifier}'.", identifier);
            throw new InvalidOperationException(InvalidCredentials);
        }

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            logger.LogWarning("Login failed: user '{Username}' has no PasswordHash stored.", user.Username);
            throw new InvalidOperationException(InvalidCredentials);
        }

        PasswordVerificationResult verification;
        try
        {
            verification = new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password);
        }
        catch (FormatException)
        {
            // Happens when the stored value is plain text or a non-Identity hash (e.g. seeded data).
            logger.LogWarning("Login failed: PasswordHash for '{Username}' is not a valid ASP.NET Identity hash.", user.Username);
            throw new InvalidOperationException(InvalidCredentials);
        }

        if (verification == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("Login failed: wrong password for '{Username}'.", user.Username);
            throw new InvalidOperationException(InvalidCredentials);
        }

        // Only reached with a correct password, so this does not reveal which usernames exist.
        if (!user.IsActive)
        {
            throw new InvalidOperationException(await InactiveMessageAsync(user));
        }

        var options = jwt.Value;
        var expires = DateTime.UtcNow.AddMinutes(options.ExpiryMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email ?? ""),
            new(ClaimTypes.Role, user.Role)
        };

        if (!string.IsNullOrWhiteSpace(user.ProsumerNic))
        {
            claims.Add(new Claim("nic", user.ProsumerNic));
        }

        var token = new JwtSecurityToken(
            options.Issuer,
            options.Audience,
            claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
                SecurityAlgorithms.HmacSha256));

        var userResponse = new UserResponse(user.Username, user.Email ?? "", user.Role, user.ProsumerNic);
        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expires, userResponse);
    }

    // Builds a helpful message depending on why the prosumer account cannot log in.
    private async Task<string> InactiveMessageAsync(User user)
    {
        if (!string.IsNullOrWhiteSpace(user.ProsumerNic))
        {
            var prosumer = await db.Prosumers.Find(x => x.Nic == user.ProsumerNic).FirstOrDefaultAsync();
            if (prosumer is { PendingActivation: true })
            {
                return "Your registration is awaiting Backoffice activation.";
            }

            if (prosumer is not null)
            {
                return "Your account has been deactivated. Please contact Backoffice.";
            }
        }

        return "Account is not active. Please contact Backoffice.";
    }
}