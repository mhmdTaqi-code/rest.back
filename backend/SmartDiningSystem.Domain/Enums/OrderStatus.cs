namespace SmartDiningSystem.Domain.Enums;

public enum OrderStatus
{
    OrderReceived = 1,
    Received = OrderReceived,
    Preparing = 2,
    Ready = 3,
    Served = 4
}
