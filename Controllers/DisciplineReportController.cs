using Microsoft.AspNetCore.Mvc;
using SchoolManager.Models;
using SchoolManager.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class DisciplineReportController : Controller
{
    private readonly IDisciplineReportService _disciplineReportService;
    private readonly IUserService _userService;
    private readonly ILogger<DisciplineReportController> _logger;

    public DisciplineReportController(
        IDisciplineReportService disciplineReportService, 
        IUserService userService,
        ILogger<DisciplineReportController> logger)
    {
        _disciplineReportService = disciplineReportService;
        _userService = userService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var reports = await _disciplineReportService.GetAllAsync();
        return View(reports);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var report = await _disciplineReportService.GetByIdAsync(id);
        if (report == null) return NotFound();
        return View(report);
    }

    public IActionResult Create() => View();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DisciplineReportCreateDto report)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, error = "Datos inválidos", details = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)) });
            }

            var disciplineReport = new DisciplineReport
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.Parse(report.StudentId),
                TeacherId = Guid.Parse(report.TeacherId),
                SubjectId = !string.IsNullOrEmpty(report.SubjectId) ? Guid.Parse(report.SubjectId) : (Guid?)null,
                GroupId = !string.IsNullOrEmpty(report.GroupId) ? Guid.Parse(report.GroupId) : (Guid?)null,
                GradeLevelId = !string.IsNullOrEmpty(report.GradeLevelId) ? Guid.Parse(report.GradeLevelId) : (Guid?)null,
                Date = DateOnly.Parse(report.Date),
                Hora = !string.IsNullOrEmpty(report.Hora) ? TimeOnly.Parse(report.Hora) : null,
                ReportType = report.ReportType,
                Status = report.Status,
                Description = report.Description,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
            };

            try
            {
                await _disciplineReportService.CreateAsync(disciplineReport);
                return Json(new { success = true, message = "Registro guardado correctamente" });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error al guardar en la base de datos");
                return Json(new { success = false, error = "Error al guardar en la base de datos", details = ex.InnerException?.Message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear el reporte de disciplina");
            return Json(new { success = false, error = "Error al crear el reporte", details = ex.Message });
        }
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var report = await _disciplineReportService.GetByIdAsync(id);
        if (report == null) return NotFound();
        return View(report);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(DisciplineReport report)
    {
        if (ModelState.IsValid)
        {
            await _disciplineReportService.UpdateAsync(report);
            return RedirectToAction(nameof(Index));
        }
        return View(report);
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var report = await _disciplineReportService.GetByIdAsync(id);
        if (report == null) return NotFound();
        return View(report);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _disciplineReportService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetByStudent(Guid studentId)
    {
        var reports = await _disciplineReportService.GetByStudentDtoAsync(studentId);
        return Json(reports.Select(r => new {
            date = r.Date,
            time = r.Time,
            type = r.Type,
            status = r.Status,
            description = r.Description,
            teacher = r.Teacher
        }));
    }

    [HttpGet]
    public async Task<IActionResult> GetFiltered(DateTime? fechaInicio, DateTime? fechaFin, Guid? gradoId, Guid? groupId = null, Guid? studentId = null)
    {
        if (!gradoId.HasValue)
        {
            return BadRequest(new { error = "El grado es obligatorio" });
        }

        try
        {
            var reports = await _disciplineReportService.GetFilteredAsync(fechaInicio, fechaFin, gradoId, groupId, studentId);
            
            var result = reports.Select(r => new {
                estudiante = r.Student != null ? $"{r.Student.Name} {r.Student.LastName}" : null,
                fecha = r.Date.ToString("dd/MM/yyyy"),
                hora = r.Hora?.ToString("HH:mm"),
                tipo = r.ReportType,
                status = r.Status,
                description = r.Description,
                grupo = r.Group?.Name,
                grado = r.GradeLevel?.Name
            });

            return Json(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportToExcel(DateTime? fechaInicio, DateTime? fechaFin, Guid? gradoId)
    {
        var reports = await _disciplineReportService.GetFilteredAsync(fechaInicio, fechaFin, gradoId);
        var csv = "Estudiante,Fecha,Hora,Tipo,Estado,Descripción\n" +
            string.Join("\n", reports.Select(r => $"{(r.Student != null ? r.Student.Name : "")},{r.Date:yyyy-MM-dd},{r.CreatedAt?.ToString("HH:mm") ?? ""},{r.ReportType},{r.Status},{r.Description}"));
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", "registros_disciplina.csv");
    }
}
