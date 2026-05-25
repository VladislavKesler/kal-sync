using System.ComponentModel;
using kal_sync.ViewModels;

namespace kal_sync.Views;

public partial class BodyMeasurementsPage : ContentPage
{
    public BodyMeasurementsPage(BodyMeasurementViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is BodyMeasurementViewModel vm)
        {
            vm.PropertyChanged += OnVmPropertyChanged;
            vm.PageAppearingCommand.Execute(null);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (BindingContext is BodyMeasurementViewModel vm)
            vm.PropertyChanged -= OnVmPropertyChanged;
    }

    private async void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not BodyMeasurementViewModel vm) return;

        if (e.PropertyName == nameof(BodyMeasurementViewModel.Measurements))
            ChartView.Invalidate();

        if (e.PropertyName == nameof(BodyMeasurementViewModel.BalanceChartDrawable))
            BalanceChartView.Invalidate();

        if (e.PropertyName == nameof(BodyMeasurementViewModel.ReminderAlert)
            && vm.ReminderAlert is { } message)
        {
            vm.ReminderAlert = null;
            await DisplayAlert("Erinnerung", message, "OK");
        }
    }

    async void OnHomeTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("//HomePage");

    async void OnSettingsTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("//SettingsPage");
}
