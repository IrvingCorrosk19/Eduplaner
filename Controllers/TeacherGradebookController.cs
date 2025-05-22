using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Dtos;
using SchoolManager.Interfaces;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;
using SchoolManager.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace SchoolManager.Controllers
{
    [Authorize(Roles = "teacher")]
    public class TeacherGradebookController : Controller
    {
        private readonly ITrimesterService _trimesterSvc;
        private readonly ITeacherGroupService _groupSvc;
        private readonly IActivityTypeService _typeSvc;
        private readonly IActivityService _activitySvc;
        private readonly IStudentActivityScoreService _scoreSvc;
        private readonly IUserService _userService;
        private readonly IStudentService _studentService;
        private readonly IAttendanceService _attendanceService;


        public TeacherGradebookController(
            ITrimesterService trimesterSvc,
            ITeacherGroupService groupSvc,
            IActivityTypeService typeSvc,
            IActivityService activitySvc,
            IStudentActivityScoreService scoreSvc,
            IUserService userService,
            IStudentService studentService,
            IAttendanceService attendanceService)
            
        {
            _studentService = studentService;   
            _trimesterSvc = trimesterSvc;
            _groupSvc = groupSvc;
            _typeSvc = typeSvc;
            _activitySvc = activitySvc;
            _scoreSvc = scoreSvc;
            _userService = userService;
            _attendanceService = attendanceService;
            
        }


        [HttpPost]
        public async Task<IActionResult> GuardarNotasTemp([FromBody] List<StudentNotaDto> data)  
        {
            try
        {
            if (data == null || !data.Any())
                return BadRequest("No se recibió información de notas.");

            var registros = new List<StudentActivityScoreCreateDto>();

            foreach (var alumno in data)
            {
                    if (!Guid.TryParse(alumno.StudentId, out var studentId) ||
                        !Guid.TryParse(alumno.SubjectId, out var subjectId) ||
                        !Guid.TryParse(alumno.GradeLevelId, out var gradeLevelId) ||
                        !Guid.TryParse(alumno.GroupId, out var groupId) ||
                        !Guid.TryParse(alumno.TeacherId, out var teacherId))
                    {
                        return BadRequest("Uno o más IDs tienen un formato inválido.");
                    }

                foreach (var nota in alumno.Notas)
                {
                        if (!decimal.TryParse(nota.Nota, out var score))
                        {
                            return BadRequest($"La nota '{nota.Nota}' tiene un formato inválido.");
                        }

                    registros.Add(new StudentActivityScoreCreateDto
                    {
                            StudentId = studentId,
                        ActivityName = nota.Actividad,
                        Type = nota.Tipo,
                            Score = score,
                            SubjectId = subjectId,
                            GradeLevelId = gradeLevelId,
                            GroupId = groupId,
                            TeacherId = teacherId,
                        Trimester = alumno.Trimester
                    });
                }
            }

            await _scoreSvc.SaveBulkFromNotasAsync(registros);

            return Ok(new { message = "Notas procesadas y guardadas correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al guardar las notas: {ex.Message}");
            }
        }



        [HttpPost]
        public async Task<IActionResult> GetNotasCargadas([FromBody] GetNotesDto notes)
        {
            if (notes == null)
            {
                return BadRequest("Los datos están incompletos.");
            }

            // Obtener todas las actividades del grupo, materia, docente y trimestre
            var activities = await _activitySvc.GetByTeacherGroupTrimesterAsync(notes.TeacherId, notes.GroupId, notes.Trimester);
            var actividadesPorTipo = activities.GroupBy(a => a.Type.ToLower()).ToDictionary(g => g.Key, g => g.ToList());

            // Obtener las notas existentes (como antes)
            var notas = await _scoreSvc.GetNotasPorFiltroAsync(notes);
            var estudiantes = notas.Select(n => n.StudentId).Distinct().ToList();

            // Si no hay estudiantes con notas, obtener la lista de estudiantes del grupo
            if (!estudiantes.Any())
            {
                // Usar el servicio para obtener los estudiantes del grupo
                var students = await _studentService.GetByGroupAndGradeAsync(notes.GroupId, notes.GradeLevelId);
                estudiantes = students.Select(s => s.StudentId.ToString()).ToList();
            }

            // Obtener información de todos los estudiantes
            var studentsInfo = await _studentService.GetByGroupAndGradeAsync(notes.GroupId, notes.GradeLevelId);
            var studentDict = studentsInfo.ToDictionary(s => s.StudentId.ToString(), s => s.FullName);

            // Construir la respuesta para cada estudiante
            var data = estudiantes.Select(studentId => {
                var alumno = notas.FirstOrDefault(n => n.StudentId == studentId);
                var notasAlumno = alumno?.Notas ?? new List<NotaDetalleDto>();
                var notasPorActividad = new List<object>();

                foreach (var tipo in actividadesPorTipo.Keys)
                {
                    foreach (var act in actividadesPorTipo[tipo])
                    {
                        var nota = notasAlumno.FirstOrDefault(n => n.Tipo.ToLower() == tipo && n.Actividad == act.Name);
                        notasPorActividad.Add(new {
                            tipo = tipo,
                            actividad = act.Name,
                            nota = nota != null ? nota.Nota : null,
                            pdfUrl = act.PdfUrl
                        });
                    }
                }

                return new {
                    studentId = studentId,
                    fullName = studentDict.GetValueOrDefault(studentId, ""),
                    notas = notasPorActividad
                };
            }).ToList();

            return Json(data);
        }



        [HttpGet]
        public async Task<JsonResult> StudentsByGroupAndGrade(Guid groupId, Guid gradeId, Guid? subjectId = null)
        {
            IEnumerable<StudentBasicDto> students;
            if (subjectId.HasValue && subjectId.Value != Guid.Empty)
            {
                // Filtrar por materia, grupo y grado
                students = await _studentService.GetBySubjectGroupAndGradeAsync(subjectId.Value, groupId, gradeId);
            }
            else
            {
                // Filtrar solo por grupo y grado (comportamiento anterior)
                students = await _studentService.GetByGroupAndGradeAsync(groupId, gradeId);
            }
            return Json(students);
        }
        private Guid GetTeacherId()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("Usuario no autenticado.");
            }

            if (!Guid.TryParse(userId, out var teacherId))
            {
                throw new UnauthorizedAccessException("ID de usuario inválido.");
            }

            return teacherId;
        }






        public async Task<IActionResult> Index()
        {
            var teacherId = GetTeacherId();

            // 🔹 Obtener todos los docentes con asignaciones y filtrar solo al actual
            var teachers = await _userService.GetAllWithAssignmentsByRoleAsync("teacher");
            var teacher = teachers.FirstOrDefault(t => t.Id == teacherId);

            if (teacher == null)
                return NotFound("No se encontró el docente actual.");

            // 🔹 Preparar DTO visual con materias y grupos
            var assignments = teacher.TeacherAssignments ?? new List<TeacherAssignment>();

            var subjectGroupDetails = assignments.Any()
               ? assignments
                   .GroupBy(a => new
                   {
                       SubjectId = a.SubjectAssignment.SubjectId,
                       SubjectName = a.SubjectAssignment.Subject?.Name ?? "(Sin materia)"
                   })
                   .Select(g => new SubjectGroupSummary
                   {
                       SubjectId = g.Key.SubjectId,
                       SubjectName = g.Key.SubjectName,
                       GroupGradePairs = g.Select(x => new GroupGradeItem
                       {
                           GroupId = x.SubjectAssignment.GroupId,
                           GradeLevelId = x.SubjectAssignment.GradeLevelId,
                           GroupName = x.SubjectAssignment.Group?.Name ?? "(Grupo)",
                           GradeLevelName = x.SubjectAssignment.GradeLevel?.Name ?? "(Grado)"
                       })
                       .Distinct()
                       .ToList()
                   }).ToList()
               : new List<SubjectGroupSummary>();


            var teacherInfo = new TeacherAssignmentDisplayDto
            {
                TeacherId = teacher.Id,
                FullName = teacher.Name,
                Email = teacher.Email,
                Role = teacher.Role,
                IsActive = string.Equals(teacher.Status, "active", StringComparison.OrdinalIgnoreCase),
                SubjectGroupDetails = subjectGroupDetails
            };

            // 🔹 Obtener catálogo para filtros
            var trimesters = (await _trimesterSvc.GetAllAsync()).ToList();
            var firstTrim = trimesters.FirstOrDefault()?.Name ?? "";
            var groups = await _groupSvc.GetByTeacherAsync(teacherId, firstTrim);
            var types = await _typeSvc.GetAllAsync();

            // 🔹 ViewModel que combinamos con datos del docente y catálogos
            var viewModel = new TeacherGradebookViewModel
            {
                Teacher = teacherInfo,
                Trimesters = trimesters,
                Groups = groups,
                Types = types,
                TeacherId = teacherId
            };

            return View(viewModel);
        }





        // GET: /TeacherGradebook/GradeBookJson?groupId=...&trimester=...
        [HttpGet]
        public async Task<JsonResult> GradeBookJson(Guid groupId, string trimester)
        {
            var teacherId = GetTeacherId();
            var book = await _scoreSvc.GetGradeBookAsync(teacherId, groupId, trimester);
            return Json(book);
        }






        // POST: /TeacherGradebook/CreateActivity
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(52428800)]
        public async Task<JsonResult> CreateActivity([FromForm] ActivityCreateDto dto)
        {
            try
            {
                var result = await _activitySvc.CreateAsync(dto);
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }






        // POST: /TeacherGradebook/SaveScores
        [HttpPost]
        public async Task<IActionResult> SaveScores([FromBody] StudentActivityScoreCreateDto[] scores)
        {
            try
            {
                await _scoreSvc.SaveAsync(scores);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }




        // DELETE: /TeacherGradebook/DeleteActivity/{id}
        [HttpDelete]
        public async Task<IActionResult> DeleteActivity(Guid id)
        {
            try
            {
                await _activitySvc.DeleteAsync(id);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetPromediosFinales([FromBody] GetNotesDto notes)
        {
            if (notes == null)
            {
                return BadRequest("Los datos están incompletos.");
            }

            try
            {
                var promedios = await _scoreSvc.GetPromediosFinalesAsync(notes);
                return Json(new { 
                    success = true, 
                    data = promedios 
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    error = ex.Message 
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveAttendances([FromBody] List<AttendanceSaveDto> attendances)
        {
            try
            {
                await _attendanceService.SaveAttendancesAsync(attendances);
                return Ok(new { success = true, message = "Asistencias guardadas correctamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
