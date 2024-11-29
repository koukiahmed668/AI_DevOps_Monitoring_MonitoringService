using AI_DevOps_Monitoring_MonitoringService.Domain.Models;
using AI_DevOps_Monitoring_MonitoringService.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace AI_DevOps_Monitoring_MonitoringService.Application.Services
{
    public class MetricAlertService
    {
        private readonly IHubContext<MonitoringHub> _hubContext;

        public MetricAlertService(IHubContext<MonitoringHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task CheckAndNotifyAsync(Metric metric)
        {
            // Example: CPU usage > 80%
            if (metric.Category == "CPU" && double.TryParse(metric.Value, out double usage) && usage > 80)
            {
                await _hubContext.Clients.All.SendAsync("Alert", $"High CPU Usage Detected: {usage}%");
            }
        }
    }
}
