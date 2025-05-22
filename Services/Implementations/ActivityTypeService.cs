// Services/ActivityTypeService.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Dtos;
using SchoolManager.Interfaces;
using SchoolManager.Models;   // SchoolDbContext, ActivityTypes

namespace SchoolManager.Services.Implementations
{
    /// <summary>
    /// Devuelve la lista de tipos de actividad (tarea, parcial, examen…).
    /// </summary>
    public class ActivityTypeService : IActivityTypeService
    {
        private readonly SchoolDbContext _context;

        public ActivityTypeService(SchoolDbContext context) => _context = context;

        public async Task<IEnumerable<ActivityTypeDto>> GetAllAsync()
        {
            return await _context.ActivityTypes
                .OrderBy(t => t.Name)
                .Select(t => new ActivityTypeDto
                {
                    Id = t.Id,
                    Name = t.Name
                })
                .ToListAsync();
        }
    }
}
