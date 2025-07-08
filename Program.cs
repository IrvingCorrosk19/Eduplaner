using Microsoft.EntityFrameworkCore;
using SchoolManager.Mappings;
using SchoolManager.Models;
using AutoMapper;
using SchoolManager.Services.Implementations;
using SchoolManager.Services.Interfaces;
using SchoolManager.Application.Interfaces;
using SchoolManager.Infrastructure.Services;
using SchoolManager.Services;
using SchoolManager.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using BCrypt.Net;
using SchoolManager.Middleware;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Conexión a la base de datos PostgreSQL
builder.Services.AddDbContext<SchoolDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    
    // Configurar Entity Framework para manejar DateTime automáticamente
    options.ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.RowLimitingOperationWithoutOrderByWarning));
});

// Configurar MVC para usar los convertidores JSON personalizados
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.Converters.Add(new DateTimeJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableDateTimeJsonConverter());
    })
    .AddMvcOptions(options =>
    {
        // Agregar filtro global para conversión de DateTime
        options.Filters.Add<SchoolManager.Attributes.DateTimeConversionAttribute>();
    });

// Registrando todos los servicios con inyección de dependencias
builder.Services.AddScoped<ISchoolService, SchoolService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<ITeacherAssignmentService, TeacherAssignmentService>();
builder.Services.AddScoped<ITrimesterService, TrimesterService>();
builder.Services.AddScoped<IActivityTypeService, ActivityTypeService>();
builder.Services.AddScoped<ITeacherGroupService, TeacherGroupService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IStudentActivityScoreService, StudentActivityScoreService>();

builder.Services.AddSingleton<IFileStorage, LocalFileStorage>(); // o tu propio servicio

builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IDisciplineReportService, DisciplineReportService>();
builder.Services.AddScoped<ISecuritySettingService, SecuritySettingService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

builder.Services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);
builder.Services.AddScoped<IStudentReportService, StudentReportService>();
builder.Services.AddScoped<IGradeLevelService, GradeLevelService>();
builder.Services.AddScoped<IAcademicAssignmentService, AcademicAssignmentService>();
builder.Services.AddScoped<IStudentAssignmentService, StudentAssignmentService>();
builder.Services.AddScoped<IAreaService, AreaService>();
builder.Services.AddScoped<ISpecialtyService, SpecialtyService>();
builder.Services.AddScoped<ISubjectAssignmentService, SubjectAssignmentService>();
builder.Services.AddScoped<IDirectorService, DirectorService>();
builder.Services.AddScoped<ISuperAdminService, SuperAdminService>();

// Agregar servicios de autenticación
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
    });

// Agregar configuración de autorización
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdmin", policy => policy.RequireRole("SuperAdmin"));
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Teacher", policy => policy.RequireRole("Teacher"));
    options.AddPolicy("Student", policy => policy.RequireRole("Student"));
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IMenuService, MenuService>();

// Código temporal para generar hash de contraseña
var password = "Admin123!";
var hash = BCrypt.Net.BCrypt.HashPassword(password);
Console.WriteLine($"Hash generado: {hash}");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

// Agregar middleware global para DateTime
app.UseMiddleware<DateTimeMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Usar el método de extensión para el middleware
// app.UseSessionValidation();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
