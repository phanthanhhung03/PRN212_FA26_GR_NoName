using System;
using System.Collections.Generic;

namespace SportCenter.Entities;

public partial class Room
{
    public int RoomId { get; set; }

    public int SportId { get; set; }

    public string RoomName { get; set; } = null!;

    public int Capacity { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

    public virtual Sport Sport { get; set; } = null!;
}
