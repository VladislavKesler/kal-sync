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
                fonts.AddFont("OpenSans-Regular.ttf",        "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf",       "OpenSansSemibold");
                // Handoff fonts — drop TTF files into Resources/Fonts/ to activate
                fonts.AddFont("Inter_18pt-Regular.ttf",      "Inter");
                fonts.AddFont("Inter_18pt-Bold.ttf",         "InterBold");
                fonts.AddFont("JetBrainsMono-Regular.ttf",   "JetBrainsMono");
            });

        // ── Services ────────────────────────────────────────────────────────
        builder.Services.AddSingleton<GarminApiService>();
        builder.Services.AddSingleton<UserProfileService>();
        builder.Services.AddSingleton<GaintainingService>();
        builder.Services.AddSingleton<DatabaseService>();

        // ── ViewModels ──────────────────────────────────────────────────────
        builder.Services.AddSingleton<HomeViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();
        builder.Services.AddSingleton<BodyMeasurementViewModel>();

        // ── Views ───────────────────────────────────────────────────────────
        builder.Services.AddSingleton<HomePage>();
        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddSingleton<BodyMeasurementsPage>();

        return builder.Build();
    }
}
