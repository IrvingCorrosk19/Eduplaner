using System.ComponentModel.DataAnnotations;

namespace SchoolManager.ViewModels;

public class UserEditViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "El nombre es requerido")]
    [Display(Name = "Nombre")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es requerido")]
    [Display(Name = "Apellido")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es requerido")]
    [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido")]
    [Display(Name = "Correo Electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El rol es requerido")]
    [Display(Name = "Rol")]
    public string Role { get; set; } = string.Empty;

    [Required(ErrorMessage = "El estado es requerido")]
    [Display(Name = "Estado")]
    public string Status { get; set; } = string.Empty;
} 