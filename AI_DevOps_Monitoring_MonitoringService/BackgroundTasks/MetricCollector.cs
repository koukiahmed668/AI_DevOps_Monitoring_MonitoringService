using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using Microsoft.AspNetCore.SignalR;
using AI_DevOps_Monitoring_MonitoringService.SignalR.Hubs;
using AI_DevOps_Monitoring_MonitoringService.Application.Interfaces;
using System.Diagnostics;
using InfluxDB.Client.Writes;
using Docker.DotNet;
using Docker.DotNet.Models;
using Newtonsoft.Json;

namespace AI_DevOps_Monitoring_MonitoringService.BackgroundTasks
{
    public class MetricCollector : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<MetricCollector> _logger;
        private readonly IHubContext<MonitoringHub> _hubContext;
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly IInfluxDBClient _influxDbClient;
        private readonly DockerClient _dockerClient;

        private readonly string _csvFilePath = "metrics_log.csv";

        public MetricCollector(IServiceScopeFactory serviceScopeFactory, ILogger<MetricCollector> logger, IHubContext<MonitoringHub> hubContext, IInfluxDBClient influxDbClient)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _hubContext = hubContext;
            _influxDbClient = influxDbClient;
            _dockerClient = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var writeApi = _influxDbClient.GetWriteApiAsync();
            string org = "kouki";
            string bucket = "metrics";

            if (!System.IO.File.Exists(_csvFilePath))
            {
                System.IO.File.AppendAllText(_csvFilePath, "Timestamp, Metric Name, Metric Category, Metric Value\n");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var metricRepository = scope.ServiceProvider.GetRequiredService<IMetricRepository>();

                    var cpuUsage = GetCpuUsage();
                    var memoryUsage = GC.GetTotalMemory(false) / (1024 * 1024);
                    var diskUsage = GetDiskUsage();

                    var timestamp = DateTime.UtcNow;

                    var systemMetrics = new[]
                    {
                new { Name = "CPU Usage", Value = cpuUsage.ToString("F2"), Category = "CPU" },
                new { Name = "Memory Usage", Value = memoryUsage.ToString("F2"), Category = "Memory" },
                new { Name = "Disk Usage", Value = diskUsage.ToString("F2"), Category = "Disk" }
            };

                    var containerMetrics = await GetContainerMetricsAsync(stoppingToken);

                    foreach (var metric in systemMetrics)
                    {
                        var point = PointData
                            .Measurement("system_metrics")
                            .Tag("host", "host1")
                            .Field(metric.Category.ToLower() + "_usage", double.Parse(metric.Value))
                            .Timestamp(timestamp, WritePrecision.Ms);

                        await writeApi.WritePointAsync(point, bucket, org);
                    }

                    foreach (var containerMetric in containerMetrics)
                    {
                        var point = PointData
                            .Measurement("container_metrics")
                            .Tag("container_name", containerMetric.ContainerName)
                            .Field("cpu_usage", containerMetric.CpuUsage)
                            .Field("memory_usage", containerMetric.MemoryUsage)
                            .Field("memory_percentage", containerMetric.MemoryPercentage)
                            .Timestamp(timestamp, WritePrecision.Ms);

                        await writeApi.WritePointAsync(point, bucket, org);

   

                    }

                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }


        private double GetDiskUsage()
        {
            try
            {
                var driveInfo = new DriveInfo("/");
                var totalSpace = driveInfo.TotalSize;
                var freeSpace = driveInfo.AvailableFreeSpace;
                var usedSpace = totalSpace - freeSpace;
                return (double)usedSpace / totalSpace * 100;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting disk usage: {Message}", ex.Message);
                return 0;
            }
        }

        private async Task<IEnumerable<dynamic>> GetContainerMetricsAsync(CancellationToken cancellationToken)
        {
            var containerStats = new List<dynamic>();
            var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = false });

            foreach (var container in containers)
            {
                using (var statsResponse = await _dockerClient.Containers.GetContainerStatsAsync(
                    container.ID,
                    new ContainerStatsParameters { Stream = false },
                    cancellationToken))
                {
                    using (var streamReader = new StreamReader(statsResponse))
                    {
                        var statsJson = await streamReader.ReadToEndAsync();
                        var stats = JsonConvert.DeserializeObject<dynamic>(statsJson);

                        var cpuDelta = (double)(stats.cpu_stats.cpu_usage.total_usage - stats.precpu_stats.cpu_usage.total_usage);
                        var systemCpuDelta = (double)(stats.cpu_stats.system_cpu_usage - stats.precpu_stats.system_cpu_usage);
                        var cpuUsage = (cpuDelta / systemCpuDelta) * 100.0;

                        var memoryUsage = (double)stats.memory_stats.usage / (1024 * 1024);
                        var memoryLimit = (double)stats.memory_stats.limit / (1024 * 1024);
                        var memoryPercentage = (memoryUsage / memoryLimit) * 100;

                        containerStats.Add(new
                        {
                            ContainerName = container.Names[0].Trim('/'),
                            CpuUsage = cpuUsage,
                            MemoryUsage = memoryUsage,
                            MemoryPercentage = memoryPercentage
                        });
                    }
                }
            }

            return containerStats;
        }

       


        public static double GetCpuUsage()
        {
            var process = Process.GetCurrentProcess();
            var totalCpuBefore = process.TotalProcessorTime.TotalMilliseconds;
            var sw = Stopwatch.StartNew();
            Thread.Sleep(1000);
            sw.Stop();
            var totalCpuAfter = process.TotalProcessorTime.TotalMilliseconds;
            var cpuUsage = (totalCpuAfter - totalCpuBefore) / (Environment.ProcessorCount * sw.ElapsedMilliseconds);
            return cpuUsage * 100;
        }

        public override void Dispose()
        {
            _logger.LogInformation("Disposing MetricCollector resources...");
            _dockerClient?.Dispose();
            _httpClient?.Dispose();
            base.Dispose();
        }
    }
}
