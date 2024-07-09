using System.ComponentModel.DataAnnotations;

namespace Shop.Module.Payments.Models;

/// <summary>
/// Order payment request parameters
/// </summary>
public class PaymentOrderRequest
{
    [Required] public int OrderId { get; set; }

    /// <summary>
    /// Order number, within 32 characters
    /// </summary>
    [StringLength(32)]
    [Required]
    public string OrderNo { get; set; }

    /// <summary>
    /// Product brief description
    /// Skynet-Mall
    /// Tencent Recharge Center-QQ Member Recharge The title name of the homepage of the website opened by the browser-Product Overview
    /// Tencent-Game Merchant Name-Sales Product Category
    /// </summary>
    [StringLength(128)]
    [Required]
    public string Subject { get; set; }

    /// <summary>
    /// Total order amount, unit yuan (up to 2 decimal places)
    /// </summary>
    [Required]
    [Range(0.01, 50000)]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    public string OpenId { get; set; }
}
