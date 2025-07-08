using Microsoft.AspNetCore.Mvc;
using SchoolManager.ViewModels;
using SchoolManager.Services.Interfaces; // Asegúrate de incluir los namespaces correctos
using Microsoft.AspNetCore.Authorization;
using SchoolManager.Dtos;

[Authorize(Roles = "admin")]
public class AcademicCatalogController : Controller
{
    private readonly IGradeLevelService _gradeLevelService;
    private readonly IGroupService _groupService;
    private readonly ISubjectService _subjectService;
    private readonly IAreaService _areaService;
    private readonly ISpecialtyService _specialtyService;
    private readonly ITrimesterService _trimesterService;

    public AcademicCatalogController(
        IGradeLevelService gradeLevelService,
        IGroupService groupService,
        ISubjectService subjectService,
        IAreaService areaService,
        ISpecialtyService specialtyService,
        ITrimesterService trimesterService)
    {
        _gradeLevelService = gradeLevelService;
        _groupService = groupService;
        _subjectService = subjectService;
        _areaService = areaService;
        _specialtyService = specialtyService;
        _trimesterService = trimesterService;
    }

    public async Task<IActionResult> Index()
    {
        // Asegurarse de que se utiliza el tipo GradeLevel correctamente
        var model = new AcademicCatalogViewModel
        {
            GradesLevel = await _gradeLevelService.GetAllAsync(), // Esto debe devolver IEnumerable<GradeLevel>
            Groups = await _groupService.GetAllAsync(),//0
            Subjects = await _subjectService.GetAllAsync(),//0
            Areas = await _areaService.GetAllAsync(),//0
            Specialties = await _specialtyService.GetAllAsync(),
            Trimestres = await _trimesterService.GetAllAsync()
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> GuardarTrimestres([FromBody] List<SchoolManager.Dtos.TrimesterDto> trimestres)
    {
        try
        {
            if (trimestres == null || !trimestres.Any())
                return BadRequest(new { success = false, message = "No se recibieron datos de trimestres." });

            // Validar trimestres antes de guardar
            var validacion = await _trimesterService.ValidarTrimestresAsync(trimestres);
            if (!validacion.IsValid)
            {
                return BadRequest(new { 
                    success = false, 
                    message = "Error de validación en la configuración de trimestres.",
                    errors = validacion.Errors,
                    warnings = validacion.Warnings
                });
            }

            await _trimesterService.GuardarTrimestresAsync(trimestres);
            return Ok(new { 
                success = true, 
                message = "Trimestres guardados correctamente.",
                warnings = validacion.Warnings
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor al guardar los trimestres." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetTrimestres()
    {
        try
        {
            var trimestres = await _trimesterService.GetAllAsync();
            return Json(trimestres);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error al obtener los trimestres." });
        }
    }

    [HttpPost]
    public async Task<IActionResult> EditarTrimestre([FromBody] SchoolManager.Dtos.TrimesterDto dto)
    {
        try
        {
            if (dto == null || dto.Id == Guid.Empty)
                return BadRequest(new { success = false, message = "Datos de trimestre inválidos." });

            if (dto.StartDate >= dto.EndDate)
                return BadRequest(new { success = false, message = "La fecha de inicio debe ser anterior a la fecha de fin." });

            var result = await _trimesterService.EditarFechasTrimestreAsync(dto);
            if (!result)
                return NotFound(new { success = false, message = "Trimestre no encontrado." });
            
            return Ok(new { success = true, message = "Fechas actualizadas correctamente." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor al actualizar el trimestre." });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ValidarConfiguracionTrimestres([FromBody] List<SchoolManager.Dtos.TrimesterDto> trimestres)
    {
        try
        {
            var validacion = await _trimesterService.ValidarTrimestresAsync(trimestres);
            return Ok(new { 
                success = validacion.IsValid, 
                errors = validacion.Errors,
                warnings = validacion.Warnings
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error al validar la configuración." });
        }
    }

    [HttpPost]
    public async Task<IActionResult> EliminarTodosLosTrimestres()
    {
        try
        {
            await _trimesterService.EliminarTodosLosTrimestresAsync();
            return Ok(new { success = true, message = "Todos los trimestres eliminados correctamente." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error al eliminar los trimestres." });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ActivarTrimestre([FromBody] TrimesterActivationDto dto)
    {
        try
        {
            if (dto == null || dto.Id == Guid.Empty)
                return BadRequest(new { success = false, message = "ID de trimestre inválido." });

            var result = await _trimesterService.ActivarTrimestreAsync(dto.Id);
            if (!result)
                return NotFound(new { success = false, message = "Trimestre no encontrado." });
            
            return Ok(new { success = true, message = "Trimestre activado correctamente." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor al activar el trimestre." });
        }
    }

    [HttpPost]
    public async Task<IActionResult> DesactivarTrimestre([FromBody] TrimesterActivationDto dto)
    {
        try
        {
            if (dto == null || dto.Id == Guid.Empty)
                return BadRequest(new { success = false, message = "ID de trimestre inválido." });

            var result = await _trimesterService.DesactivarTrimestreAsync(dto.Id);
            if (!result)
                return NotFound(new { success = false, message = "Trimestre no encontrado." });
            
            return Ok(new { success = true, message = "Trimestre desactivado correctamente." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor al desactivar el trimestre." });
        }
    }
}
