using BlazorS7Upload.Models;
using Google.Cloud.BigQuery.V2;
using System.Data;

namespace BlazorS7Upload.Interfaces
{
    public interface IExclusionesService
    {
        Task<List<ExclusionesModel>> ConsultarOrdenesBigQ(List<string> parameterValues, string pais);
        Task<List<ExclusionesModel>> ConsultarOrdenesBigQ(List<string> parameterValues, string pais, SemaphoreSlim semaphore);
        Task<string> DeleteKpiAsync(long id);
        Task<string> DeleteMotivoAsync(long id);
        Task<List<ExclusionesKpiModel>> GetAllKpiAsync();
        Task<List<ExclusionesMotivoModel>> GetAllMotivoAsync(string pais);
        Task<List<ExclusionesSubmotivoModel>> GetAllSubmotivoAsync(string pais);
        Task<List<ExclusionesModel>> GetListAsync(List<string> parameterValues);
        Task<string> InsertKpiAsync(ExclusionesKpiModel _model);
        Task<string> InsertMotivoAsync(ExclusionesMotivoModel _model);
        Task<string> SaveChangesAsync(IList<ExclusionesModel> _exclusionesModel, string pais);
        Task<string> UpdateKpiAsync(ExclusionesKpiModel _model);
        Task<string> UpdateMotivoAsync(ExclusionesMotivoModel _model);
    }
}