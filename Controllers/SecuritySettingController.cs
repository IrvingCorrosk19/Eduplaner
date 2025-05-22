using Microsoft.AspNetCore.Mvc;
using SchoolManager.Models;

public class SecuritySettingController : Controller
{
    private readonly ISecuritySettingService _securitySettingService;

    public SecuritySettingController(ISecuritySettingService securitySettingService)
    {
        _securitySettingService = securitySettingService;
    }

    public async Task<IActionResult> Index()
    {
        var settings = await _securitySettingService.GetAllAsync();
        return View(settings);
    }

    public async Task<IActionResult> Details(Guid schoolId)
    {
        var setting = await _securitySettingService.GetBySchoolIdAsync(schoolId);
        if (setting == null) return NotFound();
        return View(setting);
    }

    public IActionResult Create() => View();

    [HttpPost]
    public async Task<IActionResult> Create(SecuritySetting setting)
    {
        if (ModelState.IsValid)
        {
            await _securitySettingService.CreateAsync(setting);
            return RedirectToAction(nameof(Index));
        }
        return View(setting);
    }

    public async Task<IActionResult> Edit(Guid schoolId)
    {
        var setting = await _securitySettingService.GetBySchoolIdAsync(schoolId);
        if (setting == null) return NotFound();
        return View(setting);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(SecuritySetting setting)
    {
        if (ModelState.IsValid)
        {
            await _securitySettingService.UpdateAsync(setting);
            return RedirectToAction(nameof(Index));
        }
        return View(setting);
    }
}
