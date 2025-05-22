using SchoolManager.Models;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Enums;
using BCrypt.Net;

public class UserService : IUserService
{
    private readonly SchoolDbContext _context;

    public UserService(SchoolDbContext context)
    {
        _context = context;
    }

    public async Task<List<User>> GetAllStudentsAsync()
    {
        return await _context.Users
            .Where(u => u.Role.ToLower() == "student" || u.Role.ToLower() == "estudiante")
            .OrderBy(u => u.Name)
            .ToListAsync();
    }

    public async Task UpdateAsync(User user, List<Guid> subjectIds, List<Guid> groupIds)
    {
        // Actualizar Subjects
        user.Subjects.Clear();
        if (subjectIds.Any())
        {
            var subjects = await _context.Subjects.Where(s => subjectIds.Contains(s.Id)).ToListAsync();
            foreach (var subject in subjects)
            {
                user.Subjects.Add(subject);
            }
        }

        // Actualizar Groups
        user.Groups.Clear();
        if (groupIds.Any())
        {
            var groups = await _context.Groups.Where(g => groupIds.Contains(g.Id)).ToListAsync();
            foreach (var group in groups)
            {
                user.Groups.Add(group);
            }
        }

        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
    public async Task UpdateAsync(User user, List<Guid> subjectIds, List<Guid> groupIds, List<Guid> gradeLevelIds)
    {
        // Actualizar Subjects
        user.Subjects.Clear();
        if (subjectIds.Any())
        {
            var subjects = await _context.Subjects.Where(s => subjectIds.Contains(s.Id)).ToListAsync();
            foreach (var subject in subjects)
            {
                user.Subjects.Add(subject);
            }
        }


        // Actualizar Groups
        user.Groups.Clear();
        if (groupIds.Any())
        {
            var groups = await _context.Groups.Where(g => groupIds.Contains(g.Id)).ToListAsync();
            foreach (var group in groups)
            {
                user.Groups.Add(group);
            }
        }

        // Actualizar GradeLevels
        user.Grades.Clear();
        if (gradeLevelIds.Any())
        {
            var grades = await _context.GradeLevels.Where(g => gradeLevelIds.Contains(g.Id)).ToListAsync();
            foreach (var grade in grades)
            {
                user.Grades.Add(grade);
            }
        }

        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
    public async Task<List<User>> GetAllTeachersAsync()
    {
        return await _context.Users
            .Where(u => u.Role == "teacher")
            .OrderBy(u => u.Name)
            .ToListAsync();
    }
    public async Task CreateAsync(User user, List<Guid> subjectIds, List<Guid> groupIds)
    {
        try
        {
            // Hash de la contraseña
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            
            // Cargar las entidades completas desde la base de datos
            var subjects = await _context.Subjects.Where(s => subjectIds.Contains(s.Id)).ToListAsync();
            var groups = await _context.Groups.Where(g => groupIds.Contains(g.Id)).ToListAsync();

            // Asignar las relaciones
            user.Subjects = subjects;
            user.Groups = groups;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new Exception("Error al crear el usuario y asignar relaciones.", ex);
        }
    }
    public async Task<User?> GetByIdWithRelationsAsync(Guid id)
    {
        return await _context.Users
            .Include(u => u.Subjects)
            .Include(u => u.Groups)
            .Include(u => u.Grades)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<List<User>> GetAllAsync() =>
        await _context.Users.ToListAsync();

    public async Task<List<User>> GetAllWithAssignmentsByRoleAsync(string role)
    {
        return await _context.Users
            .Where(u => u.Role == role)
            .Include(u => u.TeacherAssignments)
                .ThenInclude(ta => ta.SubjectAssignment)
                    .ThenInclude(sa => sa.Subject)
            .Include(u => u.TeacherAssignments)
                .ThenInclude(ta => ta.SubjectAssignment)
                    .ThenInclude(sa => sa.Group)
            .Include(u => u.TeacherAssignments)
                .ThenInclude(ta => ta.SubjectAssignment)
                    .ThenInclude(sa => sa.GradeLevel)
            .Include(u => u.TeacherAssignments)
                .ThenInclude(ta => ta.SubjectAssignment)
                    .ThenInclude(sa => sa.Area)
            .Include(u => u.TeacherAssignments)
                .ThenInclude(ta => ta.SubjectAssignment)
                    .ThenInclude(sa => sa.Specialty)
            .ToListAsync();
    }
    public async Task<User?> GetByIdAsync(Guid id) =>
        await _context.Users.FindAsync(id);

    public async Task CreateAsync(User user, List<Guid> subjectIds, List<Guid> groupIds, List<Guid> gradeLevelIds)
    {
        try
        {
            // Hash de la contraseña
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            
            var subjects = await _context.Subjects.Where(s => subjectIds.Contains(s.Id)).ToListAsync();
            var groups = await _context.Groups.Where(g => groupIds.Contains(g.Id)).ToListAsync();
            var grades = await _context.GradeLevels.Where(g => gradeLevelIds.Contains(g.Id)).ToListAsync();

            user.Subjects = subjects;
            user.Groups = groups;
            user.Grades = grades;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new Exception("Error al crear el usuario y asignar relaciones.", ex);
        }
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }


public async Task DeleteAsync(Guid id)
{
    await using var transaction = await _context.Database.BeginTransactionAsync(); // 🔁 INICIO TRANSACCIÓN

    try
    {
        var user = await _context.Users
            .Include(u => u.Subjects)
            .Include(u => u.Groups)
            .Include(u => u.Grades)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            throw new InvalidOperationException($"No se encontró el usuario con ID: {id}");

        // Validar el rol usando enum
        if (!Enum.TryParse<UserRole>(user.Role, true, out var parsedRole))
            throw new InvalidOperationException($"Rol no válido o no soportado: {user.Role}");

        switch (parsedRole)
        {
            case UserRole.Student:
                var studentAssignments = await _context.StudentAssignments
                    .Where(sa => sa.StudentId == id)
                    .ToListAsync();
                _context.StudentAssignments.RemoveRange(studentAssignments);
                break;

            case UserRole.Teacher:
                var teacherAssignments = await _context.TeacherAssignments
                    .Where(ta => ta.TeacherId == id)
                    .ToListAsync();
                _context.TeacherAssignments.RemoveRange(teacherAssignments);
                break;

            case UserRole.Admin:
            case UserRole.Director:
                // No asignaciones específicas
                break;

            default:
                throw new InvalidOperationException($"Rol no manejado: {parsedRole}");
        }

        // Limpieza de relaciones M:M
        //user.Subjects.Clear();
        //user.Groups.Clear();
        //user.Grades.Clear();

        await _context.SaveChangesAsync();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();
    }
    catch (DbUpdateException dbEx)
    {
        await transaction.RollbackAsync(); 
        throw new Exception("No se puede eliminar el usuario porque tiene dependencias en otras entidades.", dbEx);
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        throw new Exception("Error inesperado al eliminar el usuario.", ex);
    }
}
//public async Task DeleteAsync(Guid id)
//{
//    try
//    {
//        var user = await _context.Users
//            .Include(u => u.Subjects)
//            .Include(u => u.Groups)
//            .Include(u => u.Grades)
//            .FirstOrDefaultAsync(u => u.Id == id);

//        if (user == null)
//            throw new InvalidOperationException($"No se encontró el usuario con ID: {id}");

//        // Eliminar relaciones explícitas
//        //user.Subjects.Clear();
//        //user.Groups.Clear();
//        //user.Grades.Clear();

//        var role = user.Role.ToLower();


//        // Eliminar asignaciones de profesor
//        var assignments = await _context.TeacherAssignments
//            .Where(ta => ta.TeacherId == id)
//            .ToListAsync();

//        _context.TeacherAssignments.RemoveRange(assignments);

//        await _context.SaveChangesAsync();

//        // Eliminar el usuario
//        _context.Users.Remove(user);
//        await _context.SaveChangesAsync();
//    }
//    catch (DbUpdateException dbEx)
//    {
//        throw new Exception("No se puede eliminar el usuario porque tiene dependencias en otras entidades (como asignaciones de docentes).", dbEx);
//    }
//    catch (Exception ex)
//    {
//        throw new Exception("Error inesperado al eliminar el usuario.", ex);
//    }
//}


public async Task<User?> AuthenticateAsync(string email, string password)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.PasswordHash == password);
    }
    public async Task<User?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower().Trim() == email.ToLower().Trim());
    }

}
