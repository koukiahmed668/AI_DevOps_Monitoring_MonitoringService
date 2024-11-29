using AI_DevOps_Monitoring_MonitoringService.Domain.Models;
using Microsoft.EntityFrameworkCore;


namespace AI_DevOps_Monitoring_MonitoringService.Infrastructure.DbContexts
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Metric> Metrics { get; set; } // Add your domain models here
    }
}
