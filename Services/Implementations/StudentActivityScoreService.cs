using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Dtos;
using SchoolManager.Interfaces;
using SchoolManager.Models;

namespace SchoolManager.Services
{
    public class StudentActivityScoreService : IStudentActivityScoreService
    {
        private readonly SchoolDbContext _context;
        public StudentActivityScoreService(SchoolDbContext context) => _context = context;

        /* ------------ 1. Guardar / actualizar notas ------------ */
        public async Task SaveAsync(IEnumerable<StudentActivityScoreCreateDto> scores)
        {
            foreach (var dto in scores)
            {
                var entity = await _context.StudentActivityScores
                    .FirstOrDefaultAsync(s => s.StudentId == dto.StudentId &&
                                              s.ActivityId == dto.ActivityId);

                if (entity is null)
                {
                    _context.StudentActivityScores.Add(new StudentActivityScore
                    {
                        Id = Guid.NewGuid(),
                        StudentId = dto.StudentId,
                        ActivityId = dto.ActivityId,
                        Score = dto.Score,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    entity.Score = dto.Score;
                }
            }
            await _context.SaveChangesAsync();
        }

        /* ------------ 2. Libro de calificaciones pivotado ------------ */
        public async Task<GradeBookDto> GetGradeBookAsync(Guid teacherId, Guid groupId, string trimesterCode)
        {
            /* 2.1 Cabeceras: actividades del docente en ese grupo y trimestre */
            var headers = await _context.Activities
                .Where(a => a.TeacherId == teacherId &&
                            a.GroupId == groupId &&
                            a.Trimester == trimesterCode)
                .OrderBy(a => a.CreatedAt)
                .Select(a => new ActivityHeaderDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Type = a.Type,
                    Date = a.CreatedAt,
                    HasPdf = a.PdfUrl != null,
                    PdfUrl = a.PdfUrl
                })
                .ToListAsync();

            // Ajustar el tipo de fecha y valor por defecto después de traer los datos a memoria
            foreach (var h in headers)
            {
                h.Date = h.Date.HasValue
                    ? DateTime.SpecifyKind(h.Date.Value, DateTimeKind.Unspecified)
                    : DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            }

            var activityIds = headers.Select(h => h.Id).ToList();

            /* 2.2 Estudiantes asignados a ese grupo (StudentAssignments) */
            var studentIds = await _context.StudentAssignments
                .Where(sa => sa.GroupId == groupId)
                .Select(sa => sa.StudentId)
                .Distinct()
                .ToListAsync();

            var students = await _context.Students
                .Where(s => studentIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Name })
                .ToListAsync();

            /* 2.3 Notas existentes */
            var scores = await _context.StudentActivityScores
                .Where(s => activityIds.Contains(s.ActivityId))
                .ToListAsync();

            /* 2.4 Pivotar alumnos × actividades */
            var rows = students.Select(stu =>
            {
                var dict = new Dictionary<Guid, decimal?>();
                foreach (var hdr in headers)
                {
                    var score = scores.FirstOrDefault(x =>
                        x.StudentId == stu.Id && x.ActivityId == hdr.Id);
                    dict[hdr.Id] = score?.Score;
                }

                return new StudentGradeRowDto
                {
                    StudentId = stu.Id,
                    StudentName = stu.Name,
                    ScoresByActivity = dict
                };
            });

            return new GradeBookDto { Activities = headers, Rows = rows };
        }


        public async Task SaveBulkFromNotasAsync(List<StudentActivityScoreCreateDto> registros)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var dto in registros)
                {
                    // Buscar o crear la actividad por nombre, docente, grupo, trimestre y grado
                    var activity = await _context.Activities
                        .FirstOrDefaultAsync(a =>
                            a.Name == dto.ActivityName &&
                            a.TeacherId == dto.TeacherId &&
                            a.Trimester == dto.Trimester &&
                             a.SubjectId == dto.SubjectId &&
                             a.GroupId == dto.GroupId &&
                            a.GradeLevelId == dto.GradeLevelId &&
                            a.Type == dto.Type);

                    // Si no existe, la creamos
                    if (activity == null)
                    {
                        activity = new Activity
                        {
                            Id = Guid.NewGuid(),
                            Name = dto.ActivityName,
                            Type = dto.Type,
                            TeacherId = dto.TeacherId,
                            SubjectId = dto.SubjectId,
                            GroupId = dto.GroupId,
                            GradeLevelId = dto.GradeLevelId,
                            Trimester = dto.Trimester,
                            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                        };

                        _context.Activities.Add(activity);
                        await _context.SaveChangesAsync();
                    }

                    // Verificamos si ya hay nota para ese alumno y esa actividad
                    var existing = await _context.StudentActivityScores
                        .FirstOrDefaultAsync(s =>
                            s.StudentId == dto.StudentId &&
                            s.ActivityId == activity.Id);

                    if (existing == null)
                    {
                        // Si no existe, lo añadimos
                        _context.StudentActivityScores.Add(new StudentActivityScore
                        {
                            Id = Guid.NewGuid(),
                            StudentId = dto.StudentId,
                            ActivityId = activity.Id,
                            Score = dto.Score,
                            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                        });
                    }
                    else
                    {
                        // Si ya existe, actualizamos la nota
                        existing.Score = dto.Score;
                    }
                }

