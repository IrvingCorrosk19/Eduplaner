using Microsoft.EntityFrameworkCore;

using SchoolManager.Models;
using SchoolManager.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchoolManager.Services.Implementations
{
    public class StudentAssignmentService : IStudentAssignmentService
    {
        private readonly SchoolDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public StudentAssignmentService(SchoolDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }
        public async Task InsertAsync(StudentAssignment assignment)
        {
            if (assignment == null)
                throw new ArgumentNullException(nameof(assignment), "La asignación no puede ser null.");

            try
            {
                Console.WriteLine($"[StudentAssignmentService] Iniciando inserción para StudentId: {assignment.StudentId}, GradeId: {assignment.GradeId}, GroupId: {assignment.GroupId}");
                
                assignment.CreatedAt = DateTime.UtcNow;
                Console.WriteLine($"[StudentAssignmentService] CreatedAt establecido: {assignment.CreatedAt}");
                
                _context.StudentAssignments.Add(assignment);
                Console.WriteLine($"[StudentAssignmentService] Entidad agregada al contexto");
                
                await _context.SaveChangesAsync();
                Console.WriteLine($"[StudentAssignmentService] SaveChangesAsync completado exitosamente");
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"[StudentAssignmentService] DbUpdateException: {dbEx.Message}");
                Console.WriteLine($"[StudentAssignmentService] Inner Exception: {dbEx.InnerException?.Message}");
                // Excepción típica de clave foránea, clave primaria duplicada, etc.
                throw new InvalidOperationException($"Error al guardar la asignación en la base de datos. Verifica claves foráneas y datos duplicados. Detalles: {dbEx.Message}", dbEx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StudentAssignmentService] Exception general: {ex.Message}");
                Console.WriteLine($"[StudentAssignmentService] Stack Trace: {ex.StackTrace}");
                // Otro tipo de excepción general
                throw new Exception($"Ocurrió un error inesperado al insertar la asignación. Detalles: {ex.Message}", ex);
            }
        }


        public async Task<bool> ExistsAsync(Guid studentId, Guid gradeId, Guid groupId)
        {
            if (studentId == Guid.Empty || gradeId == Guid.Empty || groupId == Guid.Empty)
                return false;

            return await _context.StudentAssignments.AnyAsync(sa =>
                sa.StudentId == studentId &&
                sa.GradeId == gradeId &&
                sa.GroupId == groupId);
        }


        public async Task<List<StudentAssignment>> GetAssignmentsByStudentIdAsync(Guid studentId)
        {
            return await _context.StudentAssignments
                .Where(sa => sa.StudentId == studentId)
                .ToListAsync();
        }

        public async Task AssignAsync(Guid studentId, List<(Guid SubjectId, Guid GradeId, Guid GroupId)> assignments)
        {
            try
            {
                Console.WriteLine($"[StudentAssignmentService] Iniciando AssignAsync para StudentId: {studentId}");
                
                var existing = await _context.StudentAssignments
                    .Where(a => a.StudentId == studentId)
                    .ToListAsync();

                Console.WriteLine($"[StudentAssignmentService] Encontradas {existing.Count} asignaciones existentes");

                _context.StudentAssignments.RemoveRange(existing);

                foreach (var item in assignments)
                {
                    Console.WriteLine($"[StudentAssignmentService] Agregando asignación: GradeId={item.GradeId}, GroupId={item.GroupId}");
                    
                    _context.StudentAssignments.Add(new StudentAssignment
                    {
                        Id = Guid.NewGuid(),
                        StudentId = studentId,
                        GradeId = item.GradeId,
                        GroupId = item.GroupId,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                Console.WriteLine($"[StudentAssignmentService] Guardando cambios...");
                await _context.SaveChangesAsync();
                Console.WriteLine($"[StudentAssignmentService] AssignAsync completado exitosamente");
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"[StudentAssignmentService] DbUpdateException en AssignAsync: {dbEx.Message}");
                Console.WriteLine($"[StudentAssignmentService] InnerException: {dbEx.InnerException?.Message}");
                throw new InvalidOperationException($"Error al asignar estudiantes. Detalles: {dbEx.Message}", dbEx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StudentAssignmentService] Exception general en AssignAsync: {ex.Message}");
                Console.WriteLine($"[StudentAssignmentService] StackTrace: {ex.StackTrace}");
                throw new Exception($"Error inesperado al asignar estudiantes. Detalles: {ex.Message}", ex);
            }
        }

        public async Task RemoveAssignmentsAsync(Guid studentId)
        {
            var assignments = await _context.StudentAssignments
                .Where(a => a.StudentId == studentId)
                .ToListAsync();

            _context.StudentAssignments.RemoveRange(assignments);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> AssignStudentAsync(Guid studentId, Guid subjectId, Guid gradeId, Guid groupId)
        {
            try
            {
                Console.WriteLine($"[StudentAssignmentService] AssignStudentAsync - StudentId: {studentId}, GradeId: {gradeId}, GroupId: {groupId}");
                
                bool exists = await _context.StudentAssignments.AnyAsync(sa =>
                    sa.StudentId == studentId &&
                    sa.GradeId == gradeId &&
                    sa.GroupId == groupId
                );

                if (exists)
                {
                    Console.WriteLine($"[StudentAssignmentService] La asignación ya existe");
                    return false;
                }

                var assignment = new StudentAssignment
                {
                    Id = Guid.NewGuid(),
                    StudentId = studentId,
                    GradeId = gradeId,
                    GroupId = groupId,
                    CreatedAt = DateTime.UtcNow
                };

                Console.WriteLine($"[StudentAssignmentService] Nueva asignación creada con CreatedAt: {assignment.CreatedAt}");
                
                _context.StudentAssignments.Add(assignment);
                await _context.SaveChangesAsync();
                
                Console.WriteLine($"[StudentAssignmentService] Asignación guardada exitosamente");
                return true;
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"[StudentAssignmentService] DbUpdateException en AssignStudentAsync: {dbEx.Message}");
                Console.WriteLine($"[StudentAssignmentService] Inner Exception: {dbEx.InnerException?.Message}");
                throw new InvalidOperationException($"Error al asignar estudiante. Detalles: {dbEx.Message}", dbEx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StudentAssignmentService] Exception general en AssignStudentAsync: {ex.Message}");
                Console.WriteLine($"[StudentAssignmentService] Stack Trace: {ex.StackTrace}");
                throw new Exception($"Error inesperado al asignar estudiante. Detalles: {ex.Message}", ex);
            }
        }

        public async Task BulkAssignFromFileAsync(List<(string StudentEmail, string SubjectCode, string GradeName, string GroupName)> rows)
        {
            foreach (var row in rows)
            {
                var student = await _context.Users.FirstOrDefaultAsync(u => u.Email == row.StudentEmail);
                var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Code == row.SubjectCode);
                var grade = await _context.GradeLevels.FirstOrDefaultAsync(g => g.Name == row.GradeName);
                var group = await _context.Groups.FirstOrDefaultAsync(g => g.Name == row.GroupName && g.Grade == row.GradeName);

                if (student == null || subject == null || grade == null || group == null)
                {
                    // puedes loggear error con detalles aquí
                    continue;
                }

                bool alreadyExists = await _context.StudentAssignments.AnyAsync(sa =>
                    sa.StudentId == student.Id &&
                    sa.GradeId == grade.Id &&
                    sa.GroupId == group.Id);

                if (!alreadyExists)
                {
                    _context.StudentAssignments.Add(new StudentAssignment
                    {
                        Id = Guid.NewGuid(),
                        StudentId = student.Id,
                        GradeId = grade.Id,
                        GroupId = group.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

    }
}
