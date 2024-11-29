using AI_DevOps_Monitoring_MonitoringService.Domain.Models;

namespace AI_DevOps_Monitoring_MonitoringService.Application.Interfaces
{
    public interface IMonitoringService
    {
        Task<IEnumerable<Metric>> GetAllMetricsAsync();
        Task<IEnumerable<Metric>> GetMetricsByCategoryAsync(string category);
        Task AddMetricAsync(Metric metric);
    }
}
