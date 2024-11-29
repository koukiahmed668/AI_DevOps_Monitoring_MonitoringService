using AI_DevOps_Monitoring_MonitoringService.Application.Interfaces;
using AI_DevOps_Monitoring_MonitoringService.Domain.Models;

namespace AI_DevOps_Monitoring_MonitoringService.Application.Services
{
    public class MonitoringService : IMonitoringService
    {
        private readonly IMetricRepository _metricRepository;

        public MonitoringService(IMetricRepository metricRepository)
        {
            _metricRepository = metricRepository;
        }

        public async Task<IEnumerable<Metric>> GetAllMetricsAsync()
        {
            return await _metricRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Metric>> GetMetricsByCategoryAsync(string category)
        {
            return await _metricRepository.GetByCategoryAsync(category);
        }

        public async Task AddMetricAsync(Metric metric)
        {
            await _metricRepository.AddAsync(metric);
        }
    }
}
