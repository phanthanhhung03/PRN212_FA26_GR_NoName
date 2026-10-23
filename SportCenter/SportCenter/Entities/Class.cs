using System;
using System.Collections.Generic;

namespace SportCenter.Entities;

public partial class Class
{
    public int ClassId { get; set; }

    public int CourseId { get; set; }

    public int? CoachId { get; set; }

    public int RoomId { get; set; }

    public string ClassName { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public int MaxCapacity { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<ClassMember> ClassMembers { get; set; } = new List<ClassMember>();

    public virtual User? Coach { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual Room Room { get; set; } = null!;

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

    public virtual ICollection<TimeSlot> TimeSlots { get; set; } = new List<TimeSlot>();
}
