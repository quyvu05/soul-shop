using System.ComponentModel.DataAnnotations;

namespace Shop.Module.Core.ViewModels;

public class ChangePasswordParam
{
    [Required(ErrorMessage = "Please enter old password")]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string OldPassword { get; set; }

    [Required(ErrorMessage = "Please enter a new password")]
    [StringLength(100, ErrorMessage = "Password length 6-32 characters", MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "The passwords entered twice do not match")]
    public string ConfirmPassword { get; set; }
}
