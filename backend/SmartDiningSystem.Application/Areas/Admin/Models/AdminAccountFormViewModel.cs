using System.ComponentModel.DataAnnotations;
using SmartDiningSystem.Domain.Enums;

namespace SmartDiningSystem.Application.Areas.Admin.Models;

public class AdminAccountFormViewModel : IValidatableObject
{
    public Guid? Id { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Confirm password must match the password.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    [Display(Name = "Active Account")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Phone Verified")]
    public bool IsPhoneVerified { get; set; } = true;

    [StringLength(200)]
    [Display(Name = "Restaurant Name")]
    public string? RestaurantName { get; set; }

    [StringLength(1000)]
    [Display(Name = "Restaurant Description")]
    public string? RestaurantDescription { get; set; }

    [StringLength(500)]
    [Display(Name = "Restaurant Address")]
    public string? RestaurantAddress { get; set; }

    [StringLength(50)]
    [Display(Name = "Restaurant Phone Number")]
    public string? RestaurantPhoneNumber { get; set; }

    public IReadOnlyList<AdminRoleOptionViewModel> RoleOptions { get; set; } = [];

    public bool RequiresPasswordFields => !Id.HasValue;

    public bool RequiresRestaurantFields => string.Equals(
        Role,
        UserRole.RestaurantOwner.ToString(),
        StringComparison.Ordinal);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RequiresPasswordFields)
        {
            if (string.IsNullOrWhiteSpace(Password))
            {
                yield return new ValidationResult(
                    "Password is required when creating an account.",
                    [nameof(Password)]);
            }
            else if (Password.Length < 6)
            {
                yield return new ValidationResult(
                    "Password must be at least 6 characters long.",
                    [nameof(Password)]);
            }

            if (string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                yield return new ValidationResult(
                    "Please confirm the password.",
                    [nameof(ConfirmPassword)]);
            }
            else if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
            {
                yield return new ValidationResult(
                    "Confirm password must match the password.",
                    [nameof(ConfirmPassword)]);
            }
        }

        if (!RequiresRestaurantFields)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(RestaurantName))
        {
            yield return new ValidationResult(
                "Restaurant name is required for restaurant owners.",
                [nameof(RestaurantName)]);
        }

        if (string.IsNullOrWhiteSpace(RestaurantDescription))
        {
            yield return new ValidationResult(
                "Restaurant description is required for restaurant owners.",
                [nameof(RestaurantDescription)]);
        }

        if (string.IsNullOrWhiteSpace(RestaurantAddress))
        {
            yield return new ValidationResult(
                "Restaurant address is required for restaurant owners.",
                [nameof(RestaurantAddress)]);
        }

    }
}
