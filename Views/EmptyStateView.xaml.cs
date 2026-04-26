namespace kal_sync.Views;

public partial class EmptyStateView : ContentView
{
    public EmptyStateView()
    {
        InitializeComponent();
    }

    async void OnGoToProfileClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//SettingsPage");
}
