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

        // Every way of closing the card (× button, quitting the app, ...) goes
        // through Closing, so the last position/size is always saved. Not
        // cancelled, so no re-entry/recursion risk.
        Closing += (_, _) => SaveBoundsBlocking();

        // Anywhere on the card that isn't a control that handles the press
        // itself (buttons do) starts a window drag.
        PointerPressed += OnWindowPointerPressed;

        var s = services.Settings;
        var hasSavedBounds = s.CardX is not null && s.CardY is not null && s.CardWidth is not null && s.CardHeight is not null;
        if (hasSavedBounds)
        {
            Width = s.CardWidth!.Value;
            Height = s.CardHeight!.Value;
            Position = new PixelPoint((int)s.CardX!.Value, (int)s.CardY!.Value);
        }

        Opened += (_, _) =>
        {
            if (!hasSavedBounds)
            {
                ApplyDefaultPosition();
            }

            EnsureOnScreen();
            _ = ReloadAsync();
        };
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

    // Position and the screen's working area are in physical pixels, but
    // Width and ScreenMargin are DIPs — they must be scaled before mixing, or
    // at 125%+ display scaling the card ends up hanging off the right edge
    // (taking the × button with it).
    private void ApplyDefaultPosition()
    {
        if (Screens.Primary is not { } screen) return;

        var workingArea = screen.WorkingArea;
        var widthPx = (int)Math.Round(Width * screen.Scaling);
        var marginPx = (int)Math.Round(ScreenMargin * screen.Scaling);

        Position = new PixelPoint(
            workingArea.X + workingArea.Width - widthPx - marginPx,
            workingArea.Y + marginPx);
    }

    // On open only (not while the user drags): pulls a card that would start
    // partly or fully off-screen — e.g. saved on a monitor that's since been
    // unplugged — back into the visible work area.
    private void EnsureOnScreen()
    {
        var screen = Screens.All.FirstOrDefault(sc => sc.WorkingArea.Contains(Position)) ?? Screens.Primary;
        if (screen is null) return;

        var workingArea = screen.WorkingArea;
        var widthPx = (int)Math.Round(ClientSize.Width * screen.Scaling);
        var heightPx = (int)Math.Round(ClientSize.Height * screen.Scaling);

        var x = Math.Clamp(Position.X, workingArea.X, Math.Max(workingArea.X, workingArea.Right - widthPx));
        var y = Math.Clamp(Position.Y, workingArea.Y, Math.Max(workingArea.Y, workingArea.Bottom - heightPx));

        if (x != Position.X || y != Position.Y)
        {
            Position = new PixelPoint(x, y);
        }
    }

    private void OnSettingsRequested()
    {
        var settingsWindow = new SettingsWindow(_services, _host);
        settingsWindow.Show();
    }

    private void OnCloseButtonPressed() => Close();

    private Task SaveBoundsAsync() => PersistBoundsAsync(Position.X, Position.Y, ClientSize.Width, ClientSize.Height);

    // Closing can't await, so this blocks — but the write runs on a pool
    // thread (geometry is read here, on the UI thread, first), which keeps
    // blocking from ever deadlocking against the UI thread.
    private void SaveBoundsBlocking()
    {
        var (x, y, width, height) = (Position.X, Position.Y, ClientSize.Width, ClientSize.Height);
        Task.Run(() => PersistBoundsAsync(x, y, width, height)).GetAwaiter().GetResult();
    }

    private async Task PersistBoundsAsync(double x, double y, double width, double height)
    {
        await _services.SettingsService.SaveCardBoundsAsync(x, y, width, height);

        // A card built later this session reads AppServices.Settings (a copy
        // taken at startup) in its constructor — keep it current so it
        // reopens where the last one was closed, not where the app launched.
        var settings = _services.Settings;
        settings.CardX = x;
        settings.CardY = y;
        settings.CardWidth = width;
        settings.CardHeight = height;
    }

    private void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private ResizeEdges _resizeEdges;
    private PixelPoint _resizeStartPointer;
    private PixelPoint _resizeStartPosition;
    private Size _resizeStartSize;

    private void OnGripPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control grip || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        _resizeEdges = grip.Tag switch
        {
            "Left" => ResizeEdges.Left,
            "Right" => ResizeEdges.Right,
            "Bottom" => ResizeEdges.Bottom,
            "BottomLeft" => ResizeEdges.Bottom | ResizeEdges.Left,
            "BottomRight" => ResizeEdges.Bottom | ResizeEdges.Right,
            _ => ResizeEdges.None,
        };

        _resizeStartPointer = this.PointToScreen(e.GetPosition(this));
        _resizeStartPosition = Position;
        _resizeStartSize = ClientSize;
        e.Pointer.Capture(grip);
        e.Handled = true;
    }

    private void OnGripPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_resizeEdges == ResizeEdges.None) return;

        var scale = RenderScaling;
        var pointer = this.PointToScreen(e.GetPosition(this));
        var dx = (pointer.X - _resizeStartPointer.X) / scale;
        var dy = (pointer.Y - _resizeStartPointer.Y) / scale;

        var (width, height, positionX) = WindowResizeMath.Compute(
            _resizeEdges, _resizeStartSize.Width, _resizeStartSize.Height, _resizeStartPosition.X,
            dx, dy, MinWidth, MinHeight, scale);

        Width = width;
        Height = height;
        if (_resizeEdges.HasFlag(ResizeEdges.Left))
        {
            Position = new PixelPoint(positionX, Position.Y);
        }
    }

    private void OnGripPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _resizeEdges = ResizeEdges.None;
        e.Pointer.Capture(null);
    }
}
