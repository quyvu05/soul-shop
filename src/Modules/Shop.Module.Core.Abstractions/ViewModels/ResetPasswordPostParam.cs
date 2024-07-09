using System.ComponentModel.DataAnnotations;

namespace Shop.Module.Core.ViewModels;

public class ResetPasswordPostParam
{
    [Required(ErrorMessage = "Username parameter exception")] public string UserName { get; set; }
}
