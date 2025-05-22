using Microsoft.AspNetCore.Mvc;
using SchoolManager.ViewModels;
using SchoolManager.Services.Interfaces; // Asegúrate de incluir los namespaces correctos
using Microsoft.AspNetCore.Authorization;

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
            Groups = await _groupService.GetAllAsync(),
            Subjects = await _subjectService.GetAllAsync(),
            Areas = await _areaService.GetAllAsync(),
            Specialties = await _specialtyService.GetAllAsync(),
            Trimestres = await _trimesterService.GetAllAsync()
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> GuardarTrimestres([FromBody] List<SchoolManager.Dtos.TrimesterDto> trimestres)
    {
        if (trimestres == null || !trimestres.Any())
            return BadRequest("No se recibieron datos de trimestres.");

        // Puedes agregar lógica para eliminar los trimestres existentes del mismo año si lo deseas
        await _trimesterService.GuardarTrimestresAsync(trimestres);
        return Ok(new { success = true, message = "Trimestres guardados correctamente." });
    }

    [HttpGet]
    public async Task<IActionResult> GetTrimestres()
    {
        var trimestres = await _trimesterService.GetAllAsync();
        return Json(trimestres);
    }

    [HttpPost]
    public async Task<IActionResult> EditarTrimestre([FromBody] SchoolManager.Dtos.TrimesterDto dto)
    {
        var result = await _trimesterService.EditarFechasTrimestreAsync(dto);
        if (!result)
            return NotFound(new { success = false, message = "Trimestre no encontrado." });
        return Ok(new { success = true, message = "Fechas actualizadas correctamente." });
    }

  

    [HttpPost]
    public async Task<IActionResult> EliminarTodosLosTrimestres()
    {
        await _trimesterService.EliminarTodosLosTrimestresAsync();
        return Ok(new { success = true, message = "Todos los trimestres eliminados correctamente." });
    }
}
