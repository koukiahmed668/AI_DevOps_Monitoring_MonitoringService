using AI_DevOps_Monitoring_MonitoringService.Application.Interfaces;
using AI_DevOps_Monitoring_MonitoringService.Domain.Models;
using AI_DevOps_Monitoring_MonitoringService.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;

namespace AI_DevOps_Monitoring_MonitoringService.Infrastructure.Repositories
{
    public class MetricRepository : IMetricRepository
    {
        private readonly AppDbContext _context;

        public MetricRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Metric>> GetAllAsync()
        {
            return await _context.Metrics.ToListAsync();
        }

        public async Task<IEnumerable<Metric>> GetByCategoryAsync(string category)
        {
            return await _context.Metrics
                .Where(metric => metric.Category == category)
                .ToListAsync();
        }

        public async Task AddAsync(Metric metric)
        {
            await _context.Metrics.AddAsync(metric);
            await _context.SaveChangesAsync();
        }
    }
}
