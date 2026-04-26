using kal_sync.ViewModels;

namespace kal_sync.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    async void OnHomeTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("//HomePage");
}
