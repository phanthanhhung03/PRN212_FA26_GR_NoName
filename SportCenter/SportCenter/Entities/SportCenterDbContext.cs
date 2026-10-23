using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace SportCenter.Entities;

public partial class SportCenterDbContext : DbContext
{
    public SportCenterDbContext()
    {
    }

    public SportCenterDbContext(DbContextOptions<SportCenterDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<ClassMember> ClassMembers { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<Session> Sessions { get; set; }

    public virtual DbSet<Sport> Sports { get; set; }

    public virtual DbSet<TimeSlot> TimeSlots { get; set; }

    public virtual DbSet<User> Users { get; set; }

    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSqlServer();
    protected override void OnConfiguring(
    DbContextOptionsBuilder optionsBuilder)
    {
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(
                "appsettings.json",
                optional: false,
                reloadOnChange: true)
            .Build();

        string? strConn = config["ConnectionStrings:DefaultConnection"];

        optionsBuilder.UseSqlServer(strConn);
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceId).HasName("PK__attendan__20D6A968F158C2CE");

            entity.ToTable("attendance");

            entity.HasIndex(e => new { e.SessionId, e.MemberId }, "UQ_Session_Member").IsUnique();

            entity.Property(e => e.AttendanceId).HasColumnName("attendance_id");
            entity.Property(e => e.CheckInTime)
                .HasColumnType("datetime")
                .HasColumnName("check_in_time");
            entity.Property(e => e.MarkedBy).HasColumnName("marked_by");
            entity.Property(e => e.MemberId).HasColumnName("member_id");
            entity.Property(e => e.Remarks)
                .HasMaxLength(255)
                .HasColumnName("remarks");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Absent")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.MarkedByNavigation).WithMany(p => p.AttendanceMarkedByNavigations)
                .HasForeignKey(d => d.MarkedBy)
                .HasConstraintName("FK_Attendance_Marker");

            entity.HasOne(d => d.Member).WithMany(p => p.AttendanceMembers)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Attendance_Member");

            entity.HasOne(d => d.Session).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Attendance_Session");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.ClassId).HasName("PK__class__FDF4798631E88987");

            entity.ToTable("class");

            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.ClassName)
                .HasMaxLength(100)
                .HasColumnName("class_name");
            entity.Property(e => e.CoachId).HasColumnName("coach_id");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.MaxCapacity)
                .HasDefaultValue(20)
                .HasColumnName("max_capacity");
            entity.Property(e => e.RoomId).HasColumnName("room_id");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Enrolling")
                .HasColumnName("status");

            entity.HasOne(d => d.Coach).WithMany(p => p.Classes)
                .HasForeignKey(d => d.CoachId)
                .HasConstraintName("FK_Class_Coach");

            entity.HasOne(d => d.Course).WithMany(p => p.Classes)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Class_Course");

            entity.HasOne(d => d.Room).WithMany(p => p.Classes)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Class_Room");

