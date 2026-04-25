using kal_sync.Views;
using kal_sync.ViewModels;
using kal_sync.Services;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace kal_sync;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ── Services ────────────────────────────────────────────────────────
        builder.Services.AddSingleton<GarminApiService>();
        builder.Services.AddSingleton<UserProfileService>();
        builder.Services.AddSingleton<GaintainingService>();

        // ── ViewModels ──────────────────────────────────────────────────────
        builder.Services.AddSingleton<HomeViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();

        // ── Views ───────────────────────────────────────────────────────────
        builder.Services.AddSingleton<HomePage>();
        builder.Services.AddSingleton<SettingsPage>();

        return builder.Build();
    }
}
