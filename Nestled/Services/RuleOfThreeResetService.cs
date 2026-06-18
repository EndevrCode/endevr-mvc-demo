using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nestled.Data;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class RuleOfThreeResetService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<RuleOfThreeResetService> _logger;

    public RuleOfThreeResetService(IServiceProvider services, ILogger<RuleOfThreeResetService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextRun = now.Date.AddDays(1).AddMinutes(1); // 00:01 AM
            var delay = nextRun - now;

            await Task.Delay(delay, stoppingToken);

            using (var scope = _services.CreateScope())
            {
                try
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var yesterday = DateTime.Today.AddDays(-1);
                    var unfinished = db.RuleOfThreeEntries
                        .Where(e => e.Date == yesterday && !e.IsComplete);

                    foreach (var entry in unfinished)
                    {
                        entry.IsComplete = false; // this is just to ensure it's saved, can be omitted
                    }

                    await db.SaveChangesAsync();
                    _logger.LogInformation($"RuleOfThree reset ran at {DateTime.Now}. Marked {unfinished.Count()} entries as incomplete.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during RuleOfThreeResetService execution.");
                }
            }
        }
    }
}
