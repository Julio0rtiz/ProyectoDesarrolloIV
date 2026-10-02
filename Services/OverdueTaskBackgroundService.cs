using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace TodoApi.Services
{
    public class OverdueTaskBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OverdueTaskBackgroundService> _logger;
        private readonly TimeSpan _period = TimeSpan.FromMinutes(2);

        public OverdueTaskBackgroundService(IServiceProvider serviceProvider, ILogger<OverdueTaskBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Overdue task background service is running.");

            using var timer = new PeriodicTimer(_period);

            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    _logger.LogInformation("Checking for overdue tasks in the background");

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var overdueTaskService = scope.ServiceProvider.GetRequiredService<IOverdueTaskService>();
                        int count = await overdueTaskService.CheckAndNotifyOverdueTaskAsync(null);

                        if (count > 0)
                        {
                            _logger.LogInformation("{Count} overdue task(s) detected in the background process", count);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error ocurred while checking overdue task in the background");
                }
            }
        }
    }
}