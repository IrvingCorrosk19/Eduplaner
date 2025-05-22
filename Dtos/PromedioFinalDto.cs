using System.Collections.Generic;

namespace SchoolManager.Dtos
{
    public class PromedioFinalDto
    {
        public string StudentId { get; set; }
        public string StudentFullName { get; set; }
        public Dictionary<string, decimal> Promedios { get; set; } = new Dictionary<string, decimal>();
        public decimal PromedioFinal { get; set; }
        public string Estado { get; set; }
    }
} 