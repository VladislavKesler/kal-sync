using kal_sync.ViewModels;

namespace kal_sync.Views;

public partial class HomePage : ContentPage
{
    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is HomeViewModel vm)
            vm.PageAppearingCommand.Execute(null);
    }
}
