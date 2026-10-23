using System;
using System.Collections.Generic;

namespace SportCenter.Entities;

public partial class Sport
{
    public int SportId { get; set; }

    public string SportName { get; set; } = null!;

    public string? Description { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
