using System;
using System.Collections.Generic;

namespace SchoolManager.Models;

public partial class StudentAssignment
{
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }

    public Guid GradeId { get; set; }

    public Guid GroupId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual GradeLevel Grade { get; set; } = null!;

    public virtual Group Group { get; set; } = null!;

    public virtual User Student { get; set; } = null!;
}
