namespace SmartDiningSystem.Application.DTOs.TableAccess;

public enum TableAccessScanResultType
{
    Success = 0,
    InvalidRequest = 1,
    Unauthorized = 2,
    NotFound = 3,
    Occupied = 4,
    Reserved = 5,
    Blocked = 6
}
