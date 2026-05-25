using Android.Content;

namespace kal_sync.Services;

/// <summary>Writes balance data into SharedPreferences so DailyBalanceWorker can read them.</summary>
public class WorkerDataService
{
    private const string PrefsName = "kal_sync_worker";

    public void WriteBalanceData(
        double balanceKcal,
        double tdee,
        double bmr,
        double activeCalories,
        double targetKcal)
    {
        var prefs = Android.App.Application.Context
            .GetSharedPreferences(PrefsName, FileCreationMode.Private)!;
        var editor = prefs.Edit()!;
        editor.PutFloat("worker.last_balance_kcal",    (float)balanceKcal);
        editor.PutFloat("worker.last_tdee",            (float)tdee);
        editor.PutFloat("worker.last_bmr",             (float)bmr);
        editor.PutFloat("worker.last_active_calories", (float)activeCalories);
        editor.PutFloat("worker.last_target_kcal",     (float)targetKcal);
        editor.Apply();
    }
}
