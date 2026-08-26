using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
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

    private async void OnSaveAsClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || !vm.HasSave)
            return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Salva il salvataggio con un nome",
            DefaultExtension = "sav",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Salvataggio Pokémon") { Patterns = new[] { "*.sav" } },
                FilePickerFileTypes.All,
            },
        });

        var path = file?.TryGetLocalPath();
        if (path is not null)
            vm.SaveAs(path);
    }

    private async void OnExportPokemonClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || !vm.HasSelectedPokemon)
            return;
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Esporta Pokémon (.pk3)",
            SuggestedFileName = vm.SuggestedPkmFileName,
            DefaultExtension = "pk3",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Pokémon Gen3") { Patterns = new[] { "*.pk3" } },
                FilePickerFileTypes.All,
            },
        });
        var path = file?.TryGetLocalPath();
        if (path is not null)
            vm.ExportSelectedPokemon(path);
    }

    private async void OnImportPokemonClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || vm.SelectedSlot is null)
            return;
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Importa Pokémon (.pk3)",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Pokémon Gen3") { Patterns = new[] { "*.pk3", "*.pk*" } },
                FilePickerFileTypes.All,
            },
        });
        var path = files.Count > 0 ? files[0].TryGetLocalPath() : null;
        if (path is not null)
            vm.ImportSelectedPokemon(path);
    }

    private async void OnExportBoxClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || !vm.IsBoxSelected)
            return;
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Scegli la cartella dove esportare il Box (.pk3)",
            AllowMultiple = false,
        });
        var dir = folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
        if (dir is not null)
            vm.ExportBoxToFolder(dir);
    }

    private async void OnImportBoxClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || !vm.IsBoxSelected)
            return;
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Scegli la cartella con i .pk3 da importare nel Box",
            AllowMultiple = false,
        });
        var dir = folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
        if (dir is not null)
            vm.ImportBoxFromFolder(dir);
    }

    private async void OnRemoveClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        var confirmed = await ConfirmAsync(
            "Rimuovi Pokémon",
            "Vuoi davvero eliminarlo?\nLo slot verrà svuotato (definitivo dopo il salvataggio).");
        if (confirmed)
            vm.RemoveSelectedPokemon();
    }

    /// <summary>Minimal yes/no modal dialog; returns true if the user confirmed.</summary>
    private Task<bool> ConfirmAsync(string title, string message)
    {
        var tcs = new TaskCompletionSource<bool>();

        var yes = new Button { Content = "Sì, elimina", MinWidth = 110 };
        var no = new Button { Content = "Annulla", MinWidth = 110, IsDefault = true };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        buttons.Children.Add(no);
        buttons.Children.Add(yes);

        var panel = new StackPanel { Spacing = 16, Margin = new Thickness(20) };
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(buttons);

        var dialog = new Window
        {
            Title = title,
            Content = panel,
            Width = 380,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        yes.Click += (_, _) => { tcs.TrySetResult(true); dialog.Close(); };
        no.Click += (_, _) => { tcs.TrySetResult(false); dialog.Close(); };
        dialog.Closed += (_, _) => tcs.TrySetResult(false);

        dialog.ShowDialog(this);
        return tcs.Task;
    }
}
