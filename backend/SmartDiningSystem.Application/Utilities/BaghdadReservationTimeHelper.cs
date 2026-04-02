using System.Globalization;

namespace SmartDiningSystem.Application.Utilities;

public static class BaghdadReservationTimeHelper
{
    public const string ReservationTimeFormat = "yyyy-MM-dd HH:mm";

    private static readonly Lazy<TimeZoneInfo> BaghdadTimeZone = new(ResolveBaghdadTimeZone);

    public static bool TryParseLocalReservationTime(string? value, out DateTime localDateTime)
    {
        localDateTime = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return DateTime.TryParseExact(
            value,
            ReservationTimeFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out localDateTime);
    }

    public static bool TryParseReservationTimeToUtc(string? value, out DateTime reservationTimeUtc)
    {
        reservationTimeUtc = default;

        if (!TryParseLocalReservationTime(value, out var localDateTime))
        {
            return false;
        }

        var unspecifiedLocalTime = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        reservationTimeUtc = TimeZoneInfo.ConvertTimeToUtc(unspecifiedLocalTime, BaghdadTimeZone.Value);
        return true;
    }

    public static string ToBaghdadLocalDisplayString(DateTime utcDateTime)
    {
        var normalizedUtcDateTime = utcDateTime.Kind switch
        {
            DateTimeKind.Utc => utcDateTime,
            DateTimeKind.Local => utcDateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc)
        };

        return TimeZoneInfo
            .ConvertTimeFromUtc(normalizedUtcDateTime, BaghdadTimeZone.Value)
            .ToString(ReservationTimeFormat, CultureInfo.InvariantCulture);
    }

    private static TimeZoneInfo ResolveBaghdadTimeZone()
    {
        var supportedTimeZoneIds = new[]
        {
            "Asia/Baghdad",
            "Arabic Standard Time"
        };

        foreach (var timeZoneId in supportedTimeZoneIds)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone(
            "Baghdad Standard Time",
            TimeSpan.FromHours(3),
            "Baghdad Standard Time",
            "Baghdad Standard Time");
    }
}
