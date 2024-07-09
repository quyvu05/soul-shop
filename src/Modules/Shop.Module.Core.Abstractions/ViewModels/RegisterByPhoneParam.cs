using System.ComponentModel.DataAnnotations;

namespace Shop.Module.Core.ViewModels;

public class RegisterByPhoneParam
{
    [Required]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "Malformed phone number")]
    public string Phone { get; set; }

    /// <summary>
    /// Verification code
    /// </summary>
    [Required(ErrorMessage = "Please enter verification code")]
    public string Captcha { get; set; }

    [Required(ErrorMessage = "Please enter password")]
    [StringLength(100, ErrorMessage = "Password length 6-32 characters\", MinimumLength = 6)]", MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "The passwords entered twice do not match")]
    public string ConfirmPassword { get; set; }

    //[EmailAddress]
    public string Email { get; set; }
}
