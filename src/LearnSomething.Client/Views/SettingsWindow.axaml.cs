using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using LearnSomething.Client.Data;
using LearnSomething.Client.Localization;
using LearnSomething.Client.ViewModels;

namespace LearnSomething.Client.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    // Goes through CardWindowHost rather than holding a CardWindow directly —
    // this window can outlive several card open/close cycles, and a closed
    // Avalonia window can never be Show()'n again, so every access asks the
    // host for whichever instance is currently valid.
    public SettingsWindow(AppServices services, CardWindowHost cardHost)
    {
        InitializeComponent();

        _viewModel = new SettingsViewModel(
            services.WordService,
            services.ListService,
            services.SettingsService,
            services.StartupService,
            onCardShouldReload: () => cardHost.GetOrCreate().ReloadAsync(),
            onResetCardBounds: () => cardHost.GetOrCreate().ApplyDefaultBounds(),
            onShowCard: () =>
            {
                var card = cardHost.GetOrCreate();
                card.Show();
                card.Activate();
            });
        DataContext = _viewModel;

        Opened += (_, _) => _ = _viewModel.InitializeAsync();
    }

    private async void OnImportClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Strings.Instance.FilePickerTitle,
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("CSV") { Patterns = ["*.csv"] }],
        });

        var file = files.Count > 0 ? files[0] : null;
        if (file is null) return;

        await _viewModel.ImportFromFileAsync(file.Path.LocalPath);
    }
}
