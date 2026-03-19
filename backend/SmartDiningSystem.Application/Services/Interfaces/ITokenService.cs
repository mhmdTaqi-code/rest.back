using SmartDiningSystem.Application.DTOs.Auth;
using SmartDiningSystem.Domain.Entities;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface ITokenService
{
    AuthResponseDto CreateToken(UserAccount user);
}
