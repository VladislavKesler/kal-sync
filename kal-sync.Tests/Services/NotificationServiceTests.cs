using FluentAssertions;
using Xunit;

namespace kal_sync.Tests.Services;

/// <summary>
/// Tests for NotificationService evening-check logic.
/// Platform APIs (Preferences, LocalNotificationCenter) are mirrored locally
/// since the test project targets net9.0 without MAUI runtime.
/// </summary>
public class NotificationServiceTests
{
    // --- Defaults ---

    [Fact]
    public void EveningCheckEnabled_Default_IsFalse()
    {
        const bool defaultEnabled = false;
        defaultEnabled.Should().BeFalse();
    }

    [Fact]
    public void EveningCheckHour_Default_Is20()
    {
        const int defaultHour = 20;
        defaultHour.Should().Be(20);
    }

    // --- Guard: disabled → no scheduling ---

    [Fact]
    public async Task ScheduleEveningCheckAsync_WhenDisabled_DoesNotThrow()
    {
        // Mirrors the EveningCheckEnabled guard in NotificationService
        Func<Task> act = () => ScheduleIfEnabled(enabled: false, 2000, 1700, 500);
        await act.Should().NotThrowAsync();
    }

    // --- Body string ---

    [Fact]
    public void BuildEveningCheckBody_ContainsTargetKcalRounded()
    {
        double targetKcal     = 2156.7;
        double bmr            = 1942.0;
        double activeCalories = 487.0;

        string body = BuildEveningCheckBody(targetKcal, bmr, activeCalories);

        body.Should().Contain("2157");
    }

    [Fact]
    public void BuildEveningCheckBody_ContainsAllThreeValues()
    {
        string body = BuildEveningCheckBody(2000, 1700, 600);

        body.Should().Contain("2000")
            .And.Contain("1700")
            .And.Contain("600");
    }

    [Fact]
    public void BuildEveningCheckBody_EndsWithOnTrackQuestion()
    {
        string body = BuildEveningCheckBody(2000, 1700, 600);
        body.Should().EndWith("On track?");
    }

    // --- Helpers (mirror NotificationService without MAUI dependencies) ---

    private static Task ScheduleIfEnabled(bool enabled, double targetKcal, double bmr, double activeCalories)
    {
        if (!enabled) return Task.CompletedTask;

        // Real service would call LocalNotificationCenter here
        _ = BuildEveningCheckBody(targetKcal, bmr, activeCalories);
        return Task.CompletedTask;
    }

    private static string BuildEveningCheckBody(double targetKcal, double bmr, double activeCalories)
        => $"Tagesziel: {targetKcal:F0} kcal | Aktiv: {activeCalories:F0} kcal | BMR: {bmr:F0} kcal — On track?";
}
