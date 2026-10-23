using System;
using System.Collections.Generic;

namespace SportCenter.Entities;

public partial class User
{
    public int UserId { get; set; }

    public int RoleId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Phone { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string? Gender { get; set; }

    public int? SportId { get; set; }

    public string Status { get; set; } = null!;

    public int? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Attendance> AttendanceMarkedByNavigations { get; set; } = new List<Attendance>();

    public virtual ICollection<Attendance> AttendanceMembers { get; set; } = new List<Attendance>();

    public virtual ICollection<ClassMember> ClassMemberMembers { get; set; } = new List<ClassMember>();

    public virtual ICollection<ClassMember> ClassMemberRegisteredByNavigations { get; set; } = new List<ClassMember>();

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<User> InverseCreatedByNavigation { get; set; } = new List<User>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

    public virtual Sport? Sport { get; set; }
}
