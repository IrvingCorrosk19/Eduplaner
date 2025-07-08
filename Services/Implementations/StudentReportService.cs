using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;
using SchoolManager.Dtos;
using System.Globalization;

namespace SchoolManager.Services.Implementations
{
    public class StudentReportService : IStudentReportService
    {
        private readonly SchoolDbContext _context;
        private readonly IDisciplineReportService _disciplineReportService;
        private readonly ICurrentUserService _currentUserService;

        public StudentReportService(SchoolDbContext context, IDisciplineReportService disciplineReportService, ICurrentUserService currentUserService)
        {
            _context = context;
            _disciplineReportService = disciplineReportService;
            _currentUserService = currentUserService;
        }

        public async Task<StudentReportDto> GetReportByStudentIdAsync(Guid studentId)
        {
            // Obtener todos los trimestres disponibles para este estudiante
            var trimesters = await _context.StudentActivityScores
                .Where(s => s.StudentId == studentId)
                .Join(_context.Activities,
                      score => score.ActivityId,
                      activity => activity.Id,
                      (score, activity) => activity.Trimester)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();

            if (!trimesters.Any())
            {
                return null; // No hay actividades registradas para el estudiante
            }

            // Seleccionar SIEMPRE el primer trimestre disponible (por orden: 1T, 2T, 3T)
            var selectedTrimester = trimesters.FirstOrDefault(t => t == "1T") ??
                                    trimesters.FirstOrDefault(t => t == "2T") ??
                                    trimesters.FirstOrDefault(t => t == "3T");

            // Obtener las actividades del estudiante con la calificación para el trimestre seleccionado
            var studentScores = await _context.StudentActivityScores
                .Where(s => s.StudentId == studentId)
                .Join(_context.Activities,
                      score => score.ActivityId,
                      activity => activity.Id,
                      (score, activity) => new
                      {
                          activity.GradeLevelId,
                          activity.GroupId,
                          activity.SubjectId,
                          activity.Name,
                          activity.Trimester,
                          activity.TeacherId,
                          score.Score,
                          score.CreatedAt
                      })
                .Where(a => a.Trimester == selectedTrimester)
                .ToListAsync();

            if (studentScores == null || !studentScores.Any())
            {
                return null;
            }

            // Obtener el Grado y Grupo del estudiante
            var studentAssignment = await _context.StudentAssignments
                .Where(sa => sa.StudentId == studentId)
                .Join(_context.GradeLevels,
                      sa => sa.GradeId,
                      gl => gl.Id,
                      (sa, gl) => new { sa.GroupId, sa.GradeId, GradeName = gl.Name })
                .Join(_context.Groups,
                      sa => sa.GroupId,
                      g => g.Id,
                      (sa, g) => new { sa.GroupId, GradeName = sa.GradeName, GroupName = g.Name })
                .FirstOrDefaultAsync();

            if (studentAssignment == null)
            {
                return null;
            }

            // Obtener los datos del estudiante
            var studentData = await _context.Users
               .Where(u => u.Id == studentId)
               .Select(u => new { u.Name, u.LastName })
               .FirstOrDefaultAsync();

            if (studentData == null)
            {
                return null;
            }

            var name = $"{studentData.Name} {studentData.LastName}";

            var grades = studentScores.Select(a => new GradeDto
            {
                Subject = _context.Subjects.FirstOrDefault(s => s.Id == a.SubjectId)?.Name ?? "Desconocida",
                Teacher = _context.Users
                    .Where(u => u.Id == a.TeacherId)
                    .Select(u => $"{u.Name ?? "Nombre Desconocido"} {u.LastName ?? "Apellido Desconocido"}")
                    .FirstOrDefault() ?? "Desconocido",
                ActivityName = a.Name,
                Type = _context.Activities.FirstOrDefault(act => act.Name == a.Name && act.TeacherId == a.TeacherId && act.GroupId == a.GroupId && act.SubjectId == a.SubjectId && act.Trimester == a.Trimester)?.Type ?? "SinTipo",
                Value = a.Score,
                CreatedAt = a.CreatedAt.ToUniversalTime(),
                FileUrl = _context.Activities.FirstOrDefault(act => act.Name == a.Name && act.TeacherId == a.TeacherId && act.GroupId == a.GroupId && act.SubjectId == a.SubjectId && act.Trimester == a.Trimester)?.PdfUrl,
                Trimester = a.Trimester
            }).ToList();

            // --- ASISTENCIA POR TRIMESTRE ---
            var trimesterConfig = await _context.Trimesters.FirstOrDefaultAsync(t => t.Name == selectedTrimester);
            var attendanceByTrimester = new List<AttendanceDto>();
            if (trimesterConfig != null)
            {
                var startDate = trimesterConfig.StartDate.ToUniversalTime();
                var endDate = trimesterConfig.EndDate.ToUniversalTime();

                var asistencias = await _context.Attendances
                    .Where(a => 
                        a.StudentId == studentId && 
                        a.Date.ToDateTime(TimeOnly.MinValue) >= startDate && 
                        a.Date.ToDateTime(TimeOnly.MinValue) <= endDate)
                    .ToListAsync();

                attendanceByTrimester.Add(new AttendanceDto
                {
                    Month = trimesterConfig.Name,
                    Present = asistencias.Count(a => a.Status == "present"),
                    Absent = asistencias.Count(a => a.Status == "absent"),
                    Late = asistencias.Count(a => a.Status == "late"),
                    Trimester = trimesterConfig.Name
                });
            }

            // --- ASISTENCIA POR MES ---
            var attendanceByMonth = new List<AttendanceDto>();
            if (trimesterConfig != null)
            {
                var startDate = trimesterConfig.StartDate.ToUniversalTime();
                var endDate = trimesterConfig.EndDate.ToUniversalTime();

                var attendanceByMonthRaw = await _context.Attendances
                    .Where(a => 
                        a.StudentId == studentId && 
                        a.Date.ToDateTime(TimeOnly.MinValue) >= startDate && 
                        a.Date.ToDateTime(TimeOnly.MinValue) <= endDate)
                    .GroupBy(a => new { a.Date.Year, a.Date.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        MonthNumber = g.Key.Month,
                        Present = g.Count(a => a.Status == "present"),
                        Absent = g.Count(a => a.Status == "absent"),
                        Late = g.Count(a => a.Status == "late")
                    })
                    .OrderBy(g => g.Year).ThenBy(g => g.MonthNumber)
                    .ToListAsync();

                attendanceByMonth = attendanceByMonthRaw
                    .Select(g => new AttendanceDto
                    {
                        Month = new DateTime(g.Year, g.MonthNumber, 1).ToString("MMMM", new CultureInfo("es-ES")),
                        Present = g.Present,
                        Absent = g.Absent,
                        Late = g.Late,
                        Trimester = trimesterConfig.Name
                    })
                    .ToList();
            }

            // Obtener los reportes de disciplina
            var disciplineReports = await _disciplineReportService.GetByStudentDtoAsync(studentId, selectedTrimester);

            // Obtener las actividades pendientes
            List<PendingActivityDto> pendingActivities = new();

            try
            {
                pendingActivities = await _context.Activities
                    .Where(a => a.GroupId == studentAssignment.GroupId &&
                                a.Trimester == selectedTrimester &&
                                !_context.StudentActivityScores.Any(s => s.ActivityId == a.Id && s.StudentId == studentId))
                    .Select(a => new PendingActivityDto
                    {
                        ActivityId = a.Id,
                        Name = a.Name,
                        SubjectName = a.Subject.Name,
                        CreatedAt = a.CreatedAt ?? DateTime.UtcNow,
                        FileUrl = a.PdfUrl,
                        TeacherName = $"{a.Teacher.Name} {a.Teacher.LastName}",
                        Type = a.Type ?? "SinTipo"
                    })
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                pendingActivities = new List<PendingActivityDto>();
            }

            return new StudentReportDto
            {
                StudentId = studentId,
                StudentName = name,
                Grade = $"{studentAssignment.GradeName} - {studentAssignment.GroupName}",
                Grades = grades,
                AttendanceByTrimester = attendanceByTrimester,
                AttendanceByMonth = attendanceByMonth,
                Trimester = selectedTrimester,
                AvailableTrimesters = trimesters
                    .Select(t => new AvailableTrimesters { Trimester = t })
                    .ToList(),
                DisciplineReports = disciplineReports,
                PendingActivities = pendingActivities
            };
        }

