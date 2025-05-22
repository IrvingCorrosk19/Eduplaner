using SchoolManager.Models;

public interface IStudentAssignmentService
{
    Task<List<StudentAssignment>> GetAssignmentsByStudentIdAsync(Guid studentId);

    Task AssignAsync(Guid studentId, List<(Guid SubjectId, Guid GradeId, Guid GroupId)> assignments);

    Task<bool> AssignStudentAsync(Guid studentId, Guid subjectId, Guid gradeId, Guid groupId); // ← NUEVO

    Task RemoveAssignmentsAsync(Guid studentId);

    Task BulkAssignFromFileAsync(List<(string StudentEmail, string SubjectCode, string GradeName, string GroupName)> rows);
    Task<bool> ExistsAsync(Guid studentId, Guid gradeId, Guid groupId);
    Task InsertAsync(StudentAssignment assignment);


}
