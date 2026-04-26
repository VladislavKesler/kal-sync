using kal_sync.Converters;
using kal_sync.ViewModels;

namespace kal_sync.Views;

public partial class HomePage : ContentPage
{
    private HomeViewModel? _vm;

    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _vm = viewModel;

        // Wire ring drawable to SurplusPercent changes
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(HomeViewModel.SurplusPercent))
                UpdateRing();
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is HomeViewModel vm)
            vm.PageAppearingCommand.Execute(null);
    }

    /// <summary>Push current SurplusPercent to the ring drawable and trigger redraw.</summary>
    private void UpdateRing()
    {
        if (_vm is null) return;
        RingDrawable.Surplus = _vm.SurplusPercent;
        RingView.Invalidate();
    }

    async void OnSettingsTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("//SettingsPage");
}
