using System;
using System.Collections.Generic;
using System.Text;

namespace Shop.Module.Core.ViewModels;

public class UserQueryParam
{
    public string Name { get; set; }

    public string Email { get; set; }

    /// <summary>
    /// Activated
    /// </summary>
    public bool? IsActive { get; set; }

    public string PhoneNumber { get; set; }

    /// <summary>
    /// Contact information, email/phone
    /// </summary>
    public string Contact { get; set; }

    public IList<int> RoleIds { get; set; } = new List<int>();
}
