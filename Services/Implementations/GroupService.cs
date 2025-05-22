using SchoolManager.Models;
using Microsoft.EntityFrameworkCore;
public class GroupService : IGroupService
{
    private readonly SchoolDbContext _context;

    public GroupService(SchoolDbContext context)
    {
        _context = context;
    }
    public async Task<Group?> GetByNameAndGradeAsync(string groupName)
    {
        return await _context.Groups
            .FirstOrDefaultAsync(g =>
                g.Name.ToLower() == groupName.ToLower());
    }
    public async Task<Group> GetOrCreateAsync(string name)
    {
        name = name.Trim().ToUpper();
        var group = await _context.Groups.FirstOrDefaultAsync(g => g.Name.ToUpper() == name);
        if (group == null)
        {
            group = new Group
            {
                Id = Guid.NewGuid(),
                Name = name,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
            };
            _context.Groups.Add(group);
            await _context.SaveChangesAsync();
        }
        return group;
    }

    public async Task<List<Group>> GetAllAsync() =>
        await _context.Groups.ToListAsync();

    public async Task<Group?> GetByIdAsync(Guid id) =>
        await _context.Groups.FindAsync(id);

    public async Task<Group> CreateAsync(Group group)
    {
        try
        {
            group.Id = Guid.NewGuid();
            group.CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            return group;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al crear el grupo: {ex.Message}", ex);
        }
    }


    public async Task UpdateAsync(Group group)
    {
        _context.Groups.Update(group);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        try
        {
            var group = await _context.Groups.FindAsync(id);
            if (group != null)
            {
                _context.Groups.Remove(group);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            // Puedes registrar el error aquí si tienes un logger, por ejemplo:
            // _logger.LogError(ex, "Error al eliminar el grupo con ID: {id}", id);
            throw new Exception("Error al eliminar el grupo.", ex);
        }
    }

}
