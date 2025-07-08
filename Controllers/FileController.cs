using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Controllers;

[Authorize]
public class FileController : Controller
{
    private readonly ISuperAdminService _superAdminService;
    private readonly ILogger<FileController> _logger;

    public FileController(ISuperAdminService superAdminService, ILogger<FileController> logger)
    {
        _superAdminService = superAdminService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetSchoolLogo(string logoUrl)
    {
        if (string.IsNullOrEmpty(logoUrl))
        {
            return NotFound();
        }

        try
        {
            var bytes = await _superAdminService.GetLogoAsync(logoUrl);
            if (bytes == null)
            {
                return NotFound();
            }

            return File(bytes, "image/jpeg");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo logo de escuela: {logoUrl}", logoUrl);
            return NotFound();
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetUserAvatar(string avatarUrl)
    {
        if (string.IsNullOrEmpty(avatarUrl))
        {
            return NotFound();
        }

        try
        {
            var bytes = await _superAdminService.GetAvatarAsync(avatarUrl);
            if (bytes == null)
            {
                return NotFound();
            }

            return File(bytes, "image/jpeg");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo avatar de usuario: {avatarUrl}", avatarUrl);
            return NotFound();
        }
    }
} 