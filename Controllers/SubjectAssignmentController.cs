using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Dtos;
using SchoolManager.Models;
using SchoolManager.Models.ViewModels;
using SchoolManager.Services.Interfaces;
using SchoolManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchoolManager.Controllers
{
    public class SubjectAssignmentController : Controller
    {
        private readonly SchoolDbContext _context;
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISubjectService _subjectService;
        private readonly IGroupService _groupService;
        private readonly IGradeLevelService _gradeLevelService;
        private readonly IAreaService _areaService;
        private readonly ISpecialtyService _specialtyService;
        private readonly IStudentAssignmentService _studentAssignmentService;
        private readonly ISubjectAssignmentService _subjectAssignmentService;


        public SubjectAssignmentController(
            SchoolDbContext context,
            IUserService userService,
            ICurrentUserService currentUserService,
            ISubjectService subjectService,
            IGroupService groupService,
            IGradeLevelService gradeLevelService,
            IAreaService areaService,
            ISpecialtyService specialtyService,
            IStudentAssignmentService studentAssignmentService,
            ISubjectAssignmentService subjectAssignmentService)
        {
            _context = context;
            _userService = userService;
            _currentUserService = currentUserService;
            _subjectService = subjectService;
            _groupService = groupService;
            _gradeLevelService = gradeLevelService;
            _areaService = areaService;
            _specialtyService = specialtyService;
            _studentAssignmentService = studentAssignmentService;
            _subjectAssignmentService = subjectAssignmentService;

        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Obtener el usuario actual y su escuela
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var schoolId = currentUser.SchoolId;
            if (schoolId == null)
            {
                return View(new List<SubjectAssignmentViewModel>());
            }

            // Obtener solo las asignaciones de la escuela del usuario
            var subjectAssignments = await _context.SubjectAssignments
                .Include(sa => sa.Specialty)
                .Include(sa => sa.Area)
                .Include(sa => sa.Subject)
                .Include(sa => sa.GradeLevel)
                .Include(sa => sa.Group)
                .Where(sa => sa.SchoolId == schoolId)
                .ToListAsync();

            var viewModel = subjectAssignments.Select(sa => new SubjectAssignmentViewModel
            {
                Id = sa.Id,
                SpecialtyId = sa.SpecialtyId,
                AreaId = sa.AreaId,
                SubjectId = sa.SubjectId,
                GradeLevelId = sa.GradeLevelId,
                GroupId = sa.GroupId,

                SpecialtyName = sa.Specialty.Name,
                AreaName = sa.Area.Name,
                SubjectName = sa.Subject.Name,
                GradeLevelName = sa.GradeLevel.Name,
                GroupName = sa.Group.Name,

                // Agregar el campo Status
                Status = sa.Status ?? "Active" // Asignar 'Active' si Status es null
            }).ToList();

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAssignments()
        {
            // Obtener el usuario actual y su escuela
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Usuario no autenticado." });
            }

            var schoolId = currentUser.SchoolId;
            if (schoolId == null)
            {
                return Json(new { success = true, assignments = new List<object>() });
            }

            var allAssignments = await _context.SubjectAssignments
                .Include(sa => sa.Specialty)
                .Include(sa => sa.Area)
                .Include(sa => sa.Subject)
                .Include(sa => sa.GradeLevel)
                .Include(sa => sa.Group)
                .Where(sa => sa.SchoolId == schoolId)
                .Select(sa => new
                {
                    sa.Id,
                    sa.SpecialtyId,
                    sa.AreaId,
                    sa.SubjectId,
                    sa.GradeLevelId,
                    sa.GroupId,
                    SpecialtyName = sa.Specialty.Name,
                    AreaName = sa.Area.Name,
                    SubjectName = sa.Subject.Name,
                    GradeLevelName = sa.GradeLevel.Name,
                    GroupName = sa.Group.Name
                })
                .ToListAsync();

            return Json(new { success = true, assignments = allAssignments });
        }

        [HttpGet]
        public async Task<IActionResult> GetDropdownData()
        {
            try
            {
                // Obtener el usuario actual y su escuela
                var currentUser = await _currentUserService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return Json(new { success = false, message = "Usuario no autenticado." });
                }

                var schoolId = currentUser.SchoolId;
                if (schoolId == null)
                {
                    return Json(new { success = false, message = "Usuario no tiene escuela asignada." });
                }

                // Obtener todos los datos disponibles (sin filtrar por SchoolId ya que algunos modelos no lo tienen)
                var specialties = await _context.Specialties.ToListAsync();
                var areas = await _context.Areas.ToListAsync();
                var subjects = await _context.Subjects.ToListAsync();
                var gradeLevels = await _context.GradeLevels.ToListAsync();
                var groups = await _context.Groups.ToListAsync();

                return Json(new
                {
                    success = true,
                    specialties = specialties.Select(s => new { s.Id, s.Name }),
                    areas = areas.Select(a => new { a.Id, a.Name }),
                    subjects = subjects.Select(s => new { s.Id, s.Name }),
                    gradeLevels = gradeLevels.Select(g => new { g.Id, g.Name }),
                    groups = groups.Select(g => new { g.Id, g.Name })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al cargar los datos: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SubjectAssignmentCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Los datos no son válidos." });
            }

            try
            {
                // Obtener el usuario actual y su escuela
                var currentUser = await _currentUserService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return Json(new { success = false, message = "Usuario no autenticado." });
                }

                var schoolId = currentUser.SchoolId;
                if (schoolId == null)
                {
                    return Json(new { success = false, message = "Usuario no tiene escuela asignada." });
                }

                // Obtener los nombres de los elementos para mensajes más descriptivos
                var specialty = await _context.Specialties.FindAsync(model.SpecialtyId);
                var area = await _context.Areas.FindAsync(model.AreaId);
                var subject = await _context.Subjects.FindAsync(model.SubjectId);
                var gradeLevel = await _context.GradeLevels.FindAsync(model.GradeLevelId);
                var group = await _context.Groups.FindAsync(model.GroupId);

                // Verificar que no exista otra asignación con la misma combinación completa
                var existingAssignment = await _context.SubjectAssignments
                    .Include(sa => sa.Specialty)
                    .Include(sa => sa.Area)
                    .Include(sa => sa.Subject)
                    .Include(sa => sa.GradeLevel)
                    .Include(sa => sa.Group)
                    .FirstOrDefaultAsync(sa =>
                        sa.SpecialtyId == model.SpecialtyId &&
                        sa.AreaId == model.AreaId &&
                        sa.SubjectId == model.SubjectId &&
                        sa.GradeLevelId == model.GradeLevelId &&
                        sa.GroupId == model.GroupId &&
                        sa.SchoolId == schoolId
                    );

                if (existingAssignment != null)
                {
                    var message = $"Ya existe una asignación con la siguiente combinación:\n" +
                                $"• Especialidad: {existingAssignment.Specialty.Name}\n" +
                                $"• Área: {existingAssignment.Area.Name}\n" +
                                $"• Materia: {existingAssignment.Subject.Name}\n" +
                                $"• Grado: {existingAssignment.GradeLevel.Name}\n" +
                                $"• Grupo: {existingAssignment.Group.Name}\n\n" +
                                $"Esta combinación ya está registrada en el sistema.";

                    return Json(new { success = false, message = message });
                }

                // Verificar combinaciones parciales que podrían causar conflictos
                var sameSpecialtyAreaSubject = await _context.SubjectAssignments
                    .Include(sa => sa.Specialty)
                    .Include(sa => sa.Area)
                    .Include(sa => sa.Subject)
                    .Include(sa => sa.GradeLevel)
                    .Include(sa => sa.Group)
                    .FirstOrDefaultAsync(sa =>
                        sa.SpecialtyId == model.SpecialtyId &&
                        sa.AreaId == model.AreaId &&
                        sa.SubjectId == model.SubjectId &&
                        sa.SchoolId == schoolId &&
                        (sa.GradeLevelId != model.GradeLevelId || sa.GroupId != model.GroupId)
                    );

                if (sameSpecialtyAreaSubject != null)
                {
                    var message = $"Ya existe una asignación con la misma Especialidad, Área y Materia:\n" +
                                $"• Especialidad: {sameSpecialtyAreaSubject.Specialty.Name}\n" +
                                $"• Área: {sameSpecialtyAreaSubject.Area.Name}\n" +
                                $"• Materia: {sameSpecialtyAreaSubject.Subject.Name}\n" +
                                $"• Grado: {sameSpecialtyAreaSubject.GradeLevel.Name}\n" +
                                $"• Grupo: {sameSpecialtyAreaSubject.Group.Name}\n\n" +
                                $"Verifique que no esté duplicando la misma materia para diferentes grados o grupos.";

                    return Json(new { success = false, message = message });
                }

                // Verificar si la materia ya está asignada al mismo grupo
                var sameSubjectGroup = await _context.SubjectAssignments
                    .Include(sa => sa.Specialty)
                    .Include(sa => sa.Area)
                    .Include(sa => sa.Subject)
                    .Include(sa => sa.GradeLevel)
                    .Include(sa => sa.Group)
                    .FirstOrDefaultAsync(sa =>
                        sa.SubjectId == model.SubjectId &&
                        sa.GroupId == model.GroupId &&
                        sa.SchoolId == schoolId &&
                        sa.Id != Guid.Empty // Excluir la asignación actual si estamos editando
                    );

                if (sameSubjectGroup != null)
                {
                    var message = $"La materia '{subject?.Name}' ya está asignada al grupo '{group?.Name}' con:\n" +
                                $"• Especialidad: {sameSubjectGroup.Specialty.Name}\n" +
                                $"• Área: {sameSubjectGroup.Area.Name}\n" +
                                $"• Grado: {sameSubjectGroup.GradeLevel.Name}\n\n" +
                                $"Una materia no puede estar asignada al mismo grupo más de una vez.";

                    return Json(new { success = false, message = message });
                }

                var subjectAssignment = new SubjectAssignment
                {
                    Id = Guid.NewGuid(),
                    SpecialtyId = model.SpecialtyId,
                    AreaId = model.AreaId,
                    SubjectId = model.SubjectId,
                    GradeLevelId = model.GradeLevelId,
                    GroupId = model.GroupId,
                    SchoolId = schoolId,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                };

                _context.SubjectAssignments.Add(subjectAssignment);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Asignación creada correctamente." });
            }
            catch (DbUpdateException ex)
            {
                return Json(new { success = false, message = "Error al crear la asignación en la base de datos. Por favor revisa los datos." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ocurrió un error inesperado: " + ex.Message });
            }
        }



        [HttpPost]
        public async Task<IActionResult> Edit([FromBody] EditSubjectAssignmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Los datos no son válidos." });
            }

            try
            {
                var subjectAssignment = await _context.SubjectAssignments
                    .FirstOrDefaultAsync(sa => sa.Id == model.Id);

                if (subjectAssignment == null)
                {
                    return Json(new { success = false, message = "La asignación no existe." });
                }

                // Obtener los nombres de los elementos para mensajes más descriptivos
                var specialty = await _context.Specialties.FindAsync(model.SpecialtyId);
                var area = await _context.Areas.FindAsync(model.AreaId);
                var subject = await _context.Subjects.FindAsync(model.SubjectId);
                var gradeLevel = await _context.GradeLevels.FindAsync(model.GradeLevelId);
                var group = await _context.Groups.FindAsync(model.GroupId);

                // Verificar que no exista otra asignación con la misma combinación completa
                var existingAssignment = await _context.SubjectAssignments
                    .Include(sa => sa.Specialty)
                    .Include(sa => sa.Area)
                    .Include(sa => sa.Subject)
                    .Include(sa => sa.GradeLevel)
                    .Include(sa => sa.Group)
                    .FirstOrDefaultAsync(sa =>
                        sa.SpecialtyId == model.SpecialtyId &&
                        sa.AreaId == model.AreaId &&
                        sa.SubjectId == model.SubjectId &&
                        sa.GradeLevelId == model.GradeLevelId &&
                        sa.GroupId == model.GroupId &&
                        sa.Id != model.Id
                    );

                if (existingAssignment != null)
                {
                    var message = $"Ya existe una asignación con la siguiente combinación:\n" +
                                $"• Especialidad: {existingAssignment.Specialty.Name}\n" +
                                $"• Área: {existingAssignment.Area.Name}\n" +
                                $"• Materia: {existingAssignment.Subject.Name}\n" +
                                $"• Grado: {existingAssignment.GradeLevel.Name}\n" +
                                $"• Grupo: {existingAssignment.Group.Name}\n\n" +
                                $"Esta combinación ya está registrada en el sistema.";

                    return Json(new { success = false, message = message });
                }

                // Verificar combinaciones parciales que podrían causar conflictos
                var sameSpecialtyAreaSubject = await _context.SubjectAssignments
                    .Include(sa => sa.Specialty)
                    .Include(sa => sa.Area)
                    .Include(sa => sa.Subject)
                    .Include(sa => sa.GradeLevel)
                    .Include(sa => sa.Group)
                    .FirstOrDefaultAsync(sa =>
                        sa.SpecialtyId == model.SpecialtyId &&
                        sa.AreaId == model.AreaId &&
                        sa.SubjectId == model.SubjectId &&
                        sa.Id != model.Id &&
                        (sa.GradeLevelId != model.GradeLevelId || sa.GroupId != model.GroupId)
                    );

                if (sameSpecialtyAreaSubject != null)
                {
                    var message = $"Ya existe una asignación con la misma Especialidad, Área y Materia:\n" +
                                $"• Especialidad: {sameSpecialtyAreaSubject.Specialty.Name}\n" +
                                $"• Área: {sameSpecialtyAreaSubject.Area.Name}\n" +
                                $"• Materia: {sameSpecialtyAreaSubject.Subject.Name}\n" +
                                $"• Grado: {sameSpecialtyAreaSubject.GradeLevel.Name}\n" +
                                $"• Grupo: {sameSpecialtyAreaSubject.Group.Name}\n\n" +
                                $"Verifique que no esté duplicando la misma materia para diferentes grados o grupos.";

                    return Json(new { success = false, message = message });
                }

                // Verificar si la materia ya está asignada al mismo grupo
                var sameSubjectGroup = await _context.SubjectAssignments
                    .Include(sa => sa.Specialty)
                    .Include(sa => sa.Area)
                    .Include(sa => sa.Subject)
                    .Include(sa => sa.GradeLevel)
                    .Include(sa => sa.Group)
                    .FirstOrDefaultAsync(sa =>
                        sa.SubjectId == model.SubjectId &&
                        sa.GroupId == model.GroupId &&
                        sa.Id != model.Id
                    );

                if (sameSubjectGroup != null)
                {
                    var message = $"La materia '{subject?.Name}' ya está asignada al grupo '{group?.Name}' con:\n" +
                                $"• Especialidad: {sameSubjectGroup.Specialty.Name}\n" +
                                $"• Área: {sameSubjectGroup.Area.Name}\n" +
                                $"• Grado: {sameSubjectGroup.GradeLevel.Name}\n\n" +
                                $"Una materia no puede estar asignada al mismo grupo más de una vez.";

                    return Json(new { success = false, message = message });
                }

                // Actualizar la asignación
                subjectAssignment.SpecialtyId = model.SpecialtyId;
                subjectAssignment.AreaId = model.AreaId;
                subjectAssignment.SubjectId = model.SubjectId;
                subjectAssignment.GradeLevelId = model.GradeLevelId;
                subjectAssignment.GroupId = model.GroupId;

                _context.SubjectAssignments.Update(subjectAssignment);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Asignación actualizada correctamente." });
            }
            catch (DbUpdateException ex)
            {
                // Esto atrapa errores de base de datos como llaves duplicadas, constraints, etc.
                return Json(new { success = false, message = "Error al actualizar la asignación en la base de datos. Por favor revisa los datos." });
            }
            catch (Exception ex)
            {
                // Cualquier otro error inesperado
                return Json(new { success = false, message = "Ocurrió un error inesperado: " + ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var subjectAssignment = await _context.SubjectAssignments.FindAsync(id);
            if (subjectAssignment != null)
            {
                _context.SubjectAssignments.Remove(subjectAssignment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        // Método para carga masiva
        [HttpPost]
        public async Task<IActionResult> SaveAssignments([FromBody] List<StudentAssignmentInputModel> asignaciones)
        {
            if (asignaciones == null || asignaciones.Count == 0)
                return BadRequest(new { success = false, message = "No se recibieron asignaciones." });

            int insertadas = 0;
            int duplicadas = 0;
            var errores = new List<string>();

            foreach (var item in asignaciones)
            {
                try
                {
                    var student = await _userService.GetByEmailAsync(item.Estudiante);
                    var grade = await _gradeLevelService.GetByNameAsync(item.Grado);
                    var group = await _groupService.GetByNameAndGradeAsync(item.Grupo);

                    if (student == null || grade == null || group == null)
                    {
                        errores.Add($"Error de datos: {item.Estudiante} - {item.Grado} - {item.Grupo}");
                        continue;
                    }

                    bool exists = await _studentAssignmentService.ExistsAsync(student.Id, grade.Id, group.Id);
                    if (exists)
                    {
                        duplicadas++;
                        continue;
                    }

                    var assignment = new StudentAssignment
                    {
                        Id = Guid.NewGuid(),
                        StudentId = student.Id,
                        GradeId = grade.Id,
                        GroupId = group.Id,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _studentAssignmentService.InsertAsync(assignment);
                    insertadas++;
                }
                catch (Exception ex)
                {
                    errores.Add($"Excepción en {item.Estudiante}: {ex.Message}");
                }
            }

            return Ok(new
            {
                success = true,
                insertadas,
                duplicadas,
                errores,
                message = "Carga masiva completada."
            });
        }

        // Método para asignaciones individuales
        [HttpPost]
        public async Task<IActionResult> SaveAssignmentsSingle([FromBody] List<SubjectAssignmentPreview> asignaciones)
        {
            if (asignaciones == null || asignaciones.Count == 0)
                return BadRequest(new { success = false, message = "No se recibieron asignaciones." });

            var asignacionesCreadas = new List<string>();

            foreach (var item in asignaciones)
            {
                var materia = await _context.Subjects.FirstOrDefaultAsync(s => s.Name.ToLower() == item.Materia.ToLower());
                var grado = await _context.GradeLevels.FirstOrDefaultAsync(g => g.Name.ToLower() == item.Grado.ToLower());
                var grupo = await _context.Groups.FirstOrDefaultAsync(g => g.Name.ToLower() == item.Grupo.ToLower());

                if (materia != null && grado != null && grupo != null)
                {
                    bool yaExiste = await _context.SubjectAssignments.AnyAsync(a =>
                        a.SubjectId == materia.Id &&
                        a.GroupId == grupo.Id);

                    if (!yaExiste)
                    {
                        _context.SubjectAssignments.Add(new SubjectAssignment
                        {
                            Id = Guid.NewGuid(),
                            SubjectId = materia.Id,
                            GroupId = grupo.Id,
                            CreatedAt = DateTime.UtcNow
                        });

                        asignacionesCreadas.Add($"{materia.Name} - {grado.Name} - {grupo.Name}");
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"{asignacionesCreadas.Count} asignaciones guardadas.",
                detalles = asignacionesCreadas
            });
        }

        [HttpGet]
        public async Task<IActionResult> ChangeStatus(Guid id)
        {
            var item = await _context.SubjectAssignments.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            // Cambiar el estado (asegurándose de que sea un valor válido)
            if (item.Status == "Active")
            {
                item.Status = "Inactive";
            }
            else if (item.Status == "Inactive")
            {
                item.Status = "Active";
            }
            else
            {
                // Si el estado no es válido, retornar un error o manejarlo de alguna manera
                return BadRequest("Estado no válido.");
            }

            // Guardar los cambios en la base de datos
            try
            {
                _context.Update(item);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Manejar el error de la base de datos (si lo hay)
                var innerException = ex.InnerException?.Message;
                return BadRequest($"Error al actualizar el estado: {innerException}");
            }

            // Redirigir de vuelta a la vista
            return RedirectToAction(nameof(Index));
        }

    }
}
