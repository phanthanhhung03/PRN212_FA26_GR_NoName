using System;
using System.Collections.Generic;

namespace SportCenter.Entities;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public int SessionId { get; set; }

    public int MemberId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? CheckInTime { get; set; }

    public string? Remarks { get; set; }

    public int? MarkedBy { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User? MarkedByNavigation { get; set; }

    public virtual User Member { get; set; } = null!;

    public virtual Session Session { get; set; } = null!;
}
