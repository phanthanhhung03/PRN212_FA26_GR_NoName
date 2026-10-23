using System;
using System.Collections.Generic;

namespace SportCenter.Entities;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int EnrollmentId { get; set; }

    public long OrderCode { get; set; }

    public decimal Amount { get; set; }

    public string Description { get; set; } = null!;

    public string? PaymentLinkId { get; set; }

    public string? QrCode { get; set; }

    public string? CheckoutUrl { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? PaidAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ClassMember Enrollment { get; set; } = null!;
}
