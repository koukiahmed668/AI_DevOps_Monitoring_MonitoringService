namespace AI_DevOps_Monitoring_MonitoringService.Domain.Models
{
    public class Metric
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public DateTime Timestamp { get; set; }
        public string Category { get; set; } // e.g., CPU, Memory, Application Metrics
    }
}
