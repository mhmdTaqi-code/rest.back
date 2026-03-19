using System.ComponentModel.DataAnnotations;

namespace SmartDiningSystem.Application.Areas.Admin.Models;

public class AdminLoginViewModel
{
    [Required]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
