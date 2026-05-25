using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Extensions.DependencyInjection;
using kal_sync.Services;

namespace kal_sync
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            IPlatformApplication.Current?.Services
                .GetService<WorkManagerService>()
                ?.ScheduleDailyBalanceWorker();
        }
    }
}
