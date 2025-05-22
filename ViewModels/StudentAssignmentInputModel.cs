namespace SchoolManager.ViewModels
{
    public class StudentAssignmentInputModel
    {
        public string Estudiante { get; set; } = string.Empty; // Email
        //public string Materia { get; set; } = string.Empty;    // Código de materia
        public string Grado { get; set; } = string.Empty;      // Nombre del grado
        public string Grupo { get; set; } = string.Empty;      // Nombre del grupo
    }
}
