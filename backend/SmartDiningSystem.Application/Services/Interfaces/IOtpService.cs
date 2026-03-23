using SmartDiningSystem.Application.DTOs.Auth;
using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Domain.Enums;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface IOtpService
{
    Task<OtpDispatchResponseDto> CreateAndSendRegistrationOtpAsync(
        PendingRegistration pendingRegistration,
        CancellationToken cancellationToken = default);

    Task<OtpDispatchResponseDto> ResendRegistrationOtpAsync(
        PendingRegistration pendingRegistration,
        CancellationToken cancellationToken = default);

    Task<OtpCode> GetValidOtpAsync(
        string phoneNumber,
        string code,
        CancellationToken cancellationToken = default);
}
