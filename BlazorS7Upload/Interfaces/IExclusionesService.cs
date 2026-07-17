using BlazorS7Upload.Models;
using Google.Cloud.BigQuery.V2;
using System.Data;

namespace BlazorS7Upload.Interfaces
{
    public interface IExclusionesService
    {
        Task<List<ExclusionesModel>> ConsultarOrdenesBigQ(List<string> parameterValues, string pais);
        Task<List<ExclusionesModel>> ConsultarOrdenesBigQ(List<string> parameterValues, string pais, SemaphoreSlim semaphore);
        Task<string> DeleteKpiAsync(int id);
        Task<string> DeleteMotivoAsync(int id);
        Task<List<ExclusionesKpiModel>> GetAllKpiAsync();
        Task<List<ExclusionesMotivoModel>> GetAllMotivoAsync();
        Task<List<ExclusionesModel>> GetListAsync(List<string> parameterValues);
        Task<string> InsertKpiAsync(ExclusionesKpiModel _model);
        Task<string> InsertMotivoAsync(ExclusionesMotivoModel _model);
        Task<string> SaveChangesAsync(IList<ExclusionesModel> _exclusionesModel, string pais);
        Task<string> UpdateKpiAsync(ExclusionesKpiModel _model);
        Task<string> UpdateMotivoAsync(ExclusionesMotivoModel _model);
    }
}