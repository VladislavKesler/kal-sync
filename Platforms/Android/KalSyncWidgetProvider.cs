using System.Globalization;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;

namespace kal_sync;

public class KalSyncWidgetProvider : AppWidgetProvider
{
    private const string SharedPrefsName = "kal_sync_widget";
    private const string TargetKcalKey   = "widget.target_kcal";
    private const string SurplusKey      = "widget.surplus_percent";

    public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
    {
        if (context is null || appWidgetManager is null || appWidgetIds is null) return;

        var prefs     = context.GetSharedPreferences(SharedPrefsName, FileCreationMode.Private);
        float kcal    = prefs?.GetFloat(TargetKcalKey, 0f) ?? 0f;
        float surplus = prefs?.GetFloat(SurplusKey, 0f) ?? 0f;
        bool hasData  = kcal > 0f;

        string kcalText   = hasData ? ((int)kcal).ToString(CultureInfo.InvariantCulture) : "–";
        string statusText = hasData
            ? surplus > 0f ? $"+{surplus:F1}% Überschuss"
            : surplus < 0f ? $"{surplus:F1}% Defizit"
            : "Erhalt"
            : string.Empty;

        // ARGB color constants — green for surplus/neutral, red for deficit
        int statusColor = surplus >= 0f
            ? unchecked((int)0xFF3F6E1F)
            : unchecked((int)0xFFC0533A);

        foreach (int widgetId in appWidgetIds)
        {
            var views = new RemoteViews(context.PackageName!, Resource.Layout.kal_sync_widget);

            views.SetTextViewText(Resource.Id.widget_calories, kcalText);
            views.SetTextViewText(Resource.Id.widget_status, statusText);
            views.SetInt(Resource.Id.widget_status, "setTextColor", statusColor);

            // Tap opens the app — Immutable flag requires API 23+
            var launchIntent = new Intent(context, typeof(MainActivity));
            var flags = Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M
                ? PendingIntentFlags.Immutable
                : PendingIntentFlags.UpdateCurrent;
            var pendingIntent = PendingIntent.GetActivity(context, 0, launchIntent, flags);
            views.SetOnClickPendingIntent(Resource.Id.widget_root, pendingIntent);

            appWidgetManager.UpdateAppWidget(widgetId, views);
        }
    }
}
