using System;
using System.Collections.Generic;

namespace SportCenter.Entities;

public partial class Course
{
    public int CourseId { get; set; }

    public int SportId { get; set; }

    public string CourseName { get; set; } = null!;

    public string? Level { get; set; }

    public int TotalSessions { get; set; }

    public decimal TuitionFee { get; set; }

    public string? Description { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual Sport Sport { get; set; } = null!;
}
