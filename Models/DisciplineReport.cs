using System;
using System.Collections.Generic;

namespace SchoolManager.Models;

public partial class DisciplineReport
{
    public Guid Id { get; set; }

    public Guid? StudentId { get; set; }

    public Guid? TeacherId { get; set; }

    public DateOnly Date { get; set; }

    public TimeOnly? Hora { get; set; }

    public string? ReportType { get; set; }

    public string? Description { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public Guid? SubjectId { get; set; }

    public Guid? GroupId { get; set; }

    public Guid? GradeLevelId { get; set; }

    public virtual GradeLevel? GradeLevel { get; set; }

    public virtual Group? Group { get; set; }

    public virtual User? Student { get; set; }

    public virtual Subject? Subject { get; set; }

    public virtual User? Teacher { get; set; }
}
