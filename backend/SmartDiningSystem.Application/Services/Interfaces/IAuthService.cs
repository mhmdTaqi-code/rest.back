using SmartDiningSystem.Application.DTOs.Auth;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface IAuthService
{
    Task<OtpDispatchResponseDto> RegisterUserAsync(RegisterUserRequestDto request, CancellationToken cancellationToken = default);
    Task<OtpDispatchResponseDto> RegisterOwnerAsync(RegisterOwnerRequestDto request, CancellationToken cancellationToken = default);
    Task<OtpDispatchResponseDto> ResendOtpAsync(ResendOtpRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> VerifyOtpAsync(VerifyOtpRequestDto request, CancellationToken cancellationToken = default);
}
