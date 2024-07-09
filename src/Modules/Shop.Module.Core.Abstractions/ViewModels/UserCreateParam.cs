using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Shop.Module.Core.ViewModels;

public class UserCreateParam
{
    [Required]
    [RegularExpression(@"(\w[-\w.?]*@?[-\w.?]*){4,64}", ErrorMessage = "The username is 4-64 characters long and can only be composed of \"letters, numbers, and special characters (._-@)\"")]
    public string UserName { get; set; }

    [Required] public string FullName { get; set; }

    [EmailAddress] public string Email { get; set; }

    [RegularExpression(@"[0-9-()（）]{4,32}", ErrorMessage = "Mobile phone number format is incorrect")]
    public string PhoneNumber { get; set; }

    //[Required(ErrorMessage = "Password can not be blank")]
    //[StringLength(maximumLength: 32, MinimumLength = 6, ErrorMessage = "Password length 6-32 characters")]
    public string Password { get; set; }

    /// <summary>
    /// Activated
    /// </summary>
    public bool IsActive { get; set; }

    public IList<int> RoleIds { get; set; } = new List<int>();

    public string AdminRemark { get; set; }
}
