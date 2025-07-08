using SchoolManager.Models;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Services.Interfaces;

public class SchoolService : ISchoolService
{
    private readonly SchoolDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SchoolService(SchoolDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<School>> GetAllAsync() =>
        await _context.Schools.ToListAsync();

    public async Task<School?> GetByIdAsync(Guid id) =>
        await _context.Schools.FindAsync(id);

    public async Task CreateAsync(School school)
    {
        _context.Schools.Add(school);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(School school)
    {
        _context.Schools.Update(school);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var school = await _context.Schools.FindAsync(id);
        if (school != null)
        {
            _context.Schools.Remove(school);
            await _context.SaveChangesAsync();
        }
    }
}
