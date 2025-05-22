using System.Collections.Generic;
using System.Threading.Tasks;
using SchoolManager.Dtos;

namespace SchoolManager.Services.Interfaces
{
    public interface ITrimesterService
    {
        Task<List<TrimesterDto>> GetAllAsync();
        Task GuardarTrimestresAsync(List<TrimesterDto> trimestres);
        Task<bool> EditarFechasTrimestreAsync(TrimesterDto dto);
        Task EliminarTodosLosTrimestresAsync();
    }
}
