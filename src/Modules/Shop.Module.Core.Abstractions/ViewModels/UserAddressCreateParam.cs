using System.ComponentModel.DataAnnotations;

namespace Shop.Module.Core.ViewModels;

public class UserAddressCreateParam
{
    [MaxLength(20)]
    [Required(ErrorMessage = "Please enter contact person")]
    public string ContactName { get; set; }

    [MaxLength(20)]
    [Required(ErrorMessage = "Please enter the phone number")]
    public string Phone { get; set; }

    [MaxLength(200)]
    [Required(ErrorMessage = "Please enter address")]
    public string AddressLine1 { get; set; }

    [Required(ErrorMessage = "Please select province")] public int StateOrProvinceId { get; set; }

    [Required(ErrorMessage = "Please select city")] public int CityId { get; set; }

    public int? DistrictId { get; set; }

    public bool IsDefault { get; set; }
}
