using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Dtos;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Services
{
    public class DisciplineReportService : IDisciplineReportService
    {
        private readonly SchoolDbContext _context;

        public DisciplineReportService(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<DisciplineReport>> GetAllAsync() =>
            await _context.DisciplineReports.ToListAsync();

        public async Task<DisciplineReport?> GetByIdAsync(Guid? id)
        {
            if (!id.HasValue)
                return null;
            
            return await _context.DisciplineReports.FindAsync(id.Value);
        }

        public async Task CreateAsync(DisciplineReport report)
        {
            report.CreatedAt = DateTime.UtcNow;
            _context.DisciplineReports.Add(report);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(DisciplineReport report)
        {
            report.UpdatedAt = DateTime.UtcNow;
            _context.DisciplineReports.Update(report);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var report = await _context.DisciplineReports.FindAsync(id);
            if (report != null)
            {
                _context.DisciplineReports.Remove(report);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<DisciplineReport>> GetByStudentAsync(Guid studentId)
        {
            return await _context.DisciplineReports
                .Where(r => r.StudentId == studentId)
                .ToListAsync();
        }

        public async Task<List<DisciplineReport>> GetFilteredAsync(DateTime? fechaInicio, DateTime? fechaFin, Guid? gradoId, Guid? groupId = null, Guid? studentId = null)
        {
            var query = _context.DisciplineReports
                .Include(r => r.Student)
                .Include(r => r.Teacher)
                .Include(r => r.Group)
                .Include(r => r.GradeLevel)
                .AsQueryable();

            // Filtros obligatorios
            if (gradoId.HasValue)
            {
                query = query.Where(r => r.GradeLevelId == gradoId);
            }

            // Filtros opcionales
            if (groupId.HasValue)
            {
                query = query.Where(r => r.GroupId == groupId);
            }

            if (studentId.HasValue)
            {
                query = query.Where(r => r.StudentId == studentId);
            }

            if (fechaInicio.HasValue)
            {
                var startDate = fechaInicio.Value.Date.ToUniversalTime();
                query = query.Where(r => r.Date >= startDate);
            }

            if (fechaFin.HasValue)
            {
                var endDate = fechaFin.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
                query = query.Where(r => r.Date <= endDate);
            }

            return await query
                .OrderByDescending(r => r.Date)
                .ThenByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<DisciplineReportDto>> GetByStudentDtoAsync(Guid studentId, string trimester = null)
        {
            var query = _context.DisciplineReports
                .Include(r => r.Teacher)
                .Where(r => r.StudentId == studentId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(trimester))
            {
                var trimesterInfo = await _context.Trimesters
                    .FirstOrDefaultAsync(t => t.Name == trimester);

                if (trimesterInfo != null)
                {
                    var startDate = trimesterInfo.StartDate.ToUniversalTime();
                    var endDate = trimesterInfo.EndDate.ToUniversalTime();
                    query = query.Where(r => r.Date >= startDate && r.Date <= endDate);
                }
            }

            var reports = await query
                .OrderByDescending(r => r.Date)
                .ThenByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reports.Select(r => new DisciplineReportDto
            {
                Type = r.ReportType,
                Status = r.Status,
                Description = r.Description,
                Date = r.Date,
                Teacher = r.Teacher != null ? $"{r.Teacher.Name} {r.Teacher.LastName}" : null
            }).ToList();
        }
    }
}
