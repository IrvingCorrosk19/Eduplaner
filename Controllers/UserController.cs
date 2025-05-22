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

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = model.Name,
            LastName = model.LastName,
            Email = model.Email,
            DocumentId = model.DocumentId,
            Role = model.Role.ToLower(),
            Status = model.Status,
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
            PasswordHash = model.PasswordHash ?? "123456",
            DateOfBirth = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
        };

        await _userService.CreateAsync(user, model.Subjects, model.Groups);

        return Ok(new { message = "Usuario creado correctamente", id = user.Id });
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Roles = Enum.GetNames(typeof(UserRole)).ToList();

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
        existingUser.DateOfBirth = model.DateOfBirth;

        if (!string.IsNullOrEmpty(model.PasswordHash))
        {
            existingUser.PasswordHash = model.PasswordHash;
        }

        await _userService.UpdateAsync(existingUser, model.Subjects, model.Groups);

        return Ok(new { message = "Usuario actualizado correctamente" });
    }
}
