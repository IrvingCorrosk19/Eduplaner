using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Dtos;
using SchoolManager.Interfaces;
using SchoolManager.Models;

namespace SchoolManager.Services
{
    public class TeacherGroupService : ITeacherGroupService
    {
        private readonly SchoolDbContext _context;
        public TeacherGroupService(SchoolDbContext context) => _context = context;

        public async Task<IEnumerable<GroupDto>> GetByTeacherAsync(Guid teacherId, string trimesterCode)
        {
            return await _context.TeacherAssignments
                .Where(ta => ta.TeacherId == teacherId)
                .Include(ta => ta.SubjectAssignment)      // 1er salto
                    .ThenInclude(sa => sa.Group)          // 2º salto
                .Select(ta => ta.SubjectAssignment.Group)
                .Distinct()
                .Select(g => new GroupDto
                {
                    Id = g.Id,
                    DisplayName = $"{g.Grade} – {g.Name}" // «1° – A»
                })
                .ToListAsync();
        }
    }
}
