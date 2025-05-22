using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;
using SchoolManager.Dtos;
using SchoolManager.Interfaces;

namespace SchoolManager.Services
{
    public class StudentService : IStudentService
    {
        private readonly SchoolDbContext _context;

        public StudentService(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<Student>> GetAllAsync() =>
            await _context.Students.ToListAsync();

        public async Task<Student?> GetByIdAsync(Guid id) =>
            await _context.Students.FindAsync(id);

        public async Task CreateAsync(Student student)
        {
            _context.Students.Add(student);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Student student)
        {
            _context.Students.Update(student);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Student>> GetByGroupAsync(string groupName) =>
            await _context.Students
                .Where(s => s.GroupName == groupName)
                .ToListAsync();

        public async Task<IEnumerable<StudentBasicDto>> GetByGroupAndGradeAsync(Guid groupId, Guid gradeId)
        {
            var result = await (from sa in _context.StudentAssignments
                                join student in _context.Users on sa.StudentId equals student.Id
                                join grade in _context.GradeLevels on sa.GradeId equals grade.Id
                                join grupo in _context.Groups on sa.GroupId equals grupo.Id
                                where (student.Role == "estudiante" || student.Role == "student" || student.Role == "alumno")
                                      && sa.GroupId == groupId
                                      && sa.GradeId == gradeId
                                orderby student.Name
                                select new StudentBasicDto
                                {
                                    StudentId = student.Id,
                                    FullName = student.Name,
                                    GradeName = grade.Name,
                                    GroupName = grupo.Name
                                }).ToListAsync();

            return result;
        }

        public async Task<IEnumerable<StudentBasicDto>> GetBySubjectGroupAndGradeAsync(Guid subjectId, Guid groupId, Guid gradeId)
        {
            var result = await (from sa in _context.StudentAssignments
                                join student in _context.Users on sa.StudentId equals student.Id
                                join grade in _context.GradeLevels on sa.GradeId equals grade.Id
                                join grupo in _context.Groups on sa.GroupId equals grupo.Id
                                join subjectAssign in _context.SubjectAssignments on new { sa.GradeId, sa.GroupId } equals new { GradeId = subjectAssign.GradeLevelId, GroupId = subjectAssign.GroupId }
                                where (student.Role == "estudiante" || student.Role == "student" || student.Role == "alumno")
                                      && sa.GroupId == groupId
                                      && sa.GradeId == gradeId
                                      && subjectAssign.SubjectId == subjectId
                                orderby student.Name
                                select new StudentBasicDto
                                {
                                    StudentId = student.Id,
                                    FullName = student.Name,
                                    GradeName = grade.Name,
                                    GroupName = grupo.Name
                                }).ToListAsync();

            return result;
        }
    }
}
