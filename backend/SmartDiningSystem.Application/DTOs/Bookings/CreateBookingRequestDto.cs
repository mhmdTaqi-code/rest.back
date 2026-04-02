using System.ComponentModel;

namespace SmartDiningSystem.Application.DTOs.Bookings;

public class CreateBookingRequestDto
{
    public Guid TableId { get; set; }

    [Description("Baghdad local time in format yyyy-MM-dd HH:mm")]
    [DefaultValue("2026-04-03 22:30")]
    public string ReservationTime { get; set; } = string.Empty;

    public IReadOnlyList<CreateBookingItemRequestDto> Items { get; set; } = [];
}
