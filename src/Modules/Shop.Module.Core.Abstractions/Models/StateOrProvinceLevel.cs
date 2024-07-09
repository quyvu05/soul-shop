namespace Shop.Module.Core.Models;

/// <summary>
/// State/Province, City, District, Street Type
/// </summary>
public enum StateOrProvinceLevel
{
    Default = 0, //State, province, municipality, autonomous region
    City = 1, //city
    District = 2, //Districts and counties
    Street = 3 //streets, towns
}
