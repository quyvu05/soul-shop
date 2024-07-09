using System.ComponentModel;

namespace Shop.Module.Core.Models;

/// <summary>
/// Types of user two-factor authentication
/// </summary>
public enum UserTwoFactorType
{
    [Description("cell phone")] Phone = 0,
    [Description("Mail")] Email = 1
}