                // Guardamos todos los cambios a la base de datos
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Manejamos el error
                Console.WriteLine("❌ Error guardando notas en bloque:");
                Console.WriteLine($"Mensaje: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                throw new Exception($"Error al guardar las notas: {ex.Message}", ex);
            }
        }

        public async Task<List<StudentNotaDto>> GetNotasPorFiltroAsync(GetNotesDto notes)
        {
            // Obtener las notas existentes
            var notas = await _context.StudentActivityScores
                .Include(sa => sa.Activity)
                .Include(sa => sa.Student)
                .Where(sa =>
                    sa.Activity.TeacherId == notes.TeacherId &&
                    sa.Activity.SubjectId == notes.SubjectId &&
                    sa.Activity.GroupId == notes.GroupId &&
                    sa.Activity.GradeLevelId == notes.GradeLevelId &&
                    sa.Activity.Trimester == notes.Trimester)
                .ToListAsync();

            // Agrupar las notas por estudiante
            var resultado = notas
                .GroupBy(n => n.StudentId)
                .Select(g => new StudentNotaDto
                {
                    StudentId = g.Key.ToString(),
                    TeacherId = notes.TeacherId.ToString(),
                    SubjectId = notes.SubjectId.ToString(),
                    GroupId = notes.GroupId.ToString(),
                    GradeLevelId = notes.GradeLevelId.ToString(),
                    Trimester = notes.Trimester,
                    Notas = g.Select(n => new NotaDetalleDto
                    {
                        Tipo = n.Activity.Type,
                        Actividad = n.Activity.Name,
                        Nota = n.Score.ToString("0.00")
                    }).ToList()
                })
                .ToList();

            return resultado;
        }

        public async Task<List<PromedioFinalDto>> GetPromediosFinalesAsync(GetNotesDto notes)
        {
            // Obtener todas las notas del grupo y materia incluyendo el tipo de actividad
            var notasPorTrimestre = await _context.StudentActivityScores
                .Join(_context.Activities,
                    score => score.ActivityId,
                    activity => activity.Id,
                    (score, activity) => new
                    {
                        StudentId = score.StudentId,
                        Score = score.Score,
                        Trimester = activity.Trimester,
                        ActivityType = activity.Type,
                        SubjectId = activity.SubjectId,
                        GroupId = activity.GroupId,
                        GradeLevelId = activity.GradeLevelId
                    })
                .Where(x => x.SubjectId == notes.SubjectId &&
                           x.GroupId == notes.GroupId &&
                           x.GradeLevelId == notes.GradeLevelId)
                .ToListAsync();

            // Obtener información de los estudiantes
            var studentIds = notasPorTrimestre.Select(x => x.StudentId).Distinct();
            var students = await _context.Users
                .Where(u => studentIds.Contains(u.Id))
                .OrderBy(u => u.Name)  // Ordenar por nombre
                .Select(u => new { u.Id, u.Name })
                .ToListAsync();

            // Agrupar por estudiante
            var promedios = students.Select(student =>
            {
                var promediosPorTrimestre = notasPorTrimestre
                    .Where(n => n.StudentId == student.Id)
                    .GroupBy(n => n.Trimester)
                    .ToDictionary(
                        g => g.Key,
                        g =>
                        {
                            // Calcular promedio por tipo de actividad
                            var promediosPorTipo = g.GroupBy(n => n.ActivityType)
                                .Select(t => t.Average(n => n.Score))
                                .ToList();

                            // Promedio final del trimestre (promedio de los promedios por tipo)
                            return promediosPorTipo.Any() 
                                ? Math.Floor(promediosPorTipo.Average() * 10) / 10 
                                : 0.0m;
                        });

                var promedioFinal = promediosPorTrimestre.Any()
                    ? Math.Floor(promediosPorTrimestre.Values.Average() * 10) / 10
                    : 0.0m;

                return new PromedioFinalDto
                {
                    StudentId = student.Id.ToString(),
                    StudentFullName = student.Name,
                    Promedios = promediosPorTrimestre,
                    PromedioFinal = promedioFinal,
                    Estado = promedioFinal >= 3.0m ? "Aprobado" : "Reprobado"
                };
            }).ToList();

            return promedios;
        }

    }






}

