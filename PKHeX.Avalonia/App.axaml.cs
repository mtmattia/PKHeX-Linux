using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PKHeX.Avalonia.ViewModels;
using PKHeX.Avalonia.Views;

namespace PKHeX.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = new MainViewModel();

            // Dev convenience: auto-open a save if PKHEX_AUTOLOAD points to one.
            var autoload = System.Environment.GetEnvironmentVariable("PKHEX_AUTOLOAD");
            if (!string.IsNullOrEmpty(autoload) && System.IO.File.Exists(autoload))
            {
                vm.LoadSaveFromPath(autoload);
                // Dev convenience: preselect the first occupied slot so the editor shows.
                foreach (var s in vm.CurrentBox)
                {
                    if (!s.IsEmpty) { vm.SelectedSlot = s; break; }
                }
            }

            desktop.MainWindow = new MainWindow { DataContext = vm };
        }

        base.OnFrameworkInitializationCompleted();
    }
}