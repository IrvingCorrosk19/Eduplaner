using Microsoft.EntityFrameworkCore;
using SchoolManager.Dtos;            // ⇦ DTOs con get/set
using SchoolManager.Interfaces;      // ⇦ IActivityService, IFileStorage
using SchoolManager.Models;          // ⇦ SchoolDbContext, Activity

namespace SchoolManager.Services
{
    public class ActivityService : IActivityService
    {
        private readonly SchoolDbContext _context;
        private readonly IFileStorage _fileStorage;

        public ActivityService(SchoolDbContext context, IFileStorage fileStorage)
        {
            _context = context;
            _fileStorage = fileStorage;
        }

        /* ────────────────────────────────────────
           1.  Métodos usados por el Portal Docente
           ────────────────────────────────────────*/

        public async Task<ActivityDto> CreateAsync(ActivityCreateDto dto)
        {
            var activity = new Activity
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Type = dto.Type,          // 'tarea' | 'parcial' | 'examen'
                Trimester = dto.TrimesterCode, // '1T' | '2T' | '3T'
                TeacherId = dto.TeacherId,
                SubjectId = dto.SubjectId,
                GroupId = dto.GroupId,
                GradeLevelId = dto.GradeLevelId,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
            };

            if (dto.Pdf != null)
            {
                var path = $"activities/{activity.Id}/{dto.Pdf.FileName}";
                await using var stream = dto.Pdf.OpenReadStream();
                activity.PdfUrl = await _fileStorage.SaveAsync(path, stream);
            }

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            var subject = await _context.Subjects.FindAsync(dto.SubjectId);
            var group = await _context.Groups.FindAsync(dto.GroupId);

            return new ActivityDto
            {
                Id = activity.Id,
                Name = activity.Name,
                Type = activity.Type,
                Date = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
                TrimesterCode = activity.Trimester,
                SubjectName = subject?.Name ?? string.Empty,
                GroupDisplayName = group != null ? $"{group.Grade} – {group.Name}" : string.Empty,
                PdfUrl = activity.PdfUrl
            };
        }


        public async Task<IEnumerable<ActivityHeaderDto>> GetByTeacherGroupTrimesterAsync(
            Guid teacherId, Guid groupId, string trimesterCode)
        {
            return await _context.Activities
                .Where(a => a.TeacherId == teacherId
                         && a.GroupId == groupId
                         && a.Trimester == trimesterCode)
                .OrderBy(a => a.CreatedAt)
                .Select(a => new ActivityHeaderDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Type = a.Type,
                    Date = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
                    HasPdf = a.PdfUrl != null,
                    PdfUrl = a.PdfUrl
                })
                .ToListAsync();
        }

        public async Task UploadPdfAsync(Guid activityId, string fileName, Stream content)
        {
            var activity = await _context.Activities.FindAsync(activityId)
                ?? throw new InvalidOperationException("Actividad no encontrada.");

            var path = $"activities/{activityId}/{fileName}";
            activity.PdfUrl = await _fileStorage.SaveAsync(path, content);

            await _context.SaveChangesAsync();
        }

        /* ────────────────────────────────────────
           2.  CRUD "legacy" que aún usa tu proyecto
           ────────────────────────────────────────*/

        public async Task<List<Activity>> GetAllAsync() =>
            await _context.Activities.ToListAsync();

        public async Task<Activity?> GetByIdAsync(Guid id) =>
            await _context.Activities.FindAsync(id);

        public async Task UpdateAsync(Activity activity)
        {
            _context.Activities.Update(activity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.Activities.FindAsync(id);
            if (entity is null) return;

            _context.Activities.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Activity>> GetByGroupAndSubjectAsync(Guid groupId, Guid subjectId)
        {
            return await _context.Activities
                .Where(a => a.GroupId == groupId && a.SubjectId == subjectId)
                .ToListAsync();
        }
    }
}
