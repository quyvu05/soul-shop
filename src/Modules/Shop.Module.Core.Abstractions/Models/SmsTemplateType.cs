namespace Shop.Module.Core.Models;

/// <summary>
/// SMS template type
/// </summary>
public enum SmsTemplateType
{
    /// <summary>
    /// Verification code
    /// </summary>
    Captcha = 0,

    /// <summary>
    /// SMS notification
    /// </summary>
    Notification = 1
}
