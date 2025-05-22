using SchoolManager.Models;
using Microsoft.EntityFrameworkCore;

public class SecuritySettingService : ISecuritySettingService
{
    private readonly SchoolDbContext _context;

    public SecuritySettingService(SchoolDbContext context)
    {
        _context = context;
    }

    public async Task<List<SecuritySetting>> GetAllAsync() =>
        await _context.SecuritySettings.ToListAsync();

    public async Task<SecuritySetting?> GetBySchoolIdAsync(Guid schoolId)
    {
        return await _context.SecuritySettings
            .FirstOrDefaultAsync(s => s.SchoolId == schoolId);
    }

    public async Task CreateAsync(SecuritySetting setting)
    {
        _context.SecuritySettings.Add(setting);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(SecuritySetting setting)
    {
        _context.SecuritySettings.Update(setting);
        await _context.SaveChangesAsync();
    }
}
