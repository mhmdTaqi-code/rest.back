using Microsoft.AspNetCore.Identity;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Infrastructure.Services;

public class PasswordHashService : IPasswordHashService
{
    private readonly PasswordHasher<object> _passwordHasher = new();
    private static readonly object PasswordOwner = new();

    public string HashPassword(string password)
    {
        return _passwordHasher.HashPassword(PasswordOwner, password);
    }

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword) || string.IsNullOrEmpty(providedPassword))
        {
            return false;
        }

        try
        {
            var result = _passwordHasher.VerifyHashedPassword(PasswordOwner, hashedPassword, providedPassword);
            return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
