using Microsoft.EntityFrameworkCore;
using Hearthly.Data;
using Hearthly.Data.RuleOfThree;

public class RuleOfThreeCompletionService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public RuleOfThreeCompletionService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;

            // run daily at 00:05
            var nextRun = now.Date.AddDays(1).AddMinutes(5);
            var delay = nextRun - now;

            await Task.Delay(delay, stoppingToken);

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var yesterday = DateTime.Today.AddDays(-1);

            var entries = await db.RuleOfThreeEntries
                .Include(e => e.Tasks)
                .Where(e => e.Date == yesterday)
                .ToListAsync();

            foreach (var entry in entries)
            {
                // only mark incomplete if not already complete and not all tasks done
                if (!entry.IsComplete && (entry.Tasks.Count != 6 || entry.Tasks.Any(t => !t.IsDone)))
                {
                    entry.IsComplete = false; // explicit marking
                }
            }

            await db.SaveChangesAsync();
        }
    }
}
