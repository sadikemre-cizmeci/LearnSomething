using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using LearnSomething.Client.Data;
using LearnSomething.Client.ViewModels;

namespace LearnSomething.Client.Views;

public partial class CardWindow : Window
{
    // Must match the Width/Height set in CardWindow.axaml — kept here too so
    // "reset to defaults" can restore them without re-reading the XAML.
    private const double DefaultWidth = 380;
    private const double DefaultHeight = 330;
    private const int ScreenMargin = 24;

    private readonly AppServices _services;
    private readonly CardWindowHost _host;
    private readonly CardViewModel _viewModel;

    // True once this window has been Close()'d — Avalonia windows are
    // one-shot, so anything wanting to show the card again after this needs
    // to go through CardWindowHost instead of reusing this instance.
    public bool IsClosed { get; private set; }

    public CardWindow(AppServices services, CardWindowHost host)
    {
        InitializeComponent();

        _services = services;
        _host = host;
        _viewModel = new CardViewModel(services.QueueService);
        DataContext = _viewModel;

        _viewModel.SettingsRequested += OnSettingsRequested;
        _viewModel.HideRequested += OnCloseButtonPressed;

        Closed += (_, _) => IsClosed = true;

        var s = services.Settings;
        if (s.CardX is not null && s.CardY is not null && s.CardWidth is not null && s.CardHeight is not null)
        {
            Width = s.CardWidth.Value;
            Height = s.CardHeight.Value;
            Position = new PixelPoint((int)s.CardX.Value, (int)s.CardY.Value);
        }
        else
        {
            Opened += (_, _) => ApplyDefaultPosition();
        }

        Opened += (_, _) => _ = ReloadAsync();
    }

    // Re-reads settings + the active list from the DB and rebuilds the card's
    // queue. Safe to call anytime, not just at startup — Settings calls this
    // whenever something that affects what the card shows changes live.
    public async Task ReloadAsync()
    {
        var settings = await _services.SettingsService.GetAsync();

        Dictionary<Guid, bool> showTermFirstByListId;
        bool hasWordSource;

        if (settings.StudyAllLists)
        {
            var lists = await _services.ListService.GetAllListsAsync();
            showTermFirstByListId = lists.ToDictionary(l => l.Id, l => l.ShowTermFirst);
            hasWordSource = lists.Count > 0;
        }
        else if (settings.ActiveListId is { } listId)
        {
            var list = await _services.ListService.GetByIdAsync(listId);
            showTermFirstByListId = new Dictionary<Guid, bool> { [listId] = list?.ShowTermFirst ?? true };
            hasWordSource = true;
        }
        else
        {
            showTermFirstByListId = [];
            hasWordSource = false;
        }

        await _viewModel.InitializeAsync(showTermFirstByListId, hasWordSource);
    }

    public void ApplyDefaultBounds()
    {
        Width = DefaultWidth;
        Height = DefaultHeight;
        ApplyDefaultPosition();
        _ = SaveBoundsAsync();
    }

    private void ApplyDefaultPosition()
    {
        if (Screens.Primary?.WorkingArea is not { } workingArea) return;

        Position = new PixelPoint(
            workingArea.X + workingArea.Width - (int)Width - ScreenMargin,
            workingArea.Y + ScreenMargin);
    }

    private void OnSettingsRequested()
    {
        var settingsWindow = new SettingsWindow(_services, _host);
        settingsWindow.Show();
    }

    private async void OnCloseButtonPressed()
    {
        await SaveBoundsAsync();
        Close();
    }

    private Task SaveBoundsAsync()
    {
        return _services.SettingsService.SaveCardBoundsAsync(Position.X, Position.Y, Width, Height);
    }

    private void OnDragAreaPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}
