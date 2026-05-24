using Android.App;
using Android.Content;

namespace kal_sync;

[BroadcastReceiver(Enabled = true, Exported = false)]
public class EveningCheckReceiver : BroadcastReceiver
{
    private const string ChannelId   = "kal_sync_evening";
    private const string ChannelName = "Tagescheck";
    private const int    NotifId     = 1001;

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent is null) return;

        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.Default);
            var nm      = (NotificationManager?)context.GetSystemService(Context.NotificationService);
            nm?.CreateNotificationChannel(channel);
        }

        var notification = new Notification.Builder(context, ChannelId)
            .SetSmallIcon(Android.Resource.Drawable.IcDialogEmail)
            .SetContentTitle(intent.GetStringExtra("title"))
            .SetContentText(intent.GetStringExtra("body"))
            .SetAutoCancel(true)
            .Build()!;

        var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        manager?.Notify(NotifId, notification);
    }
}