            entity.HasMany(d => d.TimeSlots).WithMany(p => p.Classes)
                .UsingEntity<Dictionary<string, object>>(
                    "ClassTimeSlot",
                    r => r.HasOne<TimeSlot>().WithMany()
                        .HasForeignKey("TimeSlotId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_ClassTS_TimeSlot"),
                    l => l.HasOne<Class>().WithMany()
                        .HasForeignKey("ClassId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_ClassTS_Class"),
                    j =>
                    {
                        j.HasKey("ClassId", "TimeSlotId").HasName("PK_Class_TimeSlot");
                        j.ToTable("class_time_slot");
                        j.IndexerProperty<int>("ClassId").HasColumnName("class_id");
                        j.IndexerProperty<int>("TimeSlotId").HasColumnName("time_slot_id");
                    });
        });

        modelBuilder.Entity<ClassMember>(entity =>
        {
            entity.HasKey(e => e.EnrollmentId).HasName("PK__class_me__6D24AA7A7575DA53");

            entity.ToTable("class_members");

            entity.HasIndex(e => new { e.ClassId, e.MemberId }, "UQ_Class_Member").IsUnique();

            entity.Property(e => e.EnrollmentId).HasColumnName("enrollment_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.EnrolledAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("enrolled_at");
            entity.Property(e => e.MemberId).HasColumnName("member_id");
            entity.Property(e => e.RegisteredBy).HasColumnName("registered_by");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending")
                .HasColumnName("status");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassMembers)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Enrollment_Class");

            entity.HasOne(d => d.Member).WithMany(p => p.ClassMemberMembers)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Enrollment_Member");

            entity.HasOne(d => d.RegisteredByNavigation).WithMany(p => p.ClassMemberRegisteredByNavigations)
                .HasForeignKey(d => d.RegisteredBy)
                .HasConstraintName("FK_Enrollment_Registrar");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseId).HasName("PK__course__8F1EF7AEA855EAD9");

            entity.ToTable("course");

            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.CourseName)
                .HasMaxLength(150)
                .HasColumnName("course_name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Level)
                .HasMaxLength(50)
                .HasColumnName("level");
            entity.Property(e => e.SportId).HasColumnName("sport_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active")
                .HasColumnName("status");
            entity.Property(e => e.TotalSessions).HasColumnName("total_sessions");
            entity.Property(e => e.TuitionFee)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("tuition_fee");

            entity.HasOne(d => d.Sport).WithMany(p => p.Courses)
                .HasForeignKey(d => d.SportId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Course_Sport");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__payment__ED1FC9EA15DC6FC3");

            entity.ToTable("payment");

            entity.HasIndex(e => e.OrderCode, "UQ__payment__99D12D3FE1FAC98B").IsUnique();

            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.CheckoutUrl)
                .HasMaxLength(500)
                .HasColumnName("checkout_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("description");
            entity.Property(e => e.EnrollmentId).HasColumnName("enrollment_id");
            entity.Property(e => e.OrderCode).HasColumnName("order_code");
            entity.Property(e => e.PaidAt)
                .HasColumnType("datetime")
                .HasColumnName("paid_at");
            entity.Property(e => e.PaymentLinkId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("payment_link_id");
            entity.Property(e => e.QrCode).HasColumnName("qr_code");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending")
                .HasColumnName("status");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Payments)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_Payment_CreatedBy");

            entity.HasOne(d => d.Enrollment).WithMany(p => p.Payments)
                .HasForeignKey(d => d.EnrollmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payment_Enrollment");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__role__760965CC123E34D6");

            entity.ToTable("role");

            entity.HasIndex(e => e.RoleName, "UQ__role__783254B1BEE5B618").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.RoomId).HasName("PK__room__19675A8A61EA9463");

            entity.ToTable("room");

            entity.HasIndex(e => e.RoomName, "UQ_Room_Name").IsUnique();

            entity.Property(e => e.RoomId).HasColumnName("room_id");
            entity.Property(e => e.Capacity).HasColumnName("capacity");
            entity.Property(e => e.RoomName)
                .HasMaxLength(100)
                .HasColumnName("room_name");
            entity.Property(e => e.SportId).HasColumnName("sport_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active")
                .HasColumnName("status");

            entity.HasOne(d => d.Sport).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.SportId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Room_Sport");
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("PK__session__69B13FDC48B45047");

            entity.ToTable("session");

            entity.HasIndex(e => new { e.ClassId, e.SessionNo }, "UQ_Class_Session").IsUnique();

            entity.HasIndex(e => new { e.CoachId, e.SessionDate, e.TimeSlotId }, "UX_Session_Coach")
                .IsUnique()
                .HasFilter("([status]<>'Cancelled')");

            entity.HasIndex(e => new { e.RoomId, e.SessionDate, e.TimeSlotId }, "UX_Session_Room")
                .IsUnique()
                .HasFilter("([status]<>'Cancelled')");

            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.CoachId).HasColumnName("coach_id");
            entity.Property(e => e.RoomId).HasColumnName("room_id");
            entity.Property(e => e.SessionDate).HasColumnName("session_date");
            entity.Property(e => e.SessionNo).HasColumnName("session_no");
            entity.Property(e => e.SessionNote)
                .HasMaxLength(500)
                .HasColumnName("session_note");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Scheduled")
                .HasColumnName("status");
            entity.Property(e => e.TimeSlotId).HasColumnName("time_slot_id");

            entity.HasOne(d => d.Class).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Session_Class");

            entity.HasOne(d => d.Coach).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.CoachId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Session_Coach");

            entity.HasOne(d => d.Room).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Session_Room");

            entity.HasOne(d => d.TimeSlot).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.TimeSlotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Session_TimeSlot");
        });

        modelBuilder.Entity<Sport>(entity =>
        {
            entity.HasKey(e => e.SportId).HasName("PK__sport__04392641493D3BB1");

            entity.ToTable("sport");

            entity.Property(e => e.SportId).HasColumnName("sport_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.SportName)
                .HasMaxLength(100)
                .HasColumnName("sport_name");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active")
                .HasColumnName("status");
        });

        modelBuilder.Entity<TimeSlot>(entity =>
        {
            entity.HasKey(e => e.TimeSlotId).HasName("PK__time_slo__47FE23DD5679B996");

            entity.ToTable("time_slot");

            entity.HasIndex(e => new { e.DayOfWeek, e.StartTime, e.EndTime }, "UQ_TimeSlot").IsUnique();

            entity.Property(e => e.TimeSlotId).HasColumnName("time_slot_id");
            entity.Property(e => e.DayOfWeek).HasColumnName("day_of_week");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.SlotName)
                .HasMaxLength(50)
                .HasColumnName("slot_name");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__user__B9BE370FD40DDD63");

            entity.ToTable("user");

            entity.HasIndex(e => e.Email, "UQ__user__AB6E6164E18DF777").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.BirthDate).HasColumnName("birth_date");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("gender");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.SportId).HasColumnName("sport_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active")
                .HasColumnName("status");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.InverseCreatedByNavigation)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_User_CreatedBy");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_User_Role");

            entity.HasOne(d => d.Sport).WithMany(p => p.Users)
                .HasForeignKey(d => d.SportId)
                .HasConstraintName("FK_User_Sport");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
