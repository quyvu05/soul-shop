using System.ComponentModel.DataAnnotations;

namespace Shop.Module.Core.ViewModels;

public class AddEmailPostParam
{
    [Required]
    [EmailAddress(ErrorMessage = "Email address format error")]
    public string Email { get; set; }
}
