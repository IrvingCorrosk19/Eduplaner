using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace SchoolManager.Services.Implementations
{
public class GradeLevelService : IGradeLevelService
{
    private readonly SchoolDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GradeLevelService(SchoolDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
            _currentUserService = currentUserService;
    }
    public async Task<GradeLevel?> GetByNameAsync(string name)
    {
        return await _context.GradeLevels
            .FirstOrDefaultAsync(g => g.Name.ToLower() == name.ToLower());
    }
    public async Task<GradeLevel> GetOrCreateAsync(string name)
    {
        name = name.Trim().ToUpper();
        var grade = await _context.GradeLevels.FirstOrDefaultAsync(g => g.Name.ToUpper() == name);
        if (grade == null)
        {
            grade = new GradeLevel
            {
                Id = Guid.NewGuid(),
                Name = name,
                CreatedAt = DateTime.UtcNow
            };
            _context.GradeLevels.Add(grade);
            await _context.SaveChangesAsync();
        }
        return grade;
    }

    public async Task<IEnumerable<GradeLevel>> GetAllAsync()
    {
        return await _context.GradeLevels.ToListAsync();
    }

    public async Task<GradeLevel?> GetByIdAsync(Guid id)
    {
        return await _context.GradeLevels.FindAsync(id);
    }

    public async Task<GradeLevel> CreateAsync(GradeLevel gradeLevel)
    {
        try
        {
            gradeLevel.Id = Guid.NewGuid();
            _context.GradeLevels.Add(gradeLevel);
            await _context.SaveChangesAsync();
            return gradeLevel;
        }
        catch (Exception ex)
        {
            throw new Exception("Error al crear el grado académico", ex);
        }
    }

    public async Task<GradeLevel> UpdateAsync(GradeLevel gradeLevel)
    {
        try
        {
            _context.GradeLevels.Update(gradeLevel);
            await _context.SaveChangesAsync();
            return gradeLevel;
        }
        catch (Exception ex)
        {
            throw new Exception("Error al actualizar el grado académico", ex);
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var entity = await _context.GradeLevels.FindAsync(id);
            if (entity == null) return false;

            _context.GradeLevels.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            throw new Exception("Error al eliminar el grado académico", ex);
            }
        }
    }
}
