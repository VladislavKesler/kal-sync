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

        // Wire ring drawable to DeficitPercent changes
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(HomeViewModel.DeficitPercent))
                UpdateRing();
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is HomeViewModel vm)
            vm.PageAppearingCommand.Execute(null);
    }

    /// <summary>Push current DeficitPercent to the ring drawable and trigger redraw.</summary>
    private void UpdateRing()
    {
        if (_vm is null) return;
        RingDrawable.DeficitPercent = _vm.DeficitPercent;
        RingView.Invalidate();
    }

    async void OnMeasurementsTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("//BodyMeasurementsPage");

    async void OnSettingsTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("//SettingsPage");
}
