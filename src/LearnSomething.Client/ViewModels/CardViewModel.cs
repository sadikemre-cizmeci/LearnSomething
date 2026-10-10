using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LearnSomething.Client.Services;
using LearnSomething.Shared;

namespace LearnSomething.Client.ViewModels;

public partial class CardViewModel : ViewModelBase
{
    private readonly WordQueueService _queueService;
    private readonly Queue<WordEntry> _queue = new();
    private readonly HashSet<Guid> _shownIds = [];
    private WordEntry? _current;
    private Dictionary<Guid, bool> _showTermFirstByListId = [];
    private bool _hasActiveList = true;

    [ObservableProperty]
    public partial string FrontText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string BackText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ExampleText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasExample { get; set; }

    [ObservableProperty]
    public partial bool IsRevealed { get; set; }

    [ObservableProperty]
    public partial bool HasCurrentWord { get; set; }

    [ObservableProperty]
    public partial bool IsQuotaReached { get; set; }

    [ObservableProperty]
    public partial bool HasNoActiveList { get; set; }

    // True only once every list in scope has genuinely run out of words for
    // its own study mode — a stronger, rarer state than IsQuotaReached
    // (today's cap hit, but more exists for later), checked fresh every
    // time the queue empties since studying during this session can change
    // the answer (e.g. the very last New word just got marked Learned).
    [ObservableProperty]
    public partial bool IsAllFinished { get; set; }

    [ObservableProperty]
    public partial string AllFinishedMessage { get; set; } = string.Empty;

    public event Action? SettingsRequested;
    public event Action? HideRequested;

    public CardViewModel(WordQueueService queueService)
    {
        _queueService = queueService;
    }

    // Safe to call more than once (e.g. the active list changed while the
    // card is already open) — resets in-memory queue state before rebuilding.
    // showTermFirstByListId lets each word use its own list's direction
    // preference, since a single session can now mix words from several lists.
    public async Task InitializeAsync(Dictionary<Guid, bool> showTermFirstByListId, bool hasActiveList)
    {
        _showTermFirstByListId = showTermFirstByListId;
        _hasActiveList = hasActiveList;
        _queue.Clear();
        _shownIds.Clear();

        var daily = await _queueService.BuildTodaysQueueAsync();
        foreach (var word in daily.All)
        {
            _queue.Enqueue(word);
        }

        await ShowNextAsync();
    }

    private async Task ShowNextAsync()
    {
        if (_queue.Count == 0)
        {
            _current = null;
            IsRevealed = false;
            HasCurrentWord = false;

            var completion = 
            _hasActiveList ? 
                await _queueService.CheckCompletionAsync() :
                new CompletionStatus(false, null);

            IsAllFinished = completion.AllFinished;
            AllFinishedMessage = completion.Message ?? string.Empty;
            IsQuotaReached = _hasActiveList && !completion.AllFinished;
            HasNoActiveList = !_hasActiveList;
            return;
        }

        _current = _queue.Dequeue();
        _shownIds.Add(_current.Id);
        await _queueService.RecordWordShownAsync(_current.ListId);

        var showTermFirst = _showTermFirstByListId.GetValueOrDefault(_current.ListId, true);
        FrontText = showTermFirst ? _current.Term : _current.Translation;
        BackText = showTermFirst ? _current.Translation : _current.Term;
        ExampleText = _current.Example ?? string.Empty;
        HasExample = !string.IsNullOrWhiteSpace(_current.Example);

        IsRevealed = false;
        HasCurrentWord = true;
        IsQuotaReached = false;
        IsAllFinished = false;
        HasNoActiveList = false;
    }

    [RelayCommand]
    private void Reveal() => IsRevealed = true;

    [RelayCommand]
    private async Task Learned()
    {
        if (_current is null) return;

        await _queueService.MarkLearnedAsync(_current.Id);
        await ShowNextAsync();
    }

    [RelayCommand]
    private async Task Repeat()
    {
        if (_current is null) return;

        await _queueService.MarkForRepeatAsync(_current.Id);
        await ShowNextAsync();
    }

    [RelayCommand]
    private async Task ContinueBeyondQuota()
    {
        var more = await _queueService.GetMoreNewWordsAsync(_shownIds);
        if (more.Count == 0) return;

        // Bypasses today's cap for every list in scope from now on — even
        // if the card is closed and reopened again later today.
        await _queueService.MarkQuotaExceededForScopeAsync();

        foreach (var word in more)
        {
            _queue.Enqueue(word);
        }

        await ShowNextAsync();
    }

    [RelayCommand]
    private void Close() => HideRequested?.Invoke();

    [RelayCommand]
    private void OpenSettings() => SettingsRequested?.Invoke();
}
