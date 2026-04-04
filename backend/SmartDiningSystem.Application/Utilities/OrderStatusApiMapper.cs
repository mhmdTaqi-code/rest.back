using SmartDiningSystem.Domain.Enums;

namespace SmartDiningSystem.Application.Utilities;

public static class OrderStatusApiMapper
{
    public static readonly string[] AllowedStatuses =
    [
        "OrderReceived",
        "Preparing",
        "Ready",
        "Served"
    ];

    public static OrderStatus Normalize(OrderStatus status)
    {
        return status == OrderStatus.Received ? OrderStatus.OrderReceived : status;
    }

    public static string ToApiStatus(OrderStatus status)
    {
        return Normalize(status) switch
        {
            OrderStatus.OrderReceived => "OrderReceived",
            OrderStatus.Preparing => "Preparing",
            OrderStatus.Ready => "Ready",
            OrderStatus.Served => "Served",
            _ => status.ToString()
        };
    }

    public static bool TryParse(string? value, out OrderStatus status)
    {
        status = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (string.Equals(value, "OrderReceived", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "Received", StringComparison.OrdinalIgnoreCase))
        {
            status = OrderStatus.OrderReceived;
            return true;
        }

        if (!Enum.TryParse<OrderStatus>(value, true, out var parsed))
        {
            return false;
        }

        status = Normalize(parsed);
        return true;
    }
}
