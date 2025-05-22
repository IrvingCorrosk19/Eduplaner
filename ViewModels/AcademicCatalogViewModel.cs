using System.Collections.Generic;
using SchoolManager.Models;
using SchoolManager.Dtos;

namespace SchoolManager.ViewModels
{
    public class AcademicCatalogViewModel
    {
        public IEnumerable<GradeLevel> GradesLevel { get; set; }
        public IEnumerable<Group> Groups { get; set; }
        public IEnumerable<Subject> Subjects { get; set; }
        public IEnumerable<Specialty> Specialties { get; set; }
        public IEnumerable<Area> Areas { get; set; }
        public List<TrimesterDto> Trimestres { get; set; }
    }
}
