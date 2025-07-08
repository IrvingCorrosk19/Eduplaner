using System;
using System.Collections.Generic;

namespace SchoolManager.Models;

public partial class School
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string? LogoUrl { get; set; }

    public DateTime? CreatedAt { get; set; }

    public Guid? AdminId { get; set; }

    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();

    public virtual ICollection<ActivityType> ActivityTypes { get; set; } = new List<ActivityType>();

    public virtual User? Admin { get; set; }

    public virtual ICollection<Area> Areas { get; set; } = new List<Area>();

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

    public virtual ICollection<SecuritySetting> SecuritySettings { get; set; } = new List<SecuritySetting>();

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual ICollection<SubjectAssignment> SubjectAssignments { get; set; } = new List<SubjectAssignment>();

    public virtual ICollection<Subject> Subjects { get; set; } = new List<Subject>();

    public virtual ICollection<Trimester> Trimesters { get; set; } = new List<Trimester>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
