using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace apiEndpointNameSpace.Services
{
    public class ThreadPoolMonitorService : BackgroundService
    {
        private readonly ILogger<ThreadPoolMonitorService> _logger;
        private readonly TimeSpan _monitoringInterval = TimeSpan.FromMinutes(5);

        public ThreadPoolMonitorService(ILogger<ThreadPoolMonitorService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Thread pool monitoring service started");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    ThreadPool.GetAvailableThreads(out int workerThreads, out int completionPortThreads);
                    ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompletionPortThreads);
                    
                    _logger.LogInformation(
                        "Thread pool stats - Available: {WorkerThreads}/{MaxWorkerThreads} worker, " +
                        "{CompletionPortThreads}/{MaxCompletionPortThreads} I/O completion",
                        workerThreads, maxWorkerThreads,
                        completionPortThreads, maxCompletionPortThreads);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while monitoring thread pool");
                }

                try
                {
                    // Check again after the specified interval
                    await Task.Delay(_monitoringInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // This exception is expected when the stoppingToken is triggered
                    // Just break out of the loop
                    break;
                }
            }
            
            _logger.LogInformation("Thread pool monitoring service stopped");
        }
    }
}