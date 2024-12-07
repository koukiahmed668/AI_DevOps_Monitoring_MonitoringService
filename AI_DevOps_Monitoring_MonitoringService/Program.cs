using Microsoft.EntityFrameworkCore;
using AI_DevOps_Monitoring_MonitoringService.SignalR.Hubs;
using AI_DevOps_Monitoring_MonitoringService.Application.Interfaces;
using AI_DevOps_Monitoring_MonitoringService.Application.Services;
using AI_DevOps_Monitoring_MonitoringService.Infrastructure.DbContexts;
using AI_DevOps_Monitoring_MonitoringService.Infrastructure.Repositories;
using AI_DevOps_Monitoring_MonitoringService.BackgroundTasks;
using InfluxDB.Client;


namespace AI_DevOps_Monitoring_MonitoringService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            /*builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.ListenAnyIP(8080); // Listen on all interfaces for port 8080
            });*/

            // Configuration for the Monitoring Database
            var monitoringConnectionString = builder.Configuration.GetConnectionString("MonitoringConnection");

            // Add DbContext for Monitoring
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseMySQL(monitoringConnectionString));


            var influxDbUrl = "http://localhost:8086";  // Replace with your InfluxDB URL
            var token = "oE2ChrZaWP9KHj8ytb9nGop1UjkL7H4ubuMuYrZOO2b6B1MgtgE8PNDCR6K1cuf-e0oCvjDkvWDGh554gmYvLw=="; // Replace with your InfluxDB token
            const string org = "kouki";
            const string bucket = "metrics";

            // Register InfluxDB Client
            builder.Services.AddSingleton<IInfluxDBClient>(sp =>
                InfluxDBClientFactory.Create(influxDbUrl, token));

            // Add repository
            builder.Services.AddScoped<IMetricRepository, MetricRepository>();
            builder.Services.AddScoped<IMonitoringService, MonitoringService>();
            builder.Services.AddScoped<MetricAlertService, MetricAlertService>();

            builder.Services.AddHostedService<MetricCollector>();

            // CORS configuration
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAngularApp", policy =>
                {
                    policy
                        .WithOrigins("http://localhost:4200") // Your Angular app's URL
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials(); // Allow credentials for SignalR
                });
            });

            // Add services to the container.
            builder.Services.AddSignalR();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment()/* || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"*/)
            {
                // Enable Swagger for Docker and Development
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            /* Disable HTTPS redirection for Docker environments
            if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
            {
                // Don't redirect HTTP to HTTPS when running inside Docker
                // This avoids issues with port forwarding in Docker
            }
            else
            {
                app.UseHttpsRedirection();
            }*/

            // Apply the CORS policy
            app.UseCors("AllowAngularApp");

            // Configure SignalR
            app.MapHub<MonitoringHub>("/monitoringHub");

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
