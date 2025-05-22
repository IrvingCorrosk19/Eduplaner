using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly ISubjectService _subjectService;
        private readonly IGroupService _groupService;
        private readonly IGradeLevelService _gradeLevelService;
        private readonly IStudentAssignmentService _studentAssignmentService;

        public SubjectAssignmentController(
            SchoolDbContext context,
            IUserService userService,
            ISubjectService subjectService,
            IGroupService groupService,
            IGradeLevelService gradeLevelService,
            IStudentAssignmentService studentAssignmentService)
        {
            _context = context;
            _userService = userService;
            _subjectService = subjectService;
            _groupService = groupService;
            _gradeLevelService = gradeLevelService;
            _studentAssignmentService = studentAssignmentService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var subjectAssignments = await _context.SubjectAssignments
                .Include(sa => sa.Specialty)
                .Include(sa => sa.Area)
                .Include(sa => sa.Subject)
                .Include(sa => sa.GradeLevel)
                .Include(sa => sa.Group)
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
            var allAssignments = await _context.SubjectAssignments
                .Include(sa => sa.Specialty)
                .Include(sa => sa.Area)
                .Include(sa => sa.Subject)
                .Include(sa => sa.GradeLevel)
                .Include(sa => sa.Group)
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

                // Verificar que no exista otra asignación igual
                var exists = await _context.SubjectAssignments.AnyAsync(sa =>
                    sa.SpecialtyId == model.SpecialtyId &&
                    sa.AreaId == model.AreaId &&
                    sa.SubjectId == model.SubjectId &&
                    sa.GradeLevelId == model.GradeLevelId &&
                    sa.GroupId == model.GroupId &&
                    sa.Id != model.Id
                );

                if (exists)
                {
                    return Json(new { success = false, message = "Ya existe una asignación con los mismos datos." });
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
