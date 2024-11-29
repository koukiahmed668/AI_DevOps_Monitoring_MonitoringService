using AI_DevOps_Monitoring_MonitoringService.Application.Interfaces;
using AI_DevOps_Monitoring_MonitoringService.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.IO;

namespace AI_DevOps_Monitoring_MonitoringService.BackgroundTasks
{
    public class MetricCollector : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<MetricCollector> _logger;
        private readonly IHubContext<MonitoringHub> _hubContext;
        private readonly string _csvFilePath = "metrics_log.csv";

        private string? _currentContainerId; // For storing the container ID

        // Variables to accumulate metrics and calculate averages
        private double _cpuUsageSum = 0;
        private double _memoryUsageSum = 0;
        private double _diskUsageSum = 0;
        private int _metricCount = 0;

        public MetricCollector(IServiceScopeFactory serviceScopeFactory, ILogger<MetricCollector> logger, IHubContext<MonitoringHub> hubContext)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _hubContext = hubContext;
        }

        public void StartMonitoringContainer(string containerId)
        {
            _currentContainerId = containerId;
            _logger.LogInformation("Monitoring started for container: {ContainerId}", containerId);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!File.Exists(_csvFilePath))
            {
                File.AppendAllText(_csvFilePath, "Timestamp, CPU Usage (%), Memory Usage (MB), Disk Usage (%)\n");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                if (string.IsNullOrEmpty(_currentContainerId))
                {
                    _logger.LogWarning("No container ID specified. Waiting for input...");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                try
                {
                    // Replace Docker Stats API call with command-line Docker Stats
                    var stats = GetContainerStatsFromCommandLine(_currentContainerId);

                    if (stats == null)
                    {
                        _logger.LogError("Failed to get container stats for {ContainerId}", _currentContainerId);
                        continue;
                    }

                    // Access tuple values correctly by index
                    var cpuUsage = stats.Value.Item1; // CpuUsage
                    var memoryUsage = stats.Value.Item2; // MemoryUsage
                    var diskUsage = stats.Value.Item3; // DiskUsage

                    var timestamp = DateTime.UtcNow;

                    var metrics = new[]
                    {
                        new { Name = "CPU Usage", Value = cpuUsage.ToString("F2"), Category = "CPU" },
                        new { Name = "Memory Usage", Value = memoryUsage.ToString("F2"), Category = "Memory" },
                        new { Name = "Disk Usage", Value = diskUsage.ToString("F2"), Category = "Disk" }
                    };

                    // Accumulate metrics for averaging
                    _cpuUsageSum += cpuUsage;
                    _memoryUsageSum += memoryUsage;
                    _diskUsageSum += diskUsage;
                    _metricCount++;

                    // Broadcast metrics via SignalR
                    foreach (var metric in metrics)
                    {
                        await _hubContext.Clients.All.SendAsync("ReceiveMetricUpdate", metric.Name, metric.Value);
                    }

                    // Log metrics and calculate averages every 50 metrics
                    if (_metricCount >= 50)
                    {
                        var avgCpuUsage = _cpuUsageSum / _metricCount;
                        var avgMemoryUsage = _memoryUsageSum / _metricCount;
                        var avgDiskUsage = _diskUsageSum / _metricCount;

                        var aggregatedMetrics = new
                        {
                            Timestamp = timestamp,
                            CpuAverage = avgCpuUsage.ToString("F2"),
                            MemoryAverage = avgMemoryUsage.ToString("F2"),
                            DiskAverage = avgDiskUsage.ToString("F2")
                        };

                        using var scope = _serviceScopeFactory.CreateScope();
                        var metricRepository = scope.ServiceProvider.GetRequiredService<IMetricRepository>();
                        await metricRepository.AddAsync(new Domain.Models.Metric
                        {
                            Name = "Aggregated Metrics",
                            Value = $"{avgCpuUsage:F2}, {avgMemoryUsage:F2}, {avgDiskUsage:F2}",
                            Timestamp = timestamp,
                            Category = "System"
                        });

                        var csvLine = $"{timestamp},{avgCpuUsage:F2},{avgMemoryUsage:F2},{avgDiskUsage:F2}";
                        File.AppendAllText(_csvFilePath, csvLine + Environment.NewLine);

                        _logger.LogInformation("Aggregated Metrics collected: CPU={CPU}, Memory={Memory}, Disk={Disk}",
                            avgCpuUsage, avgMemoryUsage, avgDiskUsage);

                        _cpuUsageSum = 0;
                        _memoryUsageSum = 0;
                        _diskUsageSum = 0;
                        _metricCount = 0;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error monitoring container: {ContainerId}", _currentContainerId);
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        // Use the command line to get Docker stats
        private (double CpuUsage, double MemoryUsage, double DiskUsage)? GetContainerStatsFromCommandLine(string containerId)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"stats {containerId} --no-stream --format \"{{{{.CPUPerc}}}} {{.MemUsage}} {{.BlockIOLimit}}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using var process = Process.Start(startInfo);
                using var reader = process?.StandardOutput;

                if (reader != null)
                {
                    var output = reader.ReadLine();
                    if (string.IsNullOrEmpty(output))
                    {
                        return null;
                    }

                    var stats = output.Split(' ');

                    var cpuUsage = double.Parse(stats[0].Replace("%", ""));
                    var memoryUsageParts = stats[1].Split('/');
                    var memoryUsage = double.Parse(memoryUsageParts[0].Replace("MiB", "").Trim());
                    var diskUsage = double.Parse(stats[2].Replace("B", "").Trim());

                    return (cpuUsage, memoryUsage, diskUsage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching container stats from command line.");
            }

            return null;
        }
    }
}