        public async Task<StudentReportDto> GetReportByStudentIdAndTrimesterAsync(Guid studentId, string trimester)
        {
            // Obtener las actividades del estudiante con las calificaciones para el trimestre seleccionado
            var studentScores = await _context.StudentActivityScores
                .Where(s => s.StudentId == studentId)
                .Join(_context.Activities,
                      score => score.ActivityId,
                      activity => activity.Id,
                      (score, activity) => new
                      {
                          activity.Id,
                          activity.GradeLevelId,
                          activity.GroupId,
                          activity.SubjectId,
                          activity.Name,
                          activity.Trimester,
                          activity.TeacherId,
                          activity.Type,
                          activity.PdfUrl,
                          activity.CreatedAt,
                          activity.Subject,
                          activity.Teacher,
                          Score = score.Score
                      })
                .Where(a => a.Trimester.Trim().ToLower() == trimester.Trim().ToLower())
                .ToListAsync();

            if (studentScores == null || !studentScores.Any())
            {
                return null;
            }

            // Obtener el Grado y Grupo del estudiante
            var studentAssignment = await _context.StudentAssignments
                .Where(sa => sa.StudentId == studentId)
                .Join(_context.GradeLevels,
                      sa => sa.GradeId,
                      gl => gl.Id,
                      (sa, gl) => new { sa.GroupId, sa.GradeId, GradeName = gl.Name })
                .Join(_context.Groups,
                      sa => sa.GroupId,
                      g => g.Id,
                      (sa, g) => new { sa.GroupId, GradeName = sa.GradeName, GroupName = g.Name })
                .FirstOrDefaultAsync();

            if (studentAssignment == null)
            {
                return null;
            }

            // Obtener los datos del estudiante
            var studentData = await _context.Users
                .Where(u => u.Id == studentId)
                .Select(u => new { u.Name, u.LastName })
                .FirstOrDefaultAsync();

            if (studentData == null)
            {
                return null;
            }

            var name = $"{studentData.Name} {studentData.LastName}";

            var grades = studentScores.Select(a => new GradeDto
            {
                Subject = a.Subject?.Name ?? "Desconocida",
                Teacher = a.Teacher != null ? $"{a.Teacher.Name} {a.Teacher.LastName}" : "Desconocido",
                ActivityName = a.Name,
                Type = a.Type ?? "SinTipo",
                Value = a.Score,
                CreatedAt = a.CreatedAt ?? DateTime.UtcNow,
                FileUrl = a.PdfUrl,
                Trimester = a.Trimester
            }).ToList();

            // --- ASISTENCIA POR TRIMESTRE ---
            var trimesterConfig = await _context.Trimesters.FirstOrDefaultAsync(t => t.Name == trimester);
            var attendanceByTrimester = new List<AttendanceDto>();
            if (trimesterConfig != null)
            {
                var startDate = trimesterConfig.StartDate.ToUniversalTime();
                var endDate = trimesterConfig.EndDate.ToUniversalTime();

                var asistencias = await _context.Attendances
                    .Where(a => 
                        a.StudentId == studentId && 
                        a.Date.ToDateTime(TimeOnly.MinValue) >= startDate && 
                        a.Date.ToDateTime(TimeOnly.MinValue) <= endDate)
                    .ToListAsync();

                attendanceByTrimester.Add(new AttendanceDto
                {
                    Month = trimesterConfig.Name,
                    Present = asistencias.Count(a => a.Status == "present"),
                    Absent = asistencias.Count(a => a.Status == "absent"),
                    Late = asistencias.Count(a => a.Status == "late"),
                    Trimester = trimesterConfig.Name
                });
            }

            // --- ASISTENCIA POR MES ---
            var attendanceByMonth = new List<AttendanceDto>();
            if (trimesterConfig != null)
            {
                var startDate = trimesterConfig.StartDate.ToUniversalTime();
                var endDate = trimesterConfig.EndDate.ToUniversalTime();

                var attendanceByMonthRaw = await _context.Attendances
                    .Where(a => 
                        a.StudentId == studentId && 
                        a.Date.ToDateTime(TimeOnly.MinValue) >= startDate && 
                        a.Date.ToDateTime(TimeOnly.MinValue) <= endDate)
                    .GroupBy(a => new { a.Date.Year, a.Date.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        MonthNumber = g.Key.Month,
                        Present = g.Count(a => a.Status == "present"),
                        Absent = g.Count(a => a.Status == "absent"),
                        Late = g.Count(a => a.Status == "late")
                    })
                    .OrderBy(g => g.Year).ThenBy(g => g.MonthNumber)
                    .ToListAsync();

                attendanceByMonth = attendanceByMonthRaw
                    .Select(g => new AttendanceDto
                    {
                        Month = new DateTime(g.Year, g.MonthNumber, 1).ToString("MMMM", new CultureInfo("es-ES")),
                        Present = g.Present,
                        Absent = g.Absent,
                        Late = g.Late,
                        Trimester = trimesterConfig.Name
                    })
                    .ToList();
            }

            // Obtener los reportes de disciplina
            var disciplineReports = await _disciplineReportService.GetByStudentDtoAsync(studentId, trimester);

            // Obtener las actividades pendientes
            List<PendingActivityDto> pendingActivities = new();

            try
            {
                pendingActivities = await _context.Activities
                    .Where(a => a.GroupId == studentAssignment.GroupId &&
                                a.Trimester == trimester &&
                                !_context.StudentActivityScores.Any(s => s.ActivityId == a.Id && s.StudentId == studentId))
                    .Select(a => new PendingActivityDto
                    {
                        ActivityId = a.Id,
                        Name = a.Name,
                        SubjectName = a.Subject.Name,
                        CreatedAt = a.CreatedAt ?? DateTime.UtcNow,
                        FileUrl = a.PdfUrl,
                        TeacherName = $"{a.Teacher.Name} {a.Teacher.LastName}",
                        Type = a.Type ?? "SinTipo"
                    })
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                pendingActivities = new List<PendingActivityDto>();
            }

            return new StudentReportDto
            {
                StudentId = studentId,
                StudentName = name,
                Grade = $"{studentAssignment.GradeName} - {studentAssignment.GroupName}",
                Grades = grades,
                AttendanceByTrimester = attendanceByTrimester,
                AttendanceByMonth = attendanceByMonth,
                Trimester = trimester,
                AvailableTrimesters = new List<AvailableTrimesters> { new AvailableTrimesters { Trimester = trimester } },
                DisciplineReports = disciplineReports,
                PendingActivities = pendingActivities
            };
        }
    }
}


