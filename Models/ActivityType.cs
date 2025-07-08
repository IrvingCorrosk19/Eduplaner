using System;
using System.Collections.Generic;

namespace SchoolManager.Models;

public partial class ActivityType
{
    public Guid Id { get; set; }

    public Guid? SchoolId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Icon { get; set; }

    public string? Color { get; set; }

    public bool IsGlobal { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();

    public virtual School? School { get; set; }
}
