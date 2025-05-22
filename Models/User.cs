using System;
using System.Collections.Generic;

namespace SchoolManager.Models;

public partial class User
{
    public Guid Id { get; set; }

    public Guid? SchoolId { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string? Status { get; set; }

    public bool? TwoFactorEnabled { get; set; }

    public DateTime? LastLogin { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? LastName { get; set; }

    public string? DocumentId { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();

    public virtual ICollection<Attendance> AttendanceStudents { get; set; } = new List<Attendance>();

    public virtual ICollection<Attendance> AttendanceTeachers { get; set; } = new List<Attendance>();

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<DisciplineReport> DisciplineReportStudents { get; set; } = new List<DisciplineReport>();

    public virtual ICollection<DisciplineReport> DisciplineReportTeachers { get; set; } = new List<DisciplineReport>();

    public virtual School? School { get; set; }

    public virtual ICollection<StudentActivityScore> StudentActivityScores { get; set; } = new List<StudentActivityScore>();

    public virtual ICollection<StudentAssignment> StudentAssignments { get; set; } = new List<StudentAssignment>();

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual ICollection<TeacherAssignment> TeacherAssignments { get; set; } = new List<TeacherAssignment>();

    public virtual ICollection<GradeLevel> Grades { get; set; } = new List<GradeLevel>();

    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

    public virtual ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}
