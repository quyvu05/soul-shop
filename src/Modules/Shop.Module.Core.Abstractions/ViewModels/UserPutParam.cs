using System.ComponentModel.DataAnnotations;

namespace Shop.Module.Core.ViewModels;

public class UserPutParam
{
    /// <summary>
    /// Nickname/full name
    /// </summary>
    [Required(ErrorMessage = "Please enter your nickname/full name")]
    [StringLength(20, ErrorMessage = "Nickname cannot exceed 20 characters")]
    public string FullName { get; set; }

    public int? MediaId { get; set; }

    //public string AdminRemark { get; set; }
}
