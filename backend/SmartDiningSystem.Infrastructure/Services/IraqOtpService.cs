using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartDiningSystem.Application.Configuration;
using SmartDiningSystem.Infrastructure.Data;
using SmartDiningSystem.Application.DTOs.Auth;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Application.Utilities;

namespace SmartDiningSystem.Infrastructure.Services;

public class IraqOtpService : IOtpService
{
    private const int OtpLength = 6;
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(5);

    private readonly AppDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly IraqOtpOptions _iraqOtpOptions;
    private readonly ILogger<IraqOtpService> _logger;

    public IraqOtpService(
        AppDbContext dbContext,
        HttpClient httpClient,
        IOptions<IraqOtpOptions> iraqOtpOptions,
        ILogger<IraqOtpService> logger)
    {
        _dbContext = dbContext;
        _httpClient = httpClient;
        _iraqOtpOptions = iraqOtpOptions.Value;
        _logger = logger;
    }

    public async Task<OtpDispatchResponseDto> CreateAndSendRegistrationOtpAsync(
        PendingRegistration pendingRegistration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pendingRegistration);

        return await CreateAndSendOtpAsync(
            pendingRegistration.PhoneNumber,
            OtpPurpose.Registration,
            pendingRegistrationId: pendingRegistration.Id,
            userAccountId: null,
            cancellationToken);
    }

    public async Task<OtpCode> GetValidOtpAsync(
        string phoneNumber,
        string code,
        CancellationToken cancellationToken = default)
    {
        var normalizedPhoneNumber = NormalizePhoneNumber(phoneNumber);
        var normalizedCode = code.Trim();
        var nowUtc = DateTime.UtcNow;

        _logger.LogInformation(
            "Verifying OTP for normalized phone number {PhoneNumber}.",
            normalizedPhoneNumber);

        var latestOtpCode = await _dbContext.OtpCodes
            .Include(entity => entity.UserAccount)
            .Include(entity => entity.PendingRegistration)
            .Where(entity => entity.PhoneNumber == normalizedPhoneNumber)
            .OrderByDescending(entity => entity.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestOtpCode is null)
        {
            _logger.LogWarning(
                "OTP verification failed for phone number {PhoneNumber}: no OTP records found.",
                normalizedPhoneNumber);

            throw new AuthServiceException("Invalid OTP code.", StatusCodes.Status400BadRequest);
        }

        var hasAssociatedPrincipal = latestOtpCode.UserAccount is not null || latestOtpCode.PendingRegistration is not null;
        if (!hasAssociatedPrincipal)
        {
            _logger.LogWarning(
                "OTP verification failed for phone number {PhoneNumber}: latest OTP record has no associated login account or pending registration.",
                normalizedPhoneNumber);

            throw new AuthServiceException("Invalid OTP code.", StatusCodes.Status400BadRequest);
        }

        if (latestOtpCode.IsUsed)
        {
            _logger.LogInformation(
                "OTP verification failed for phone number {PhoneNumber}: latest OTP was already used.",
                normalizedPhoneNumber);

            throw new AuthServiceException("OTP code has already been used.", StatusCodes.Status400BadRequest);
        }

        if (latestOtpCode.ExpiresAtUtc < nowUtc)
        {
            _logger.LogInformation(
                "OTP verification failed for phone number {PhoneNumber}: latest OTP expired at {ExpiresAtUtc}.",
                normalizedPhoneNumber,
                latestOtpCode.ExpiresAtUtc);

            throw new AuthServiceException("OTP code has expired.", StatusCodes.Status400BadRequest);
        }

        if (!string.Equals(latestOtpCode.Code, normalizedCode, StringComparison.Ordinal))
        {
            _logger.LogInformation(
                "OTP verification failed for phone number {PhoneNumber}: latest valid OTP exists but the submitted code did not match.",
                normalizedPhoneNumber);

            throw new AuthServiceException("Invalid OTP code.", StatusCodes.Status400BadRequest);
        }

        _logger.LogInformation(
            "OTP verification lookup succeeded for phone number {PhoneNumber}. Purpose: {Purpose}, CreatedAtUtc: {CreatedAtUtc}.",
            normalizedPhoneNumber,
            latestOtpCode.Purpose,
            latestOtpCode.CreatedAtUtc);

        return latestOtpCode;
    }

    private async Task<OtpDispatchResponseDto> CreateAndSendOtpAsync(
        string phoneNumber,
        OtpPurpose purpose,
        Guid? pendingRegistrationId,
        Guid? userAccountId,
        CancellationToken cancellationToken)
    {
        var normalizedPhoneNumber = NormalizePhoneNumber(phoneNumber);
        EnsureProviderConfigured();

        var activeCodes = await _dbContext.OtpCodes
            .Where(otpCode => otpCode.PhoneNumber == normalizedPhoneNumber && !otpCode.IsUsed)
            .ToListAsync(cancellationToken);

        foreach (var activeCode in activeCodes)
        {
            activeCode.IsUsed = true;
            activeCode.UsedAtUtc = DateTime.UtcNow;
        }

        var otpCode = new OtpCode
        {
            Id = Guid.NewGuid(),
            UserAccountId = userAccountId,
            PendingRegistrationId = pendingRegistrationId,
            PhoneNumber = normalizedPhoneNumber,
            Code = GenerateCode(),
            Purpose = purpose,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.Add(OtpLifetime),
            IsUsed = false
        };

        _dbContext.OtpCodes.Add(otpCode);
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await SendOtpThroughProviderAsync(normalizedPhoneNumber, otpCode.Code, cancellationToken);
        }
        catch
        {
            otpCode.IsUsed = true;
            otpCode.UsedAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }

        _logger.LogInformation(
            "OTP sent successfully through OTPIQ for phone number {PhoneNumber} and purpose {Purpose}.",
            normalizedPhoneNumber,
            purpose);

        return new OtpDispatchResponseDto
        {
            PhoneNumber = normalizedPhoneNumber,
            Purpose = purpose.ToString(),
            ExpiresAtUtc = otpCode.ExpiresAtUtc
        };
    }

    private async Task SendOtpThroughProviderAsync(
        string phoneNumber,
        string code,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != OtpLength)
        {
            throw new AuthServiceException(
                "Failed to generate a valid OTP code.",
                StatusCodes.Status500InternalServerError);
        }

        var requestUri = BuildSendSmsUri();
        var requestPayload = new OtpIqSendSmsRequest
        {
            PhoneNumber = phoneNumber,
            SmsType = "verification",
            Provider = "whatsapp-sms",
            VerificationCode = code
        };
        var requestBody = JsonSerializer.Serialize(requestPayload);

        _logger.LogInformation(
            "Sending OTPIQ request. Url: {RequestUrl}, PhoneNumber: {PhoneNumber}, Provider: {Provider}, RequestBody: {RequestBody}",
            requestUri,
            phoneNumber,
            requestPayload.Provider,
            requestBody);

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _iraqOtpOptions.ApiKey);
        request.Content = JsonContent.Create(requestPayload);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var safeResponseBody = await ReadSafeResponseBodyAsync(response, cancellationToken);

            _logger.LogInformation(
                "OTPIQ response received. Url: {RequestUrl}, StatusCode: {StatusCode}, ResponseBody: {ResponseBody}",
                requestUri,
                (int)response.StatusCode,
                safeResponseBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "OTPIQ send request failed. Url: {RequestUrl}, PhoneNumber: {PhoneNumber}, StatusCode: {StatusCode}, ResponseBody: {ResponseBody}",
                    requestUri,
                    phoneNumber,
                    (int)response.StatusCode,
                    safeResponseBody);

                throw new AuthServiceException(
                    $"OTPIQ send failed with status {(int)response.StatusCode}. Provider response: {safeResponseBody}",
                    StatusCodes.Status502BadGateway,
                    new Dictionary<string, string[]>
                    {
                        ["provider"] = ["OTPIQ"],
                        ["requestUrl"] = [requestUri.ToString()],
                        ["phoneNumber"] = [phoneNumber],
                        ["statusCode"] = [((int)response.StatusCode).ToString()],
                        ["providerResponse"] = [safeResponseBody],
                        ["providerValue"] = [requestPayload.Provider]
                    });
            }

            _logger.LogInformation(
                "OTPIQ send request succeeded. Url: {RequestUrl}, PhoneNumber: {PhoneNumber}, StatusCode: {StatusCode}, VerificationCodeLength: {VerificationCodeLength}",
                requestUri,
                phoneNumber,
                (int)response.StatusCode,
                code.Length);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "OTPIQ send request failed. Url: {RequestUrl}, PhoneNumber: {PhoneNumber}, Error: {ErrorMessage}",
                requestUri,
                phoneNumber,
                exception.Message);

            throw new AuthServiceException(
                $"OTPIQ send request failed: {exception.Message}",
                StatusCodes.Status502BadGateway,
                new Dictionary<string, string[]>
                {
                    ["provider"] = ["OTPIQ"],
                    ["requestUrl"] = [requestUri.ToString()],
                    ["phoneNumber"] = [phoneNumber],
                    ["providerError"] = [exception.Message]
                });
        }
        catch (TaskCanceledException exception)
        {
            _logger.LogError(
                exception,
                "OTPIQ send request timed out. Url: {RequestUrl}, PhoneNumber: {PhoneNumber}, Error: {ErrorMessage}",
                requestUri,
                phoneNumber,
                exception.Message);

            throw new AuthServiceException(
                $"OTPIQ send request timed out: {exception.Message}",
                StatusCodes.Status502BadGateway,
                new Dictionary<string, string[]>
                {
                    ["provider"] = ["OTPIQ"],
                    ["requestUrl"] = [requestUri.ToString()],
                    ["phoneNumber"] = [phoneNumber],
                    ["providerError"] = ["The OTPIQ request timed out."],
                    ["exceptionMessage"] = [exception.Message]
                });
        }
    }

    private void EnsureProviderConfigured()
    {
        if (!Uri.TryCreate(_iraqOtpOptions.BaseUrl, UriKind.Absolute, out _)
            || string.IsNullOrWhiteSpace(_iraqOtpOptions.SendSmsEndpoint)
            || string.IsNullOrWhiteSpace(_iraqOtpOptions.ApiKey)
            || _iraqOtpOptions.ApiKey.Contains("PLACEHOLDER_ONLY", StringComparison.OrdinalIgnoreCase))
        {
            throw new AuthServiceException(
                "OTPIQ provider configuration is missing or invalid.",
                StatusCodes.Status502BadGateway);
        }
    }

    private static string GenerateCode()
    {
        return RandomNumberGenerator.GetInt32(0, 1_000_000).ToString($"D{OtpLength}");
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        if (!IraqiPhoneNumberHelper.TryNormalize(phoneNumber, out var normalizedPhoneNumber))
        {
            throw new AuthServiceException("Phone number must be a valid Iraqi mobile number.", StatusCodes.Status400BadRequest);
        }

        return normalizedPhoneNumber;
    }

    private string BuildSendSmsEndpoint()
    {
        return _iraqOtpOptions.SendSmsEndpoint.StartsWith("/", StringComparison.Ordinal)
            ? _iraqOtpOptions.SendSmsEndpoint
            : $"/{_iraqOtpOptions.SendSmsEndpoint}";
    }

    private Uri BuildSendSmsUri()
    {
        if (!Uri.TryCreate(_iraqOtpOptions.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new AuthServiceException(
                "OTPIQ provider configuration is missing or invalid.",
                StatusCodes.Status502BadGateway);
        }

        return new Uri(baseUri, BuildSendSmsEndpoint());
    }

    private static async Task<string> ReadSafeResponseBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
        {
            return "<empty>";
        }

        return content.Length <= 500 ? content : content[..500];
    }

    private sealed class OtpIqSendSmsRequest
    {
        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; } = string.Empty;

        [JsonPropertyName("smsType")]
        public string SmsType { get; set; } = string.Empty;

        [JsonPropertyName("provider")]
        public string Provider { get; set; } = string.Empty;

        [JsonPropertyName("verificationCode")]
        public string VerificationCode { get; set; } = string.Empty;
    }
}
