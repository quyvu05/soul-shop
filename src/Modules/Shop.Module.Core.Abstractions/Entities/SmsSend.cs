using Shop.Infrastructure.Models;
using Shop.Module.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace Shop.Module.Core.Entities;

public class SmsSend : EntityBase
{
    public SmsSend()
    {
        CreatedOn = DateTime.Now;
        UpdatedOn = DateTime.Now;
    }

    /// <summary>
    /// Mobile phone number to receive text messages
    /// Domestic SMS: 11-digit mobile phone number
    /// </summary>
    [StringLength(450)]
    [Required]
    public string PhoneNumber { set; get; }

    /// <summary>
    /// The value sent (verification code, etc.)
    /// </summary>
    [StringLength(450)]
    public string Value { get; set; }

    /// <summary>
    /// SMS signature name. Please check the signature name column on the console signature management page.
    /// </summary>
    [StringLength(450)]
    public string SignName { get; set; }

    /// <summary>
    /// SMS template type
    /// </summary>
    public SmsTemplateType? TemplateType { get; set; }

    /// <summary>
    /// SMS template ID. Please check the template CODE column on the console template management page.
    /// SMS_153055065
    /// </summary>
    [StringLength(450)]
    public string TemplateCode { set; get; }

    /// <summary>
    /// The actual value corresponding to the SMS template variable, in JSON format.
    /// {"code":"1111"}
    /// </summary>
    public string TemplateParam { set; get; }

    /// <summary>
    /// External pipeline extension field.
    /// </summary>
    [StringLength(450)]
    public string OutId { get; set; }

    /// <summary>
    /// Send receipt ID.
    /// </summary>
    [StringLength(450)]
    public string ReceiptId { get; set; }

    /// <summary>
    /// Has it been used?
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Whether sent successfully
    /// </summary>
    public bool IsSucceed { get; set; }

    /// <summary>
    /// Is it a test message?
    /// The test text message does not actually send the text message, but only generates the sending record.
    /// </summary>
    public bool IsTest { get; set; }

    /// <summary>
    /// Send success or failure message
    /// </summary>
    public string Message { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateTime UpdatedOn { get; set; }
}
