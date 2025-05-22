using System;
using System.Collections.Generic;

namespace SchoolManager.Models;

public partial class StudentActivityScore
{
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }

    public Guid ActivityId { get; set; }

    public decimal Score { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Activity Activity { get; set; } = null!;

    public virtual User Student { get; set; } = null!;
}
