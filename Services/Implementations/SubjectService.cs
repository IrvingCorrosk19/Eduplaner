using SchoolManager.Models;
using Microsoft.EntityFrameworkCore;

public class SubjectService : ISubjectService
{
    private readonly SchoolDbContext _context;

    public SubjectService(SchoolDbContext context)
    {
        _context = context;
    }


    public async Task<Subject?> GetByCodeAsync(string code)
    {
        return await _context.Subjects.FirstOrDefaultAsync(s => s.Code.ToLower() == code.ToLower());
    }

    public async Task<List<SubjectAssignment>> GetSubjectAssignmentsByGradeAndGroupAsync(Guid gradeId, Guid groupId)
    {
        return await _context.SubjectAssignments
            .Include(sa => sa.Subject)
            .Include(sa => sa.GradeLevel)
            .Include(sa => sa.Group)
            .Include(sa => sa.Area)
            .Include(sa => sa.Specialty)
            .Where(sa => sa.GradeLevelId == gradeId && sa.GroupId == groupId)
            .ToListAsync();
    }


    public async Task<Subject> GetOrCreateAsync(string name)
    {
        name = name.Trim().ToUpper();
        var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Name.ToUpper() == name);
        if (subject == null)
        {
            subject = new Subject
            {
                Id = Guid.NewGuid(),
                Name = name
            };
            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();
        }
        return subject;
    }

    public async Task<List<Subject>> GetAllAsync() =>
        await _context.Subjects.ToListAsync();

    public async Task<Subject?> GetByIdAsync(Guid id) =>
        await _context.Subjects.FindAsync(id);

    public async Task<Subject> CreateAsync(Subject subject)
    {
        // guardar en la base de datos
        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync();
        return subject;
    }

    public async Task<Subject> UpdateAsync(Subject subject)
    {
        _context.Subjects.Update(subject);
        await _context.SaveChangesAsync();
        return subject;
    }


    public async Task DeleteAsync(Guid id)
    {
        try
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject != null)
            {
                _context.Subjects.Remove(subject);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            // Puedes registrar el error si tienes un sistema de logging, por ejemplo:
            // _logger.LogError(ex, "Error al eliminar la materia con ID: {id}", id);
            throw new Exception("Error al eliminar la materia.", ex);
        }
    }

}
