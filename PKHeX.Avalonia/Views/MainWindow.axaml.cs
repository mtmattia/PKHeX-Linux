using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PKHeX.Avalonia.ViewModels;

namespace PKHeX.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnOpenClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Apri un salvataggio Pokémon",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Salvataggi Pokémon")
                {
                    Patterns = new[] { "main", "*.sav", "*.dsv", "*.dat", "*.gci", "*.bin", "*.pk*" },
                },
                FilePickerFileTypes.All,
            },
        });

        if (files.Count == 0)
            return;

        var path = files[0].TryGetLocalPath();
        if (path is not null)
            vm.LoadSaveFromPath(path);
    }
}
