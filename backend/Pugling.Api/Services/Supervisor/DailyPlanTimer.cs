using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;

namespace Pugling.Api.Services.Supervisor;

/// <summary>
/// The backend's one timer (B-734): every quarter of an hour it makes today's plan for each child whose planner is on, so
/// the plan stands from 6 a.m. even before the child opens the app. It calls the same <see cref="DailyPlanner.EnsureTodayAsync"/>
/// as the first request - one code path - and is switched off with <c>Planner:BackgroundEnabled=false</c> (the test hosts),
/// like <c>RateLimiting:LoginEnabled</c>.
/// </summary>
public sealed class DailyPlanTimer(IServiceScopeFactory scopes, TimeProvider time, ILogger<DailyPlanTimer> logger)
    : BackgroundService
{
    /// <summary>How often the timer looks; a plan is due once a day, so a quarter of an hour late is still before school.</summary>
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, time);
        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>
    /// One pass over the children whose planner is on, each in its own scope: a child whose plan fails is logged and skipped,
    /// so it never keeps the timer from the children after it.
    /// </summary>
    public async Task RunOnceAsync(CancellationToken ct)
    {
        List<int> children;
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            children = await scope.ServiceProvider.GetRequiredService<PuglingDbContext>().Children.AsNoTracking()
                .Where(c => c.PlannerMode != PlannerMode.Off).Select(c => c.Id).ToListAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A failed pass must not stop the host (a background exception does by default); the next tick tries again.
            logger.LogError(ex, "Tagesplaner-Durchlauf fehlgeschlagen; der nächste Durchlauf versucht es erneut.");
            return;
        }
        foreach (var childId in children)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<DailyPlanner>().EnsureTodayAsync(childId, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Tagesplan für Kind {ChildId} fehlgeschlagen; der nächste Durchlauf oder Zugriff versucht es erneut.",
                    childId);
            }
        }
    }
}
