namespace SmartDiningSystem.Domain.Enums;

public enum OrderStatus
{
    OrderReceived = 0,
    Received = OrderReceived,
    Preparing = 1,
    Ready = 2,
    Served = 3
}
