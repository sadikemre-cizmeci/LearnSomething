using LearnSomething.Client.Data;

namespace LearnSomething.Client.Views;

// Avalonia windows are one-shot: once Close()'d (the card's × button), the
// same instance can never be Show()'n again — attempting to throws
// InvalidOperationException. SettingsWindow can stay open across the card
// being closed and reopened many times, so it can't hold a raw CardWindow
// reference; it goes through this host instead, which transparently builds
// a fresh CardWindow whenever the current one has been closed.
public class CardWindowHost
{
    private readonly AppServices _services;
    private CardWindow _current;

    public CardWindowHost(AppServices services)
    {
        _services = services;
        _current = new CardWindow(services, this);
    }

    public CardWindow GetOrCreate()
    {
        if (_current.IsClosed)
        {
            _current = new CardWindow(_services, this);
        }

        return _current;
    }
}
