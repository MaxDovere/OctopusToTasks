using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using OctopusToTasks.Services;

namespace OctopusToTasks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            MonitorLoop monitorLoop = host.Services.GetRequiredService<MonitorLoop>()!;
            monitorLoop.StartMonitorLoop();
            
            host.Run();
        }
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(options => 
                    options.AddFilter<EventLogLoggerProvider>(level => level >= LogLevel.Information))
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<MonitorLoop>();
                    services.AddSingleton<IBackgroundTaskQueue>(_ =>
                    {
                        if (!int.TryParse(hostContext.Configuration["QueueCapacity"], out var queueCapacity))
                        {
                            queueCapacity = 100;
                        }

                        return new DefaultBackgroundTaskQueue(queueCapacity);
                    });
                    services.AddHostedService<QueuedHostedService>()
                        .Configure<EventLogSettings>(config =>
                        {
                            config.LogName = "OctopusToTasks Service";
                            config.SourceName = "OctopusToTasks Service Source";
                        });
                })
                .UseWindowsService(config =>
                    config.ServiceName = "OctopusToTasks Service 1.0"
                );
        }
}
