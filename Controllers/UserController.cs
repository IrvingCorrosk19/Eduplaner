using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SchoolManager.Enums;
using SchoolManager.Models;
using SchoolManager.ViewModels;
using Microsoft.AspNetCore.Authorization;

[Authorize(Roles = "admin")]
public class UserController : Controller
{
    private readonly IUserService _userService;
    private readonly ISubjectService _subjectService;
    private readonly IGroupService _groupService;
    private readonly IMapper _mapper;

    public UserController(
        IUserService userService,
        ISubjectService subjectService,
        IGroupService groupService,
        IMapper mapper)
    {
        _userService = userService;
        _subjectService = subjectService;
        _groupService = groupService;
        _mapper = mapper;
    }

    [HttpPost]
    public async Task<IActionResult> CreateJson([FromBody] CreateUserViewModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Validar formato del correo electrónico
        if (!IsValidEmail(model.Email))
        {
            return BadRequest(new { message = "El formato del correo electrónico no es válido" });
        }

        // Validar si el correo electrónico ya existe
        var existingUser = await _userService.GetByEmailAsync(model.Email);
        if (existingUser != null)
        {
            return BadRequest(new { message = "El correo electrónico ya está registrado en el sistema" });
        }

        // Validar fortaleza de la contraseña
        if (!string.IsNullOrEmpty(model.PasswordHash) && !IsStrongPassword(model.PasswordHash))
        {
            return BadRequest(new { message = "La contraseña debe tener al menos 8 caracteres, una mayúscula, una minúscula, un número y un carácter especial" });
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = model.Name,
            LastName = model.LastName,
            Email = model.Email,
            DocumentId = model.DocumentId,
            Role = model.Role.ToLower(),
            Status = model.Status,
            CreatedAt = DateTime.UtcNow,
            PasswordHash = model.PasswordHash ?? "123456",
            DateOfBirth = model.DateOfBirth.ToUniversalTime(),
        };

        await _userService.CreateAsync(user, model.Subjects, model.Groups);

        return Ok(new { message = "Usuario creado correctamente", id = user.Id });
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private bool IsStrongPassword(string password)
    {
        // La contraseña debe tener:
        // - Al menos 8 caracteres
        // - Al menos una letra mayúscula
        // - Al menos una letra minúscula
        // - Al menos un número
        // - Al menos un carácter especial
        var hasNumber = password.Any(char.IsDigit);
        var hasUpperChar = password.Any(char.IsUpper);
        var hasLowerChar = password.Any(char.IsLower);
        var hasSpecialChar = password.Any(c => !char.IsLetterOrDigit(c));
        var hasMinLength = password.Length >= 8;

        return hasNumber && hasUpperChar && hasLowerChar && hasSpecialChar && hasMinLength;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Roles = Enum.GetValues(typeof(UserRole))
      .Cast<UserRole>()
      .Where(r => r != UserRole.Superadmin && r != UserRole.Admin && r != UserRole.Student)
      .Select(r => r.ToString())
      .ToList();


        var users = await _userService.GetAllAsync();
        return View(users);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null) return NotFound();
        return View(user);
    }

    public IActionResult Create() => View();

    [HttpPost]
    public async Task<IActionResult> Create(User user)
    {
        if (ModelState.IsValid)
        {
            return RedirectToAction(nameof(Index));
        }
        return View(user);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null) return NotFound();
        return View(user);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(User user)
    {
        if (ModelState.IsValid)
        {
            await _userService.UpdateAsync(user);
            return RedirectToAction(nameof(Index));
        }
        return View(user);
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null) return NotFound();
        return View(user);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _userService.DeleteAsync(id);
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetUserJson(Guid id)
    {
        var user = await _userService.GetByIdWithRelationsAsync(id);
        if (user == null) return NotFound();

        var result = new
        {
            user.Id,
            user.Name,
            user.LastName,
            user.Email,
            user.DocumentId,
            user.PasswordHash,
            Role = char.ToUpper(user.Role[0]) + user.Role.Substring(1).ToLower(),
            user.Status,
            user.DateOfBirth,
            Subjects = user.Subjects.Select(s => s.Id),
            Groups = user.Groups.Select(g => g.Id)
        };

        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateJson([FromBody] CreateUserViewModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existingUser = await _userService.GetByIdWithRelationsAsync(model.Id);
        if (existingUser == null)
            return NotFound(new { message = "Usuario no encontrado" });

        existingUser.Name = model.Name;
        existingUser.LastName = model.LastName;
        existingUser.Email = model.Email;
        existingUser.DocumentId = model.DocumentId;
        existingUser.Role = model.Role.ToLower();
        existingUser.Status = model.Status;
        existingUser.DateOfBirth = model.DateOfBirth.ToUniversalTime();

        if (!string.IsNullOrEmpty(model.PasswordHash))
        {
            existingUser.PasswordHash = model.PasswordHash;
        }

        await _userService.UpdateAsync(existingUser, model.Subjects, model.Groups);

        return Ok(new { message = "Usuario actualizado correctamente" });
    }
}
