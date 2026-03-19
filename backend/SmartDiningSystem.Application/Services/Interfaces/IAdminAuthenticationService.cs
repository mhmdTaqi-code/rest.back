using System.Security.Claims;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface IAdminAuthenticationService
{
    Task<ClaimsPrincipal?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken);
}
