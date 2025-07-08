using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;
using SchoolManager.ViewModels;
using System.Security.Cryptography;
using System.Text;

namespace SchoolManager.Services.Implementations;

public class SuperAdminService : ISuperAdminService
{
    private readonly SchoolDbContext _context;
    private readonly ILogger<SuperAdminService> _logger;

    public SuperAdminService(SchoolDbContext context, ILogger<SuperAdminService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Escuelas

    public async Task<List<SchoolListViewModel>> GetAllSchoolsAsync(string? searchString = null)
    {
        Console.WriteLine($"🏫 [SuperAdminService] Obteniendo lista de escuelas con filtro: '{searchString}'");
        
        var query = from s in _context.Schools
                   join u in _context.Users on s.AdminId equals u.Id into adminJoin
                   from admin in adminJoin.DefaultIfEmpty()
                   select new SchoolListViewModel
                   {
                       SchoolId = s.Id,
                       SchoolName = s.Name,
                       SchoolAddress = s.Address,
                       SchoolPhone = s.Phone,
                       SchoolLogoUrl = s.LogoUrl,
                       AdminId = admin != null ? admin.Id : Guid.Empty,
                       AdminName = admin != null ? admin.Name : "",
                       AdminLastName = admin != null ? admin.LastName : "",
                       AdminEmail = admin != null ? admin.Email : "",
                       AdminStatus = admin != null ? admin.Status : "",
                       CreatedAt = s.CreatedAt
                   };

        if (!string.IsNullOrEmpty(searchString))
        {
            searchString = searchString.ToLower();
            query = query.Where(s => s.SchoolName.ToLower().Contains(searchString) ||
                                   s.AdminName.ToLower().Contains(searchString) ||
                                   s.AdminEmail.ToLower().Contains(searchString));
        }

        var schools = await query.ToListAsync();
        Console.WriteLine($"✅ [SuperAdminService] Encontradas {schools.Count} escuelas");
        
        foreach (var school in schools)
        {
            Console.WriteLine($"   - {school.SchoolName} (ID: {school.SchoolId}) - Admin: {school.AdminName} {school.AdminLastName}");
        }

        return schools;
    }

    public async Task<School?> GetSchoolByIdAsync(Guid id)
    {
        Console.WriteLine($"🔍 [SuperAdminService] Buscando escuela con ID: {id}");
        
        var school = await _context.Schools
            .Include(s => s.Users)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (school != null)
        {
            Console.WriteLine($"✅ [SuperAdminService] Escuela encontrada: {school.Name}");
        }
        else
        {
            Console.WriteLine($"❌ [SuperAdminService] Escuela no encontrada con ID: {id}");
        }

        return school;
    }

    public async Task<SchoolAdminViewModel?> GetSchoolForEditAsync(Guid id)
    {
        Console.WriteLine($"🔍 [SuperAdminService] Obteniendo escuela para edición con ID: {id}");
        
        var school = await _context.Schools
            .Include(s => s.Users)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (school == null)
        {
            Console.WriteLine($"❌ [SuperAdminService] Escuela no encontrada para edición");
            return null;
        }

        var admin = school.Users.FirstOrDefault(u => u.Id == school.AdminId);

        var viewModel = new SchoolAdminViewModel
        {
            SchoolId = school.Id,
            SchoolName = school.Name,
            SchoolAddress = school.Address,
            SchoolPhone = school.Phone,
            AdminName = admin?.Name ?? "",
            AdminLastName = admin?.LastName ?? "",
            AdminEmail = admin?.Email ?? "",
            AdminPassword = "",
            AdminStatus = admin?.Status ?? "active"
        };

        Console.WriteLine($"✅ [SuperAdminService] ViewModel creado para escuela: {school.Name}");
        return viewModel;
    }

    public async Task<bool> CreateSchoolWithAdminAsync(SchoolAdminViewModel model, IFormFile? logoFile, string uploadsPath)
    {
        Console.WriteLine($"🏫 [SuperAdminService] Creando escuela con admin: {model.SchoolName}");
        
        try
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            // Guardar logo si se proporciona
            string? logoUrl = null;
            if (logoFile != null)
            {
                logoUrl = await SaveLogoAsync(logoFile, uploadsPath);
                Console.WriteLine($"📁 [SuperAdminService] Logo guardado: {logoUrl}");
            }

            // Crear la escuela primero sin admin
            var school = new School
            {
                Id = Guid.NewGuid(),
                Name = model.SchoolName,
                Address = model.SchoolAddress,
                Phone = model.SchoolPhone,
                LogoUrl = logoUrl,
                CreatedAt = DateTime.UtcNow
            };

            _context.Schools.Add(school);
            await _context.SaveChangesAsync();
            Console.WriteLine($"🏫 [SuperAdminService] Escuela creada: {school.Name}");

            // Crear el admin con la referencia a la escuela
            var admin = new User
            {
                Id = Guid.NewGuid(),
                Name = model.AdminName,
                LastName = model.AdminLastName,
                Email = model.AdminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.AdminPassword),
                Role = "admin",
                SchoolId = school.Id,
                Status = model.AdminStatus,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(admin);
            await _context.SaveChangesAsync();
            Console.WriteLine($"👤 [SuperAdminService] Admin creado: {admin.Name} {admin.LastName}");

            // Actualizar la escuela con la referencia al admin
            school.AdminId = admin.Id;
            _context.Schools.Update(school);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            Console.WriteLine($"✅ [SuperAdminService] Escuela y admin creados exitosamente");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SuperAdminService] Error creando escuela: {ex.Message}");
            _logger.LogError(ex, "Error creando escuela con admin");
            return false;
        }
    }

    public async Task<bool> UpdateSchoolAsync(SchoolAdminEditViewModel model, IFormFile? logoFile, string uploadsPath)
    {
        Console.WriteLine($"🔄 [SuperAdminService] Actualizando escuela: {model.SchoolName}");
        
        try
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            var school = await _context.Schools.FindAsync(model.SchoolId);
            if (school == null)
            {
                Console.WriteLine($"❌ [SuperAdminService] Escuela no encontrada para actualizar");
                return false;
            }

            // Actualizar datos de la escuela
            school.Name = model.SchoolName;
            school.Address = model.SchoolAddress;
            school.Phone = model.SchoolPhone;

            // Guardar nuevo logo si se proporciona
            if (logoFile != null)
            {
                var logoUrl = await SaveLogoAsync(logoFile, uploadsPath);
                if (!string.IsNullOrEmpty(logoUrl))
                {
                    school.LogoUrl = logoUrl;
                    Console.WriteLine($"📁 [SuperAdminService] Nuevo logo guardado: {logoUrl}");
                }
            }

            // Actualizar admin si se proporciona
            if (!string.IsNullOrEmpty(model.AdminName))
            {
                var admin = await _context.Users.FindAsync(model.AdminId);
                if (admin != null)
                {
                    admin.Name = model.AdminName;
                    admin.LastName = model.AdminLastName;
                    admin.Email = model.AdminEmail;
                    admin.Status = model.AdminStatus;

                    Console.WriteLine($"👤 [SuperAdminService] Admin actualizado: {admin.Name} {admin.LastName}");
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            Console.WriteLine($"✅ [SuperAdminService] Escuela actualizada exitosamente");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SuperAdminService] Error actualizando escuela: {ex.Message}");
            _logger.LogError(ex, "Error actualizando escuela");
            return false;
        }
    }

    public async Task<bool> DeleteSchoolAsync(Guid id)
    {
        Console.WriteLine($"🗑️ [SuperAdminService] Iniciando eliminación de escuela con ID: {id}");
        
        try
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            var school = await _context.Schools
                .Include(s => s.Users)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (school == null)
            {
                Console.WriteLine($"❌ [SuperAdminService] Escuela no encontrada con ID: {id}");
                return false;
            }

            Console.WriteLine($"✅ [SuperAdminService] Escuela encontrada: {school.Name}");
            Console.WriteLine($"👥 [SuperAdminService] Usuarios asociados: {school.Users.Count}");

            // Eliminar usuarios asociados primero
            if (school.Users.Count > 0)
            {
                Console.WriteLine($"🗑️ [SuperAdminService] Eliminando usuarios asociados...");
                
                var usersToDelete = school.Users.ToList();
                
                foreach (var user in usersToDelete)
                {
                    Console.WriteLine($"   - Eliminando usuario: {user.Name} {user.LastName} ({user.Email})");
                    
                    // Eliminar relaciones del usuario
                    await DeleteUserRelationsAsync(user);
                    
                    // Eliminar el usuario
                    _context.Users.Remove(user);
                    Console.WriteLine($"     ✅ [SuperAdminService] Relaciones eliminadas para: {user.Name}");
                }
            }

            // Eliminar entidades de la escuela
            await DeleteSchoolEntitiesAsync(school);

            // Eliminar relaciones muchos a muchos
            await DeleteManyToManyRelationsAsync(school);

            // Guardar cambios y eliminar la escuela
            await _context.SaveChangesAsync();
            Console.WriteLine($"✅ [SuperAdminService] Relaciones de la escuela eliminadas");

            _context.Schools.Remove(school);
            await _context.SaveChangesAsync();
            
            await transaction.CommitAsync();
            Console.WriteLine($"✅ [SuperAdminService] Escuela eliminada exitosamente: {school.Name}");
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SuperAdminService] Error eliminando escuela: {ex.Message}");
            _logger.LogError(ex, "Error eliminando escuela");
            return false;
        }
    }

    public async Task<SchoolAdminEditViewModel?> GetSchoolForEditWithAdminAsync(Guid id)
    {
        Console.WriteLine($"🔍 [SuperAdminService] Obteniendo escuela para edición con admin, ID: {id}");
        
        var school = await _context.Schools
            .Include(s => s.Users)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (school == null)
        {
            Console.WriteLine($"❌ [SuperAdminService] Escuela no encontrada para edición");
            return null;
        }

        var admin = school.Users.FirstOrDefault(u => u.Id == school.AdminId);

        var viewModel = new SchoolAdminEditViewModel
        {
            SchoolId = school.Id,
            SchoolName = school.Name,
            SchoolAddress = school.Address,
            SchoolPhone = school.Phone,
            AdminId = admin?.Id ?? Guid.Empty,
            AdminName = admin?.Name ?? "",
            AdminLastName = admin?.LastName ?? "",
            AdminEmail = admin?.Email ?? "",
            AdminStatus = admin?.Status ?? "active"
        };

        Console.WriteLine($"✅ [SuperAdminService] ViewModel creado para escuela: {school.Name}");
        return viewModel;
    }

    #endregion

    #region Usuarios

    public async Task<List<User>> GetAllAdminsAsync()
    {
        Console.WriteLine($"👥 [SuperAdminService] Obteniendo lista de admins");
        
        var admins = await _context.Users
            .Where(u => u.Role == "admin" || u.Role == "superadmin")
            .Include(u => u.School)
            .ToListAsync();

        Console.WriteLine($"✅ [SuperAdminService] Encontrados {admins.Count} admins");
        return admins;
    }

    public async Task<User?> GetUserByIdAsync(Guid id)
    {
        Console.WriteLine($"🔍 [SuperAdminService] Buscando usuario con ID: {id}");
        
        var user = await _context.Users
            .Include(u => u.School)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user != null)
        {
            Console.WriteLine($"✅ [SuperAdminService] Usuario encontrado: {user.Name} {user.LastName}");
        }
        else
        {
            Console.WriteLine($"❌ [SuperAdminService] Usuario no encontrado con ID: {id}");
        }

        return user;
    }

    public async Task<UserEditViewModel?> GetUserForEditAsync(Guid id)
    {
        Console.WriteLine($"🔍 [SuperAdminService] Obteniendo usuario para edición con ID: {id}");
        
        var user = await _context.Users
            .Include(u => u.School)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            Console.WriteLine($"❌ [SuperAdminService] Usuario no encontrado para edición");
            return null;
        }

        var viewModel = new UserEditViewModel
        {
            Id = user.Id,
            Name = user.Name,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            Status = user.Status
        };

        Console.WriteLine($"✅ [SuperAdminService] ViewModel creado para usuario: {user.Name}");
        return viewModel;
    }

    public async Task<bool> UpdateUserAsync(UserEditViewModel model)
    {
        Console.WriteLine($"🔄 [SuperAdminService] Actualizando usuario: {model.Name} {model.LastName}");
        
        try
        {
            var user = await _context.Users.FindAsync(model.Id);
            if (user == null)
            {
                Console.WriteLine($"❌ [SuperAdminService] Usuario no encontrado para actualizar");
                return false;
            }

            // Protección para usuarios superadmin - no pueden ser inactivados
            if (user.Role == "superadmin" && model.Status == "inactive")
            {
                Console.WriteLine($"🚫 [SuperAdminService] Intento de inactivar superadmin bloqueado: {user.Name}");
                return false;
            }

            user.Name = model.Name;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.Role = model.Role;
            
            // Solo actualizar status si no es superadmin o si se está activando
            if (user.Role != "superadmin" || model.Status == "active")
            {
                user.Status = model.Status;
            }
            else
            {
                // Forzar status activo para superadmin
                user.Status = "active";
                Console.WriteLine($"🔒 [SuperAdminService] Status forzado a 'active' para superadmin: {user.Name}");
            }

            Console.WriteLine($"👤 [SuperAdminService] Usuario actualizado: {user.Name} {user.LastName}");

            await _context.SaveChangesAsync();
            Console.WriteLine($"✅ [SuperAdminService] Usuario actualizado exitosamente");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SuperAdminService] Error actualizando usuario: {ex.Message}");
            _logger.LogError(ex, "Error actualizando usuario");
            return false;
        }
    }

    public async Task<bool> DeleteUserAsync(Guid id)
    {
        Console.WriteLine($"🗑️ [SuperAdminService] Eliminando usuario con ID: {id}");
        
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                Console.WriteLine($"❌ [SuperAdminService] Usuario no encontrado para eliminar");
                return false;
            }

            // Eliminar relaciones del usuario
            await DeleteUserRelationsAsync(user);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            Console.WriteLine($"✅ [SuperAdminService] Usuario eliminado exitosamente: {user.Name}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SuperAdminService] Error eliminando usuario: {ex.Message}");
            _logger.LogError(ex, "Error eliminando usuario");
            return false;
        }
    }

    #endregion

    #region Diagnóstico

    public async Task<object> DiagnoseSchoolAsync(Guid id)
    {
        Console.WriteLine($"🔍 [SuperAdminService] Diagnosticando escuela con ID: {id}");
        
        var school = await _context.Schools
            .Include(s => s.Users)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (school == null)
        {
            return new { error = "Escuela no encontrada" };
        }

        var diagnosis = new
        {
            school = new
            {
                id = school.Id,
                name = school.Name,
                address = school.Address,
                phone = school.Phone,
                logoUrl = school.LogoUrl,
                createdAt = school.CreatedAt
            },
            users = school.Users.Select(u => new
            {
                id = u.Id,
                name = u.Name,
                lastName = u.LastName,
                email = u.Email,
                role = u.Role,
                status = u.Status
            }).ToList(),
            userCount = school.Users.Count
        };

        Console.WriteLine($"✅ [SuperAdminService] Diagnóstico completado para: {school.Name}");
        return diagnosis;
    }

    #endregion

    #region Archivos

    public async Task<string?> SaveLogoAsync(IFormFile? logoFile, string uploadsPath)
    {
        if (logoFile == null || logoFile.Length == 0)
            return null;

        try
        {
            var fileName = $"{Guid.NewGuid()}_{logoFile.FileName}";
            var filePath = Path.Combine(uploadsPath, "schools", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            using var stream = new FileStream(filePath, FileMode.Create);
            await logoFile.CopyToAsync(stream);

            Console.WriteLine($"📁 [SuperAdminService] Logo guardado: {fileName}");
            return fileName;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SuperAdminService] Error guardando logo: {ex.Message}");
            return null;
        }
    }

    public async Task<string?> SaveAvatarAsync(IFormFile? avatarFile, string uploadsPath)
    {
        if (avatarFile == null || avatarFile.Length == 0)
            return null;

        try
        {
            var fileName = $"{Guid.NewGuid()}_{avatarFile.FileName}";
            var filePath = Path.Combine(uploadsPath, "avatars", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            using var stream = new FileStream(filePath, FileMode.Create);
            await avatarFile.CopyToAsync(stream);

            Console.WriteLine($"📁 [SuperAdminService] Avatar guardado: {fileName}");
            return fileName;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SuperAdminService] Error guardando avatar: {ex.Message}");
            return null;
        }
    }

    public async Task<byte[]?> GetLogoAsync(string? logoUrl)
    {
        if (string.IsNullOrEmpty(logoUrl))
            return null;

        try
        {
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            var filePath = Path.Combine(uploadsPath, "schools", logoUrl);

            if (File.Exists(filePath))
            {
                return await File.ReadAllBytesAsync(filePath);
            }

            Console.WriteLine($"❌ [SuperAdminService] Logo no encontrado: {logoUrl}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SuperAdminService] Error leyendo logo: {ex.Message}");
            return null;
        }
    }

    public async Task<byte[]?> GetAvatarAsync(string? avatarUrl)
    {
        if (string.IsNullOrEmpty(avatarUrl))
            return null;

        try
        {
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            var filePath = Path.Combine(uploadsPath, "avatars", avatarUrl);

            if (File.Exists(filePath))
            {
                return await File.ReadAllBytesAsync(filePath);
            }

            Console.WriteLine($"❌ [SuperAdminService] Avatar no encontrado: {avatarUrl}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SuperAdminService] Error leyendo avatar: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region Métodos privados

    private async Task DeleteUserRelationsAsync(User user)
    {
        Console.WriteLine($"     🔍 [SuperAdminService] Buscando relaciones para usuario: {user.Name}");

        // Eliminar asignaciones de estudiantes
        var studentAssignments = await _context.StudentAssignments
            .Where(sa => sa.StudentId == user.Id)
            .ToListAsync();
        
        if (studentAssignments.Count > 0)
        {
            Console.WriteLine($"     🗑️ [SuperAdminService] Eliminando {studentAssignments.Count} asignaciones de estudiantes");
            _context.StudentAssignments.RemoveRange(studentAssignments);
        }

        // Eliminar asignaciones de profesores
        var teacherAssignments = await _context.TeacherAssignments
            .Where(ta => ta.TeacherId == user.Id)
            .ToListAsync();
        
        if (teacherAssignments.Count > 0)
        {
            Console.WriteLine($"     🗑️ [SuperAdminService] Eliminando {teacherAssignments.Count} asignaciones de profesores");
            _context.TeacherAssignments.RemoveRange(teacherAssignments);
        }

        // Eliminar puntajes de actividades
        var activityScores = await _context.StudentActivityScores
            .Where(sas => sas.StudentId == user.Id)
            .ToListAsync();
        
        if (activityScores.Count > 0)
        {
            Console.WriteLine($"     🗑️ [SuperAdminService] Eliminando {activityScores.Count} puntajes de actividades");
            _context.StudentActivityScores.RemoveRange(activityScores);
        }

        // Eliminar reportes de disciplina
        var disciplineReports = await _context.DisciplineReports
            .Where(dr => dr.StudentId == user.Id || dr.TeacherId == user.Id)
            .ToListAsync();
        
        if (disciplineReports.Count > 0)
        {
            Console.WriteLine($"     🗑️ [SuperAdminService] Eliminando {disciplineReports.Count} reportes de disciplina");
            _context.DisciplineReports.RemoveRange(disciplineReports);
        }

        // Eliminar asistencias
        var attendances = await _context.Attendances
            .Where(a => a.StudentId == user.Id || a.TeacherId == user.Id)
            .ToListAsync();
        
        if (attendances.Count > 0)
        {
            Console.WriteLine($"     🗑️ [SuperAdminService] Eliminando {attendances.Count} asistencias");
            _context.Attendances.RemoveRange(attendances);
        }

        // Eliminar actividades creadas por el usuario
        var activities = await _context.Activities
            .Where(a => a.TeacherId == user.Id)
            .ToListAsync();
        
        if (activities.Count > 0)
        {
            Console.WriteLine($"     🗑️ [SuperAdminService] Eliminando {activities.Count} actividades");
            _context.Activities.RemoveRange(activities);
        }
    }

    private async Task DeleteSchoolEntitiesAsync(School school)
    {
        Console.WriteLine($"🏫 [SuperAdminService] Eliminando entidades de la escuela: {school.Name}");

        // Eliminar actividades
        var activities = await _context.Activities
            .Where(a => a.SchoolId == school.Id)
            .ToListAsync();
        
        if (activities.Count > 0)
        {
            Console.WriteLine($"🗑️ [SuperAdminService] Eliminando {activities.Count} actividades");
            _context.Activities.RemoveRange(activities);
        }

        // Eliminar tipos de actividades
        var activityTypes = await _context.ActivityTypes
            .Where(at => at.SchoolId == school.Id)
            .ToListAsync();
        
        if (activityTypes.Count > 0)
        {
            Console.WriteLine($"🗑️ [SuperAdminService] Eliminando {activityTypes.Count} tipos de actividades");
            _context.ActivityTypes.RemoveRange(activityTypes);
        }

        // Eliminar logs de auditoría
        var auditLogs = await _context.AuditLogs
            .Where(al => al.SchoolId == school.Id)
            .ToListAsync();
        
        if (auditLogs.Count > 0)
        {
            Console.WriteLine($"🗑️ [SuperAdminService] Eliminando {auditLogs.Count} logs de auditoría");
            _context.AuditLogs.RemoveRange(auditLogs);
        }

        // Eliminar asignaciones de materias
        var subjectAssignments = await _context.SubjectAssignments
            .Where(sa => sa.SchoolId == school.Id)
            .ToListAsync();
        
        if (subjectAssignments.Count > 0)
        {
            Console.WriteLine($"🗑️ [SuperAdminService] Eliminando {subjectAssignments.Count} asignaciones de materias");
            _context.SubjectAssignments.RemoveRange(subjectAssignments);
        }

        // Eliminar grupos
        var groups = await _context.Groups
            .Where(g => g.SchoolId == school.Id)
            .ToListAsync();
        
        if (groups.Count > 0)
        {
            Console.WriteLine($"🗑️ [SuperAdminService] Eliminando {groups.Count} grupos");
            _context.Groups.RemoveRange(groups);
        }

        // Eliminar materias
        var subjects = await _context.Subjects
            .Where(s => s.SchoolId == school.Id)
            .ToListAsync();
        
        if (subjects.Count > 0)
        {
            Console.WriteLine($"🗑️ [SuperAdminService] Eliminando {subjects.Count} materias");
            _context.Subjects.RemoveRange(subjects);
        }

        // Eliminar áreas
        var areas = await _context.Areas
            .Where(a => a.SchoolId == school.Id)
            .ToListAsync();
        
        if (areas.Count > 0)
        {
            Console.WriteLine($"🗑️ [SuperAdminService] Eliminando {areas.Count} áreas");
            _context.Areas.RemoveRange(areas);
        }

        // Eliminar trimestres
        var trimesters = await _context.Trimesters
            .Where(t => t.SchoolId == school.Id)
            .ToListAsync();
        
        if (trimesters.Count > 0)
        {
            Console.WriteLine($"🗑️ [SuperAdminService] Eliminando {trimesters.Count} trimestres");
            _context.Trimesters.RemoveRange(trimesters);
        }

        // Eliminar configuraciones de seguridad
        var securitySettings = await _context.SecuritySettings
            .Where(ss => ss.SchoolId == school.Id)
            .ToListAsync();
        
        if (securitySettings.Count > 0)
        {
            Console.WriteLine($"🗑️ [SuperAdminService] Eliminando {securitySettings.Count} configuraciones de seguridad");
            _context.SecuritySettings.RemoveRange(securitySettings);
        }

        // Eliminar estudiantes
        var students = await _context.Students
            .Where(s => s.SchoolId == school.Id)
            .ToListAsync();
        
        if (students.Count > 0)
        {
            Console.WriteLine($"🗑️ [SuperAdminService] Eliminando {students.Count} estudiantes");
            _context.Students.RemoveRange(students);
        }
    }

    private async Task DeleteManyToManyRelationsAsync(School school)
    {
        Console.WriteLine($"🔍 [SuperAdminService] Eliminando relaciones muchos a muchos");

        var schoolUsers = await _context.Users
            .Where(u => u.SchoolId == school.Id)
            .ToListAsync();

        foreach (var user in schoolUsers)
        {
            Console.WriteLine($"🗑️ [SuperAdminService] Eliminando relaciones para usuario: {user.Name}");

            // Eliminar relaciones user_groups
            var userGroupsCount = await _context.Database.ExecuteSqlRawAsync(
                $"DELETE FROM user_groups WHERE user_id = '{user.Id}'");
            
            if (userGroupsCount > 0)
            {
                Console.WriteLine($"🗑️ [SuperAdminService] Eliminadas {userGroupsCount} relaciones user_groups para {user.Name}");
            }

            // Eliminar relaciones user_subjects
            var userSubjectsCount = await _context.Database.ExecuteSqlRawAsync(
                $"DELETE FROM user_subjects WHERE user_id = '{user.Id}'");
            
            if (userSubjectsCount > 0)
            {
                Console.WriteLine($"🗑️ [SuperAdminService] Eliminadas {userSubjectsCount} relaciones user_subjects para {user.Name}");
            }

            // Eliminar relaciones user_grades
            var userGradesCount = await _context.Database.ExecuteSqlRawAsync(
                $"DELETE FROM user_grades WHERE user_id = '{user.Id}'");
            
            if (userGradesCount > 0)
            {
                Console.WriteLine($"🗑️ [SuperAdminService] Eliminadas {userGradesCount} relaciones user_grades para {user.Name}");
            }
        }
    }

    #endregion
} 