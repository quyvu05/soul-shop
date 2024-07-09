namespace Shop.Module.Hangfire.Models;

/// <summary>
/// Hangfire Job configuration
/// </summary>
public class HangfireOptions
{
    /// <summary>
    /// Whether to enable Redis, if not enabled, the default is memory mode
    /// </summary>
    public bool RedisEnabled { get; set; }

    /// <summary>
    /// Redis Connection string
    /// <see cref=""/>
    /// </summary>
    public string RedisConnection { get; set; }

    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Password
    /// </summary>
    public string Password { get; set; }
}
