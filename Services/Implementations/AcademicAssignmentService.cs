using Microsoft.EntityFrameworkCore;
using SchoolManager.Application.Interfaces;
using SchoolManager.Models;
using SchoolManager.ViewModels;

namespace SchoolManager.Infrastructure.Services
{
    public class AcademicAssignmentService : IAcademicAssignmentService
    {
        private readonly SchoolDbContext _context;

        public AcademicAssignmentService(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AssignTeacherAsync(Guid teacherId, Guid subjectAssignmentId)
        {
            if (teacherId == Guid.Empty || subjectAssignmentId == Guid.Empty)
                throw new ArgumentException("Los IDs de docente y asignación académica no pueden estar vacíos.");

            try
            {
                var exists = await _context.TeacherAssignments.AnyAsync(a =>
                    a.TeacherId == teacherId &&
                    a.SubjectAssignmentId == subjectAssignmentId);

                if (exists)
                    return false;

                var assignment = new TeacherAssignment
                {
                    Id = Guid.NewGuid(),
                    TeacherId = teacherId,
                    SubjectAssignmentId = subjectAssignmentId,
                    CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                };

                await _context.TeacherAssignments.AddAsync(assignment);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (DbUpdateException ex)
            {
                // Posible error de restricción de clave foránea o única
                throw new InvalidOperationException("Error al guardar la asignación. Verifica que los datos sean válidos.", ex);
            }
            catch (Exception ex)
            {
                // Error genérico
                throw new Exception("Ocurrió un error inesperado al asignar al docente.", ex);
            }
        }

        public async Task<List<TeacherAssignmentRequest>> GetAssignmentsByTeacherAsync(Guid teacherId)
        {
            var groupedAssignments = await _context.TeacherAssignments
                .Where(a => a.TeacherId == teacherId)
                .Include(a => a.SubjectAssignment)
                .GroupBy(a => new
                {
                    a.SubjectAssignment.SubjectId,
                    a.SubjectAssignment.GradeLevelId
                })
                .Select(g => new TeacherAssignmentRequest
                {
                    UserId = teacherId,
                    SubjectId = g.Key.SubjectId,
                    GradeId = g.Key.GradeLevelId,
                    GroupIds = g.Select(x => x.SubjectAssignment.GroupId).ToList()
                })
                .ToListAsync();

            return groupedAssignments;
        }

        public async Task<bool> ExisteAsignacionAsync(Guid specialtyId, Guid areaId, Guid subjectId, Guid gradeLevelId, Guid groupId)
        {
            return await _context.SubjectAssignments.AnyAsync(sa =>
                sa.SpecialtyId == specialtyId &&
                sa.AreaId == areaId &&
                sa.SubjectId == subjectId &&
                sa.GradeLevelId == gradeLevelId &&
                sa.GroupId == groupId
            );
        }

        public async Task CreateAsignacionAsync(Guid specialtyId, Guid areaId, Guid subjectId, Guid gradeLevelId, Guid groupId)
        {
            var asignacion = new SubjectAssignment
            {
                Id = Guid.NewGuid(),
                SpecialtyId = specialtyId,
                AreaId = areaId,
                SubjectId = subjectId,
                GradeLevelId = gradeLevelId,
                GroupId = groupId,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
            };

            _context.SubjectAssignments.Add(asignacion);
            await _context.SaveChangesAsync();
        }

        public async Task<Guid?> GetSubjectAssignmentIdAsync(Guid specialtyId, Guid areaId, Guid subjectId, Guid gradeLevelId, Guid groupId)
        {
            return await _context.SubjectAssignments
                .Where(sa =>
                    sa.SpecialtyId == specialtyId &&
                    sa.AreaId == areaId &&
                    sa.SubjectId == subjectId &&
                    sa.GradeLevelId == gradeLevelId &&
                    sa.GroupId == groupId)
                .Select(sa => (Guid?)sa.Id)
                .FirstOrDefaultAsync();
        }
    }
}
