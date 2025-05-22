using System;
using System.Collections.Generic;

namespace SchoolManager.Models;

public partial class Trimester
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public DateTime? CreatedAt { get; set; }
}
