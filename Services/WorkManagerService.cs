using AndroidX.Work;
using Java.Util.Concurrent;

namespace kal_sync.Services;

/// <summary>
/// Schedules a daily PeriodicWorkRequest at 23:55 via WorkManager.
/// Idempotent — safe to call on every app start (uses unique work name).
/// </summary>
public class WorkManagerService
{
    public void ScheduleDailyBalanceWorker()
    {
        var now    = DateTime.Now;
        var target = DateTime.Today.AddHours(23).AddMinutes(55);
        if (target <= now)
            target = target.AddDays(1);

        long delayMs = (long)(target - now).TotalMilliseconds;

        var workerClass = Java.Lang.Class.FromType(typeof(DailyBalanceWorker));
        var request = new PeriodicWorkRequest.Builder(
                workerClass, 24L, TimeUnit.Hours!, 10L, TimeUnit.Minutes!)
            .SetInitialDelay(delayMs, TimeUnit.Milliseconds!)
            .Build();

        WorkManager
            .GetInstance(Android.App.Application.Context)
            .EnqueueUniquePeriodicWork(
                "kal_sync_daily_balance",
                ExistingPeriodicWorkPolicy.Keep,
                request);
    }
}
