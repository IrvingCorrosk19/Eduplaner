using Microsoft.AspNetCore.Mvc;
using SchoolManager.Services.Interfaces;
using SchoolManager.Dtos;
using System;
using System.Threading.Tasks;
using SchoolManager.Models;
using SchoolManager.Services.Implementations;
using System.Linq;
using System.Collections.Generic;

public class StudentReportController : Controller
{
    private readonly IStudentReportService _reportService;
    private readonly ICurrentUserService _currentUserService;

    public StudentReportController(IStudentReportService reportService, ICurrentUserService currentUserService)
    {
        _reportService = reportService;
        _currentUserService = currentUserService;
    }

    public async Task<IActionResult> Index()
    {
        // Obtener el ID del usuario autenticado
        var studentId = await _currentUserService.GetCurrentUserIdAsync();
        if (studentId == null)
            return Unauthorized("No se pudo identificar al usuario actual.");

        // Obtener el reporte real desde el servicio (sin pasar trimestre)
        var report = await _reportService.GetReportByStudentIdAsync(studentId.Value);

        if (report == null)
        {
            // Si no hay reporte, crea un modelo vacío para mostrar la vista igual
            report = new StudentReportDto
            {
                StudentId = studentId.Value,
                Grades = new List<GradeDto>(),
                AttendanceByTrimester = new List<AttendanceDto>(),
                AttendanceByMonth = new List<AttendanceDto>(),
                AvailableTrimesters = new List<AvailableTrimesters>(),
                DisciplineReports = new List<DisciplineReportDto>()
            };
        }

        // Forzar que el trimestre seleccionado sea 1T si existe, si no el primero disponible
        var availableTrimesters = report.AvailableTrimesters.Select(t => t.Trimester).ToList();
        string selectedTrimester = availableTrimesters.Contains("1T") ? "1T" : availableTrimesters.FirstOrDefault();
        if (selectedTrimester != null && report.Trimester != selectedTrimester)
        {
            // Volver a pedir el reporte solo para el trimestre seleccionado
            report = await _reportService.GetReportByStudentIdAndTrimesterAsync(studentId.Value, selectedTrimester) ?? report;
            report.AvailableTrimesters = availableTrimesters.Select(t => new AvailableTrimesters { Trimester = t }).ToList();
        }
        report.StudentId = studentId.Value;
        ViewBag.AvailableTrimesters = report.AvailableTrimesters;
        return View(report);
    }

    
    public async Task<IActionResult> GetTrimesterData(Guid studentId, string trimester)
    {
        try
        {
            var report = await _reportService.GetReportByStudentIdAndTrimesterAsync(studentId, trimester);

            if (report == null)
            {
                return Json(new { error = "No se encontraron datos para el trimestre seleccionado." });
            }

            return Json(new
            {
                grades = report.Grades.Select(g => new
                {
                    subject = g.Subject,
                    activityName = g.ActivityName,
                    teacher = g.Teacher,
                    value = g.Value,
                    fileUrl = g.FileUrl,
                    type = g.Type
                }),
                trimester = report.Trimester,
                attendanceByTrimester = report.AttendanceByTrimester.Select(a => new {
                    trimester = a.Trimester,
                    present = a.Present,
                    absent = a.Absent,
                    late = a.Late
                }),
                attendanceByMonth = report.AttendanceByMonth.Select(a => new {
                    month = a.Month,
                    present = a.Present,
                    absent = a.Absent,
                    late = a.Late
                }),
                disciplineReports = report.DisciplineReports.Select(r => new {
                    type = r.Type ?? "",
                    status = r.Status ?? "",
                    description = r.Description ?? "",
                    date = r.Date.ToString("yyyy-MM-dd"),
                    time = r.Time ?? "",
                    teacher = r.Teacher ?? ""
                }),
                pendingActivities = report.PendingActivities.Select(a => new {
                    activityId = a.ActivityId,
                    name = a.Name,
                    type = a.Type,
                    subjectName = a.SubjectName,
                    createdAt = a.CreatedAt.ToString("yyyy-MM-dd"),
                    fileUrl = a.FileUrl,
                    teacherName = a.TeacherName
                })
            });
        }
        catch (Exception ex)
        {
            return Json(new { error = ex.Message, stack = ex.StackTrace });
        }
    }




}


