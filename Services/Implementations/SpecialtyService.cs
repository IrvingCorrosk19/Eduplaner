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
    public class SpecialtyService : ISpecialtyService
    {
        private readonly SchoolDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public SpecialtyService(SchoolDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Specialty> GetOrCreateAsync(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("El nombre de la especialidad no puede estar vacío.", nameof(name));

                name = name.Trim().ToUpper();
                var specialty = await _context.Specialties
                    .FirstOrDefaultAsync(e => e.Name.ToUpper() == name);

                if (specialty == null)
                {
                    specialty = new Specialty
                    {
                        Id = Guid.NewGuid(),
                        Name = name,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Specialties.Add(specialty);
                    await _context.SaveChangesAsync();
                }

                return specialty;
            }
            catch (DbUpdateException ex)
            {
                throw new Exception($"No se pudo guardar la especialidad: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al procesar la especialidad: {ex.Message}", ex);
            }
        }

        public async Task<List<Specialty>> GetAllAsync()
        {
            return await _context.Specialties.ToListAsync();
        }

        public async Task<Specialty?> GetByIdAsync(Guid id)
        {
            return await _context.Specialties.FindAsync(id);
        }

        public async Task<Specialty> CreateAsync(Specialty specialty)
        {
            if (specialty == null || string.IsNullOrWhiteSpace(specialty.Name))
                throw new ArgumentException("La especialidad no es válida.");

            specialty.Id = Guid.NewGuid();
            specialty.CreatedAt = DateTime.UtcNow;
            specialty.Name = specialty.Name.Trim();
            specialty.Description = specialty.Description?.Trim();

            _context.Specialties.Add(specialty);
            await _context.SaveChangesAsync();

            return specialty;
        }

        public async Task<Specialty> UpdateAsync(Specialty specialty)
        {
            if (specialty == null || string.IsNullOrWhiteSpace(specialty.Name))
                throw new ArgumentException("La especialidad no es válida.");

            var existing = await _context.Specialties.FindAsync(specialty.Id);
            if (existing == null)
                throw new InvalidOperationException("Especialidad no encontrada.");

            existing.Name = specialty.Name.Trim();
            existing.Description = specialty.Description?.Trim();

            await _context.SaveChangesAsync();

            return existing;
        }

        public async Task DeleteAsync(Guid id)
        {
            var specialty = await _context.Specialties.FindAsync(id);
            if (specialty != null)
            {
                _context.Specialties.Remove(specialty);
                await _context.SaveChangesAsync();
            }
        }
    }
}
