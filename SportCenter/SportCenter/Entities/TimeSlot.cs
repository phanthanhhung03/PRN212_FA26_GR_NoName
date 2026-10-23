using System;
using System.Collections.Generic;

namespace SportCenter.Entities;

public partial class TimeSlot
{
    public int TimeSlotId { get; set; }

    public string SlotName { get; set; } = null!;

    public byte DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
}
