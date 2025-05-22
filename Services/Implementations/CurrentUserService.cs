using Microsoft.AspNetCore.Http;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;
using System.Security.Claims;

namespace SchoolManager.Services.Implementations
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserService _userService;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor, IUserService userService)
        {
            _httpContextAccessor = httpContextAccessor;
            _userService = userService;
        }

        public async Task<Guid?> GetCurrentUserIdAsync()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return null;

            return Guid.Parse(userIdClaim.Value);
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return null;

            return await _userService.GetByIdAsync(userId.Value);
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
        }

        public async Task<string?> GetCurrentUserRoleAsync()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
        }
    }
} 