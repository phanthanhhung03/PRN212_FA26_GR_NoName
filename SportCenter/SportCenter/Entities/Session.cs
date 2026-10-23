using System;
using System.Collections.Generic;

namespace SportCenter.Entities;

public partial class Session
{
    public int SessionId { get; set; }

    public int ClassId { get; set; }

    public int SessionNo { get; set; }

    public DateOnly SessionDate { get; set; }

    public int TimeSlotId { get; set; }

    public int CoachId { get; set; }

    public int RoomId { get; set; }

    public string Status { get; set; } = null!;

    public string? SessionNote { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual Class Class { get; set; } = null!;

    public virtual User Coach { get; set; } = null!;

    public virtual Room Room { get; set; } = null!;

    public virtual TimeSlot TimeSlot { get; set; } = null!;
}
