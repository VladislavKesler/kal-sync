namespace kal_sync.Views;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Load data when page appears
        if (BindingContext is ViewModels.HomeViewModel vm)
        {
            vm.PageAppearingCommand.Execute(null);
        }
    }
}