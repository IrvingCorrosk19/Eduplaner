namespace SchoolManager.Dtos
{
    public class TrimesterDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class TrimestersConfigDto
    {
        public int AnioEscolar { get; set; }
        public List<TrimesterDto> Trimestres { get; set; }
    }
}
