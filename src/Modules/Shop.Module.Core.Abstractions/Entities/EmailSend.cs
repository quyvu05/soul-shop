using Shop.Infrastructure.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace Shop.Module.Core.Entities;

public class EmailSend : EntityBase
{
    public EmailSend()
    {
        CreatedOn = DateTime.Now;
        UpdatedOn = DateTime.Now;
    }

    [Required] public string From { get; set; }

    /// <summary>
    /// Take over
    /// </summary>
    public string To { get; set; }

    public string Cc { get; set; }

    public string Bcc { get; set; }

    public string Subject { get; set; }

    public string Body { get; set; }

    public bool IsHtml { get; set; }

    /// <summary>
    /// External pipeline extension fields。
    /// </summary>
    [StringLength(450)]
    public string OutId { get; set; }

    /// <summary>
    /// Send receipt ID.
    /// </summary>
    [StringLength(450)]
    public string ReceiptId { get; set; }

    /// <summary>
    /// Whether sent successfully
    /// </summary>
    public bool IsSucceed { get; set; }

    /// <summary>
    /// Send success or failure message
    /// </summary>
    public string Message { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateTime UpdatedOn { get; set; }
}
