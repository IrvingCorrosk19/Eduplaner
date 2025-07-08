using Microsoft.EntityFrameworkCore;
using SchoolManager.Dtos;            // ⇦ DTOs con get/set
using SchoolManager.Interfaces;      // ⇦ IActivityService, IFileStorage
using SchoolManager.Models;          // ⇦ SchoolDbContext, Activity
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Services
{
    public class ActivityService : IActivityService
    {
        private readonly SchoolDbContext _context;
        private readonly IFileStorage _fileStorage;
        private readonly ITrimesterService _trimesterService;
        private readonly ICurrentUserService _currentUserService;

        public ActivityService(SchoolDbContext context, IFileStorage fileStorage, ITrimesterService trimesterService, ICurrentUserService currentUserService)
        {
            _context = context;
            _fileStorage = fileStorage;
            _trimesterService = trimesterService;
            _currentUserService = currentUserService;
        }

        /* ────────────────────────────────────────
           1.  Métodos usados por el Portal Docente
           ────────────────────────────────────────*/

        public async Task<ActivityDto> CreateAsync(ActivityCreateDto dto)
        {
            try
            {
                Console.WriteLine($"[ActivityService] Iniciando creación de actividad: {dto.Name}");
                
                // Validar trimestre activo
                await _trimesterService.ValidateTrimesterActiveAsync(dto.TrimesterCode);
                Console.WriteLine($"[ActivityService] Trimestre validado: {dto.TrimesterCode}");

                // Obtener la escuela del usuario logueado
                var currentUserSchool = await _currentUserService.GetCurrentUserSchoolAsync();
                if (currentUserSchool == null)
                {
                    throw new InvalidOperationException("No se pudo determinar la escuela del usuario actual.");
                }
                Console.WriteLine($"[ActivityService] Escuela obtenida: {currentUserSchool.Name}");

                // Buscar el trimestre por código y escuela
                var trimestre = await _context.Trimesters
                    .FirstOrDefaultAsync(t => t.Name == dto.TrimesterCode && t.SchoolId == currentUserSchool.Id);
                
                if (trimestre == null)
                {
                    throw new InvalidOperationException($"No se encontró el trimestre '{dto.TrimesterCode}' para la escuela actual.");
                }
                Console.WriteLine($"[ActivityService] Trimestre encontrado: {trimestre.Name}");

                var activity = new Activity
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    Type = dto.Type,          // 'tarea' | 'parcial' | 'examen'
                    Trimester = dto.TrimesterCode, // '1T' | '2T' | '3T'
                    TrimesterId = trimestre.Id,    // ← Asignar TrimesterId
                    TeacherId = dto.TeacherId,
                    SubjectId = dto.SubjectId,
                    GroupId = dto.GroupId,
                    GradeLevelId = dto.GradeLevelId,
                    SchoolId = currentUserSchool.Id,  // ← Agregar SchoolId del usuario logueado
                    CreatedAt = DateTime.UtcNow,
                    DueDate = dto.DueDate?.ToUniversalTime()
                };

                Console.WriteLine($"[ActivityService] Actividad creada con ID: {activity.Id}");
                Console.WriteLine($"[ActivityService] DueDate: {activity.DueDate} (UTC: {activity.DueDate?.ToUniversalTime()})");

                if (dto.Pdf != null)
                {
                    var path = $"activities/{activity.Id}/{dto.Pdf.FileName}";
                    await using var stream = dto.Pdf.OpenReadStream();
                    activity.PdfUrl = await _fileStorage.SaveAsync(path, stream);
                    Console.WriteLine($"[ActivityService] PDF guardado en: {activity.PdfUrl}");
                }

                _context.Activities.Add(activity);
                await _context.SaveChangesAsync();
                Console.WriteLine($"[ActivityService] Actividad guardada exitosamente en la base de datos");

                var subject = await _context.Subjects.FindAsync(dto.SubjectId);
                var group = await _context.Groups.FindAsync(dto.GroupId);

                var result = new ActivityDto
                {
                    Id = activity.Id,
                    Name = activity.Name,
                    Type = activity.Type,
                    Date = DateTime.UtcNow,
                    TrimesterCode = activity.Trimester,
                    SubjectName = subject?.Name ?? string.Empty,
                    GroupDisplayName = group != null ? $"{group.Grade} – {group.Name}" : string.Empty,
                    PdfUrl = activity.PdfUrl
                };

                Console.WriteLine($"[ActivityService] Actividad creada exitosamente: {result.Name}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ActivityService] ERROR al crear actividad: {ex.Message}");
                Console.WriteLine($"[ActivityService] Stack trace: {ex.StackTrace}");
                throw; // Re-lanzar la excepción para que el controlador la maneje
            }
        }


        public async Task<IEnumerable<ActivityHeaderDto>> GetByTeacherGroupTrimesterAsync(
            Guid teacherId, Guid groupId, string trimesterCode)
        {
            // Obtener la escuela del usuario logueado para filtrar
            var currentUserSchool = await _currentUserService.GetCurrentUserSchoolAsync();
            if (currentUserSchool == null)
            {
                throw new InvalidOperationException("No se pudo determinar la escuela del usuario actual.");
            }

            // Buscar el trimestre por código y escuela
            var trimestre = await _context.Trimesters
                .FirstOrDefaultAsync(t => t.Name == trimesterCode && t.SchoolId == currentUserSchool.Id);

            if (trimestre == null)
            {
                // Si no existe el trimestre, devolver lista vacía
                return new List<ActivityHeaderDto>();
            }

            return await _context.Activities
                .Where(a => a.TeacherId == teacherId
                         && a.GroupId == groupId
                         && a.Trimester == trimesterCode
                         && a.SchoolId == currentUserSchool.Id
                         && a.TrimesterId == trimestre.Id)  // ← Filtrar también por TrimesterId
                .OrderBy(a => a.CreatedAt)
                .Select(a => new ActivityHeaderDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Type = a.Type,
                    Date = DateTime.UtcNow,
                    HasPdf = a.PdfUrl != null,
                    PdfUrl = a.PdfUrl
                })
                .ToListAsync();
        }

        public async Task UploadPdfAsync(Guid activityId, string fileName, Stream content)
        {
            var activity = await _context.Activities.FindAsync(activityId)
                ?? throw new InvalidOperationException("Actividad no encontrada.");

            // Validar trimestre activo antes de subir PDF
            await _trimesterService.ValidateTrimesterActiveAsync(activity.Trimester);

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
            // Validar trimestre activo antes de actualizar
            await _trimesterService.ValidateTrimesterActiveAsync(activity.Trimester);

            // Obtener la escuela del usuario logueado para validación
            var currentUserSchool = await _currentUserService.GetCurrentUserSchoolAsync();
            if (currentUserSchool == null)
            {
                throw new InvalidOperationException("No se pudo determinar la escuela del usuario actual.");
            }

            // Verificar que la actividad pertenece a la misma escuela
            if (activity.SchoolId != currentUserSchool.Id)
            {
                throw new UnauthorizedAccessException("No tiene permisos para modificar actividades de otra escuela.");
            }

            // Verificar que el trimestre existe y pertenece a la misma escuela
            if (activity.TrimesterId.HasValue)
            {
                var trimestre = await _context.Trimesters
                    .FirstOrDefaultAsync(t => t.Id == activity.TrimesterId && t.SchoolId == currentUserSchool.Id);
                
                if (trimestre == null)
                {
                    throw new InvalidOperationException("El trimestre asociado no existe o no pertenece a su escuela.");
                }
            }

            // Asegurar que el SchoolId se mantenga
            activity.SchoolId = currentUserSchool.Id;

            _context.Activities.Update(activity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.Activities.FindAsync(id);
            if (entity is null) return;

            // Validar trimestre activo antes de eliminar
            await _trimesterService.ValidateTrimesterActiveAsync(entity.Trimester);

            // Obtener la escuela del usuario logueado para validación
            var currentUserSchool = await _currentUserService.GetCurrentUserSchoolAsync();
            if (currentUserSchool == null)
            {
                throw new InvalidOperationException("No se pudo determinar la escuela del usuario actual.");
            }

            // Verificar que la actividad pertenece a la misma escuela
            if (entity.SchoolId != currentUserSchool.Id)
            {
                throw new UnauthorizedAccessException("No tiene permisos para eliminar actividades de otra escuela.");
            }

            _context.Activities.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Activity>> GetByGroupAndSubjectAsync(Guid groupId, Guid subjectId)
        {
            // Obtener la escuela del usuario logueado para filtrar
            var currentUserSchool = await _currentUserService.GetCurrentUserSchoolAsync();
            if (currentUserSchool == null)
            {
                throw new InvalidOperationException("No se pudo determinar la escuela del usuario actual.");
            }

            return await _context.Activities
                .Where(a => a.GroupId == groupId 
                         && a.SubjectId == subjectId
                         && a.SchoolId == currentUserSchool.Id)  // ← Filtrar por escuela
                .ToListAsync();
        }
    }
}
