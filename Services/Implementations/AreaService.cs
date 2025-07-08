using Microsoft.EntityFrameworkCore;
using SchoolManager.Application.Interfaces;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace SchoolManager.Infrastructure.Services
{
    public class AreaService : IAreaService
    {
        private readonly SchoolDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public AreaService(SchoolDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Area> GetOrCreateAsync(string name)
        {
            name = name.Trim().ToUpper();

            var area = await _context.Areas
                .FirstOrDefaultAsync(a => a.Name.ToUpper() == name);

            if (area == null)
            {
                area = new Area
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Areas.Add(area);
                await _context.SaveChangesAsync();
            }

            return area;
        }

        public async Task<List<Area>> GetAllAsync()
        {
  
            return await _context.Areas.ToListAsync();
        }

        public async Task<Area?> GetByIdAsync(Guid id)
        {
            return await _context.Areas.FindAsync(id);
        }

        public async Task<Area> CreateAsync(Area area)
        {
            if (string.IsNullOrWhiteSpace(area.Name))
                throw new ArgumentException("El nombre del área es obligatorio.");

            area.Id = Guid.NewGuid();
            area.CreatedAt = DateTime.UtcNow;
            area.Name = area.Name.Trim();
            area.Description = area.Description?.Trim();

            _context.Areas.Add(area);
            await _context.SaveChangesAsync();

            return area;
        }

        public async Task<Area> UpdateAsync(Area area)
        {
            if (string.IsNullOrWhiteSpace(area.Name))
                throw new ArgumentException("El nombre del área es obligatorio.");

            var existing = await _context.Areas.FindAsync(area.Id);
            if (existing == null)
                throw new InvalidOperationException("Área no encontrada.");

            existing.Name = area.Name.Trim();
            existing.Description = area.Description?.Trim();

            await _context.SaveChangesAsync();

            return existing;
        }

        public async Task DeleteAsync(Guid id)
        {
            var area = await _context.Areas.FindAsync(id);
            if (area != null)
            {
                _context.Areas.Remove(area);
                await _context.SaveChangesAsync();
            }
        }
    }
}
