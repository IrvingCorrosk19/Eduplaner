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

        public StudentAssignmentService(SchoolDbContext context)
        {
            _context = context;
        }
        public async Task InsertAsync(StudentAssignment assignment)
        {
            if (assignment == null)
                throw new ArgumentNullException(nameof(assignment), "La asignación no puede ser null.");

            try
            {
                assignment.CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
                _context.StudentAssignments.Add(assignment);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                // Excepción típica de clave foránea, clave primaria duplicada, etc.
                throw new InvalidOperationException("Error al guardar la asignación en la base de datos. Verifica claves foráneas y datos duplicados.", dbEx);
            }
            catch (Exception ex)
            {
                // Otro tipo de excepción general
                throw new Exception("Ocurrió un error inesperado al insertar la asignación.", ex);
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
            var existing = await _context.StudentAssignments
                .Where(a => a.StudentId == studentId)
                .ToListAsync();

            _context.StudentAssignments.RemoveRange(existing);

            foreach (var item in assignments)
            {
                _context.StudentAssignments.Add(new StudentAssignment
                {
                    Id = Guid.NewGuid(),
                    StudentId = studentId,
                    GradeId = item.GradeId,
                    GroupId = item.GroupId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
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
            bool exists = await _context.StudentAssignments.AnyAsync(sa =>
                sa.StudentId == studentId &&
                sa.GradeId == gradeId &&
                sa.GroupId == groupId
            );

            if (exists)
                return false;

            var assignment = new StudentAssignment
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                GradeId = gradeId,
                GroupId = groupId,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
            };

            _context.StudentAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            return true;
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
                        CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

    }
}
