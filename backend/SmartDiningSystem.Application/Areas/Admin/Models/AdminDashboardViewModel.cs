namespace SmartDiningSystem.Application.Areas.Admin.Models;

public class AdminDashboardViewModel
{
    public int ActiveUsers { get; set; }
    public int ActiveRestaurantOwners { get; set; }
    public int RestaurantRecords { get; set; }
    public int PendingRestaurantRecords { get; set; }
}
