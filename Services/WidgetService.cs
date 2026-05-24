#if ANDROID
using Android.App;
using Android.Appwidget;
using Android.Content;
#endif

namespace kal_sync.Services;

/// <summary>
/// Writes calorie data into Android SharedPreferences and triggers a widget refresh.
/// On non-Android platforms all methods are no-ops.
/// </summary>
public class WidgetService
{
    private const string SharedPrefsName = "kal_sync_widget";
    private const string TargetKcalKey   = "widget.target_kcal";
    private const string SurplusKey      = "widget.surplus_percent";

#if ANDROID
    public void UpdateData(double targetKcal, double surplusPercent)
    {
        var context = Application.Context;
        context
            .GetSharedPreferences(SharedPrefsName, FileCreationMode.Private)!
            .Edit()!
            .PutFloat(TargetKcalKey, (float)targetKcal)!
            .PutFloat(SurplusKey, (float)surplusPercent)!
            .Apply();

        var manager  = AppWidgetManager.GetInstance(context)!;
        var provider = new ComponentName(context, Java.Lang.Class.FromType(typeof(KalSyncWidgetProvider)));
        int[] ids    = manager.GetAppWidgetIds(provider) ?? [];
        if (ids.Length == 0) return;

        var intent = new Intent(context, typeof(KalSyncWidgetProvider));
        intent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
        intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
        context.SendBroadcast(intent);
    }

    public void RequestPinWidget()
    {
        if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.O) return;

        var context  = Application.Context;
        var manager  = AppWidgetManager.GetInstance(context)!;
        var provider = new ComponentName(context, Java.Lang.Class.FromType(typeof(KalSyncWidgetProvider)));

        if (manager.IsRequestPinAppWidgetSupported)
            manager.RequestPinAppWidget(provider, null, null);
    }
#else
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822", Justification = "Instance method for DI / testability")]
    public void UpdateData(double targetKcal, double surplusPercent) { }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822", Justification = "Instance method for DI / testability")]
    public void RequestPinWidget() { }
#endif
}
