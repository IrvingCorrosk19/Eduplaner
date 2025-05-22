using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Dtos;
using SchoolManager.Services.Interfaces;

using SchoolManager.Models;          // SchoolDbContext

namespace SchoolManager.Services
{
    public class TrimesterService : ITrimesterService
    {
        private readonly SchoolDbContext _context;

        public TrimesterService(SchoolDbContext context) => _context = context;

        public async Task<List<TrimesterDto>> GetAllAsync()
        {
            var trimestres = await _context.Trimesters
                .OrderBy(t => t.Name)
                .Select(t => new TrimesterDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    StartDate = t.StartDate.ToDateTime(TimeOnly.MinValue),
                    EndDate = t.EndDate.ToDateTime(TimeOnly.MinValue)
                })
                .ToListAsync();
            return trimestres;
        }

        public async Task GuardarTrimestresAsync(List<TrimesterDto> trimestres)
        {
            foreach (var dto in trimestres)
            {
                var trimestre = new Trimester
                {
                    Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
                    Name = dto.Name,
                    StartDate = DateOnly.FromDateTime(DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Unspecified)),
                    EndDate = DateOnly.FromDateTime(DateTime.SpecifyKind(dto.EndDate, DateTimeKind.Unspecified)),
                    CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                };
                _context.Trimesters.Add(trimestre);
            }
            await _context.SaveChangesAsync();
        }

        public async Task<bool> EditarFechasTrimestreAsync(TrimesterDto dto)
        {
            var trimestre = await _context.Trimesters.FindAsync(dto.Id);
            if (trimestre == null)
                return false;
            trimestre.StartDate = DateOnly.FromDateTime(DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Unspecified));
            trimestre.EndDate = DateOnly.FromDateTime(DateTime.SpecifyKind(dto.EndDate, DateTimeKind.Unspecified));
            await _context.SaveChangesAsync();
            return true;
        }

        

        public async Task EliminarTodosLosTrimestresAsync()
        {
            var trimestres = await _context.Trimesters.ToListAsync();
            _context.Trimesters.RemoveRange(trimestres);
            await _context.SaveChangesAsync();
        }
    }
}
