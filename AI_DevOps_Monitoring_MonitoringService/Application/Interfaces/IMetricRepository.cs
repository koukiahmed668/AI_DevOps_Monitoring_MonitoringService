using AI_DevOps_Monitoring_MonitoringService.Domain.Models;

namespace AI_DevOps_Monitoring_MonitoringService.Application.Interfaces
{
    public interface IMetricRepository
    {
        Task<IEnumerable<Metric>> GetAllAsync();
        Task<IEnumerable<Metric>> GetByCategoryAsync(string category);
        Task AddAsync(Metric metric);
    }
}
