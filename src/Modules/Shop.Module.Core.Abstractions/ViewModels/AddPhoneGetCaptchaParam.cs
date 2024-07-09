using System.ComponentModel.DataAnnotations;

namespace Shop.Module.Core.ViewModels;

public class AddPhoneGetCaptchaParam
{
    [Required(ErrorMessage = "Please enter the phone number")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "Malformed phone number")]
    public string Phone { get; set; }
}
