using System;
using System.Collections.Generic;

namespace SportCenter.Entities;

public partial class ClassMember
{
    public int EnrollmentId { get; set; }

    public int ClassId { get; set; }

    public int MemberId { get; set; }

    public DateTime EnrolledAt { get; set; }

    public int? RegisteredBy { get; set; }

    public string Status { get; set; } = null!;

    public virtual Class Class { get; set; } = null!;

    public virtual User Member { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual User? RegisteredByNavigation { get; set; }
}
