using Microsoft.AspNetCore.SignalR;
using MySqlX.XDevAPI;

namespace AI_DevOps_Monitoring_MonitoringService.SignalR.Hubs
{
    public class MonitoringHub : Hub
    {
        public async Task SendMetricUpdate(string metricName, string value)
        {
            await Clients.All.SendAsync("ReceiveMetricUpdate", metricName, value);
        }
    }
}
    