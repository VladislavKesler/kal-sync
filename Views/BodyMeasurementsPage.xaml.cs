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
            vm.PageAppearingCommand.Execute(null);
    }

    async void OnHomeTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("//HomePage");

    async void OnSettingsTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("//SettingsPage");
}
