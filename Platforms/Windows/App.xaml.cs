using Microsoft.UI.Xaml;
using Velopack;

namespace kal_sync.WinUI
{
    public partial class App : MauiWinUIApplication
    {
        public App()
        {
            // Must run before any other code to handle install/uninstall/update hooks.
            VelopackApp.Build().Run();
            this.InitializeComponent();
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
