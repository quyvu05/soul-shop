using System.ComponentModel.DataAnnotations;

namespace Shop.Module.Core.ViewModels;

public class LoginPhoneGetCaptchaParam
{
    [Required]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "Malformed phone number")]
    public string Phone { get; set; }
}
