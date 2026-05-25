using Android.Content;
using AndroidX.Work;
using kal_sync.Models;
using kal_sync.Services;

namespace kal_sync;

public class DailyBalanceWorker : Worker
{
    public DailyBalanceWorker(Context context, WorkerParameters parameters)
        : base(context, parameters) { }

    public override Result DoWork()
    {
        var prefs = ApplicationContext!
            .GetSharedPreferences("kal_sync_worker", FileCreationMode.Private)!;

        float balanceKcal = prefs.GetFloat("worker.last_balance_kcal", float.MinValue);
        if (balanceKcal == float.MinValue) return Result.InvokeFailure();

        float tdee           = prefs.GetFloat("worker.last_tdee", 0f);
        float bmr            = prefs.GetFloat("worker.last_bmr", 0f);
        float activeCalories = prefs.GetFloat("worker.last_active_calories", 0f);
        float targetKcal     = prefs.GetFloat("worker.last_target_kcal", 0f);

        var db    = new DatabaseService();
        var entry = new DailyBalance
        {
            Date           = DateTime.Today,
            BalanceKcal    = balanceKcal,
            Tdee           = tdee,
            Bmr            = bmr,
            ActiveCalories = activeCalories,
            TargetKcal     = targetKcal,
        };

        db.UpsertDailyBalanceSync(entry);
        return Result.InvokeSuccess();
    }
}
