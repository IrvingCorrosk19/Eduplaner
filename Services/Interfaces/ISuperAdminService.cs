using SchoolManager.Models;
using SchoolManager.ViewModels;

namespace SchoolManager.Services.Interfaces;

public interface ISuperAdminService
{
    // Escuelas
    Task<List<SchoolListViewModel>> GetAllSchoolsAsync(string? searchString = null);
    Task<School?> GetSchoolByIdAsync(Guid id);
    Task<SchoolAdminViewModel?> GetSchoolForEditAsync(Guid id);
    Task<bool> CreateSchoolWithAdminAsync(SchoolAdminViewModel model, IFormFile? logoFile, string uploadsPath);
    Task<bool> UpdateSchoolAsync(SchoolAdminEditViewModel model, IFormFile? logoFile, string uploadsPath);
    Task<bool> DeleteSchoolAsync(Guid id);
    Task<SchoolAdminEditViewModel?> GetSchoolForEditWithAdminAsync(Guid id);
    
    // Usuarios
    Task<List<User>> GetAllAdminsAsync();
    Task<User?> GetUserByIdAsync(Guid id);
    Task<UserEditViewModel?> GetUserForEditAsync(Guid id);
    Task<bool> UpdateUserAsync(UserEditViewModel model);
    Task<bool> DeleteUserAsync(Guid id);
    
    // Diagnóstico
    Task<object> DiagnoseSchoolAsync(Guid id);
    
    // Archivos
    Task<string?> SaveLogoAsync(IFormFile? logoFile, string uploadsPath);
    Task<string?> SaveAvatarAsync(IFormFile? avatarFile, string uploadsPath);
    Task<byte[]?> GetLogoAsync(string? logoUrl);
    Task<byte[]?> GetAvatarAsync(string? avatarUrl);
} 