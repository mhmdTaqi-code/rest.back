using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Infrastructure.Data;
using SmartDiningSystem.Application.DTOs.Auth;
using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Application.Utilities;

namespace SmartDiningSystem.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IOtpService _otpService;
    private readonly IPasswordHashService _passwordHashService;
    private readonly ITokenService _tokenService;

    public AuthService(
        AppDbContext dbContext,
        IOtpService otpService,
        IPasswordHashService passwordHashService,
        ITokenService tokenService)
    {
        _dbContext = dbContext;
        _otpService = otpService;
        _passwordHashService = passwordHashService;
        _tokenService = tokenService;
    }

    public async Task<OtpDispatchResponseDto> RegisterUserAsync(
        RegisterUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var normalizedPhoneNumber = NormalizePhoneNumber(request.PhoneNumber);
        var normalizedEmail = NormalizeEmail(request.Email);
        var normalizedUsername = NormalizeUsername(request.Username);

        var pendingRegistration = await GetOrCreatePendingRegistrationAsync(normalizedPhoneNumber, cancellationToken);

        await EnsureRegistrationIdentityIsAvailableAsync(
            normalizedEmail,
            normalizedUsername,
            normalizedPhoneNumber,
            pendingRegistration.Id,
            cancellationToken);

        pendingRegistration.FullName = request.FullName.Trim();
        pendingRegistration.Email = normalizedEmail;
        pendingRegistration.PhoneNumber = normalizedPhoneNumber;
        pendingRegistration.Username = normalizedUsername;
        pendingRegistration.PasswordHash = _passwordHashService.HashPassword(request.Password);
        pendingRegistration.Role = UserRole.User;
        pendingRegistration.RestaurantName = null;
        pendingRegistration.RestaurantDescription = null;
        pendingRegistration.RestaurantAddress = null;
        pendingRegistration.RestaurantPhoneNumber = null;
        pendingRegistration.CreatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _otpService.CreateAndSendRegistrationOtpAsync(pendingRegistration, cancellationToken);
    }

    public async Task<OtpDispatchResponseDto> RegisterOwnerAsync(
        RegisterOwnerRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var normalizedOwnerPhoneNumber = NormalizePhoneNumber(request.PhoneNumber);
        var normalizedEmail = NormalizeEmail(request.Email);
        var normalizedUsername = NormalizeUsername(request.Username);
        var normalizedRestaurantPhoneNumber = NormalizePhoneNumber(request.RestaurantPhoneNumber);

        var pendingRegistration = await GetOrCreatePendingRegistrationAsync(normalizedOwnerPhoneNumber, cancellationToken);

        await EnsureRegistrationIdentityIsAvailableAsync(
            normalizedEmail,
            normalizedUsername,
            normalizedOwnerPhoneNumber,
            pendingRegistration.Id,
            cancellationToken);

        pendingRegistration.FullName = request.FullName.Trim();
        pendingRegistration.Email = normalizedEmail;
        pendingRegistration.PhoneNumber = normalizedOwnerPhoneNumber;
        pendingRegistration.Username = normalizedUsername;
        pendingRegistration.PasswordHash = _passwordHashService.HashPassword(request.Password);
        pendingRegistration.Role = UserRole.RestaurantOwner;
        pendingRegistration.RestaurantName = request.RestaurantName.Trim();
        pendingRegistration.RestaurantDescription = request.RestaurantDescription!.Trim();
        pendingRegistration.RestaurantAddress = request.RestaurantAddress.Trim();
        pendingRegistration.RestaurantPhoneNumber = normalizedRestaurantPhoneNumber;
        pendingRegistration.CreatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _otpService.CreateAndSendRegistrationOtpAsync(pendingRegistration, cancellationToken);
    }

    public async Task<AuthResponseDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var normalizedUsername = NormalizeUsername(request.Username);

        var user = await _dbContext.UserAccounts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                userAccount => userAccount.Username == normalizedUsername && userAccount.IsActive,
                cancellationToken);

        if (user is null || !_passwordHashService.VerifyPassword(user.PasswordHash, request.Password))
        {
            throw new AuthServiceException("Invalid username or password.", StatusCodes.Status401Unauthorized);
        }

        if (user.Role == UserRole.RestaurantOwner)
        {
            await EnsureRestaurantOwnerCanLoginAsync(user.Id, cancellationToken);
        }

        return _tokenService.CreateToken(user);
    }

    public async Task<AuthResponseDto> VerifyOtpAsync(
        VerifyOtpRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var otpCode = await _otpService.GetValidOtpAsync(request.PhoneNumber, request.Code, cancellationToken);
        var user = await ResolveUserFromValidOtpAsync(otpCode, cancellationToken);
        var isOwnerRegistration = otpCode.PendingRegistration?.Role == UserRole.RestaurantOwner;

        otpCode.IsUsed = true;
        otpCode.UsedAtUtc = DateTime.UtcNow;
        user.IsPhoneVerified = true;
        user.UpdatedAtUtc = DateTime.UtcNow;

        if (otpCode.PendingRegistration is not null)
        {
            await ReassignRegistrationOtpsToFinalAccountAsync(
                otpCode.PendingRegistration.Id,
                user.Id,
                cancellationToken);

            _dbContext.PendingRegistrations.Remove(otpCode.PendingRegistration);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        if (isOwnerRegistration)
        {
            return new AuthResponseDto
            {
                AccessGranted = false,
                PendingAdminReview = true,
                ApprovalStatus = RestaurantApprovalStatus.Pending.ToString(),
                User = new UserSummaryDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Username = user.Username,
                    Role = user.Role.ToString()
                }
            };
        }

        return _tokenService.CreateToken(user);
    }

    private async Task<UserAccount> ResolveUserFromValidOtpAsync(OtpCode otpCode, CancellationToken cancellationToken)
    {
        if (otpCode.PendingRegistration is null)
        {
            throw new AuthServiceException("Pending registration was not found for this OTP.", StatusCodes.Status400BadRequest);
        }

        var pendingRegistration = otpCode.PendingRegistration;

        await EnsureFinalAccountIdentityIsAvailableAsync(
            pendingRegistration.Email,
            pendingRegistration.Username,
            pendingRegistration.PhoneNumber,
            cancellationToken);

        var nowUtc = DateTime.UtcNow;
        var user = new UserAccount
        {
            Id = Guid.NewGuid(),
            FullName = pendingRegistration.FullName,
            Email = pendingRegistration.Email,
            PhoneNumber = pendingRegistration.PhoneNumber,
            Username = pendingRegistration.Username,
            PasswordHash = pendingRegistration.PasswordHash,
            IsPhoneVerified = true,
            IsActive = true,
            Role = pendingRegistration.Role,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };

        _dbContext.UserAccounts.Add(user);

        if (pendingRegistration.Role == UserRole.RestaurantOwner)
        {
            var restaurant = new Restaurant
            {
                Id = Guid.NewGuid(),
                OwnerId = user.Id,
                Name = pendingRegistration.RestaurantName ?? string.Empty,
                Description = pendingRegistration.RestaurantDescription,
                Address = pendingRegistration.RestaurantAddress ?? string.Empty,
                ContactPhone = pendingRegistration.RestaurantPhoneNumber ?? string.Empty,
                ApprovalStatus = RestaurantApprovalStatus.Pending,
                CreatedAtUtc = nowUtc
            };

            _dbContext.Restaurants.Add(restaurant);
        }

        return user;
    }

    private async Task EnsureRegistrationIdentityIsAvailableAsync(
        string email,
        string username,
        string phoneNumber,
        Guid currentPendingRegistrationId,
        CancellationToken cancellationToken)
    {
        await EnsureFinalAccountIdentityIsAvailableAsync(email, username, phoneNumber, cancellationToken);

        var pendingEmailExists = await _dbContext.PendingRegistrations
            .AnyAsync(
                registration => registration.Email == email && registration.Id != currentPendingRegistrationId,
                cancellationToken);

        if (pendingEmailExists)
        {
            throw new AuthServiceException("A pending registration already uses this email.", StatusCodes.Status409Conflict);
        }

        var pendingUsernameExists = await _dbContext.PendingRegistrations
            .AnyAsync(
                registration => registration.Username == username && registration.Id != currentPendingRegistrationId,
                cancellationToken);

        if (pendingUsernameExists)
        {
            throw new AuthServiceException("A pending registration already uses this username.", StatusCodes.Status409Conflict);
        }

        var pendingPhoneExists = await _dbContext.PendingRegistrations
            .AnyAsync(
                registration => registration.PhoneNumber == phoneNumber && registration.Id != currentPendingRegistrationId,
                cancellationToken);

        if (pendingPhoneExists)
        {
            throw new AuthServiceException("A pending registration already uses this phone number.", StatusCodes.Status409Conflict);
        }
    }

    private async Task EnsureFinalAccountIdentityIsAvailableAsync(
        string email,
        string username,
        string phoneNumber,
        CancellationToken cancellationToken)
    {
        var emailExists = await _dbContext.UserAccounts
            .AnyAsync(userAccount => userAccount.Email == email, cancellationToken);

        if (emailExists)
        {
            throw new AuthServiceException("An account with this email already exists.", StatusCodes.Status409Conflict);
        }

        var usernameExists = await _dbContext.UserAccounts
            .AnyAsync(userAccount => userAccount.Username == username, cancellationToken);

        if (usernameExists)
        {
            throw new AuthServiceException("An account with this username already exists.", StatusCodes.Status409Conflict);
        }

        var phoneExists = await _dbContext.UserAccounts
            .AnyAsync(userAccount => userAccount.PhoneNumber == phoneNumber, cancellationToken);

        if (phoneExists)
        {
            throw new AuthServiceException("An account with this phone number already exists.", StatusCodes.Status409Conflict);
        }
    }

    private async Task ReassignRegistrationOtpsToFinalAccountAsync(
        Guid pendingRegistrationId,
        Guid userAccountId,
        CancellationToken cancellationToken)
    {
        var registrationOtps = await _dbContext.OtpCodes
            .Where(otp => otp.PendingRegistrationId == pendingRegistrationId)
            .ToListAsync(cancellationToken);

        foreach (var registrationOtp in registrationOtps)
        {
            registrationOtp.UserAccountId = userAccountId;
            registrationOtp.PendingRegistrationId = null;
        }
    }

    private async Task<PendingRegistration> GetOrCreatePendingRegistrationAsync(string phoneNumber, CancellationToken cancellationToken)
    {
        var pendingRegistration = await _dbContext.PendingRegistrations
            .SingleOrDefaultAsync(entity => entity.PhoneNumber == phoneNumber, cancellationToken);

        if (pendingRegistration is not null)
        {
            return pendingRegistration;
        }

        pendingRegistration = new PendingRegistration
        {
            Id = Guid.NewGuid(),
            PhoneNumber = phoneNumber,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.PendingRegistrations.Add(pendingRegistration);
        return pendingRegistration;
    }

    private async Task EnsureRestaurantOwnerCanLoginAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        var restaurant = await _dbContext.Restaurants
            .AsNoTracking()
            .Where(entity => entity.OwnerId == ownerId)
            .OrderBy(entity => entity.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (restaurant is null)
        {
            throw new AuthServiceException(
                "Restaurant owner access is unavailable because no linked restaurant request was found.",
                StatusCodes.Status403Forbidden);
        }

        switch (restaurant.ApprovalStatus)
        {
            case RestaurantApprovalStatus.Approved:
                return;
            case RestaurantApprovalStatus.Pending:
                throw new AuthServiceException(
                    "Your restaurant request is under review.",
                    StatusCodes.Status403Forbidden,
                    new Dictionary<string, string[]>
                    {
                        ["approvalStatus"] = [RestaurantApprovalStatus.Pending.ToString()]
                    });
            case RestaurantApprovalStatus.Rejected:
                throw new AuthServiceException(
                    string.IsNullOrWhiteSpace(restaurant.RejectionReason)
                        ? "Your restaurant request was rejected."
                        : $"Your restaurant request was rejected: {restaurant.RejectionReason}",
                    StatusCodes.Status403Forbidden,
                    new Dictionary<string, string[]>
                    {
                        ["approvalStatus"] = [RestaurantApprovalStatus.Rejected.ToString()],
                        ["rejectionReason"] = [restaurant.RejectionReason ?? string.Empty]
                    });
            default:
                throw new AuthServiceException(
                    "Restaurant owner access is unavailable because the restaurant approval state is invalid.",
                    StatusCodes.Status403Forbidden);
        }
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        if (!IraqiPhoneNumberHelper.TryNormalize(phoneNumber, out var normalizedPhoneNumber))
        {
            throw new AuthServiceException("Phone number must be a valid Iraqi mobile number.", StatusCodes.Status400BadRequest);
        }

        return normalizedPhoneNumber;
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static string NormalizeUsername(string username)
    {
        return username.Trim().ToLowerInvariant();
    }
}
