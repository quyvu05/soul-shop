using System.ComponentModel.DataAnnotations;

namespace Shop.Module.Core.ViewModels;

public class AddPhoneParam
{
    [Required(ErrorMessage = "Please enter the phone number")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "Malformed phone number")]
    public string Phone { get; set; }

    [Required(ErrorMessage = "Please enter verification code")] public string Code { get; set; }
}
