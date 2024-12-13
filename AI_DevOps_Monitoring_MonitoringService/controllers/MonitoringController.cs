using AI_DevOps_Monitoring_MonitoringService.Application.Interfaces;
using AI_DevOps_Monitoring_MonitoringService.Application.Services;
using AI_DevOps_Monitoring_MonitoringService.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AI_DevOps_Monitoring_MonitoringService.controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MonitoringController : ControllerBase
    {
        private readonly IMonitoringService _monitoringService;
        private readonly MetricAlertService _metricAlertService;


        public MonitoringController(IMonitoringService monitoringService, MetricAlertService metricAlertService)
        {
            _monitoringService = monitoringService;
            _metricAlertService = metricAlertService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllMetrics()
        {
            var metrics = await _monitoringService.GetAllMetricsAsync();
            return Ok(metrics);
        }

        [HttpGet("{category}")]
        public async Task<IActionResult> GetMetricsByCategory(string category)
        {
            var metrics = await _monitoringService.GetMetricsByCategoryAsync(category);
            return Ok(metrics);
        }

        [HttpPost]
        public async Task<IActionResult> AddMetric([FromBody] Metric metric)
        {
            await _monitoringService.AddMetricAsync(metric);
            return Ok();
        }

        [HttpPost("stream")]
        public async Task<IActionResult> StreamMetric([FromBody] Metric metric)
        {
            await _monitoringService.AddMetricAsync(metric);
            await _metricAlertService.CheckAndNotifyAsync(metric); // Check thresholds
            return Ok();
        }

      
    }
}
