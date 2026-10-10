using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LearnSomething.Client.Localization;
using LearnSomething.Client.Services;
using LearnSomething.Shared;

namespace LearnSomething.Client.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly WordService _wordService;
    private readonly WordListService _listService;
    private readonly SettingsService _settingsService;
    private readonly StartupService _startupService;
    private readonly Func<Task> _onCardShouldReload;
    private readonly Action _onResetCardBounds;
    private readonly Action _onShowCard;

    private bool _isLoaded;
    private bool _isLoadingListDetail;
    private Guid? _editingWordId;
    private Guid? _activeListId;

    // --- Ayarlar ---
    [ObservableProperty]
    public partial int DailyWordCount { get; set; } = 1;

    // When on, DailyWordCount applies per list (every list studied at once)
    // instead of to a single active list.
    [ObservableProperty]
    public partial bool StudyAllLists { get; set; }

    [ObservableProperty]
    public partial bool StartOnBoot { get; set; }

    // When on, reaching today's quota (with more genuinely left for later)
    // closes the card instead of showing "Hedefine ulaştın" / "Devam et".
    // Deliberately doesn't apply once a mode has truly run out of words —
    // that (rarer, stronger) state still shows its own message.
    [ObservableProperty]
    public partial bool CloseCardWhenQuotaReached { get; set; }

    // Index matches AppLanguage enum order: 0 Turkish, 1 English. A change
    // takes effect immediately for Strings.Instance.CurrentLanguage, but literal XAML
    // text already on screen only refreshes the next time that window/tab
    // is (re)opened, since it's read via x:Static, not a live binding.
    [ObservableProperty]
    public partial int LanguageIndex { get; set; }

    // --- Listeler ---
    public ObservableCollection<WordListRowViewModel> Lists { get; } = [];

    [ObservableProperty]
    public partial string NewListName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial WordListRowViewModel? OpenList { get; set; }

    // Per-list, and inverted from the stored ShowTermFirst: checked means the
    // card shows the meaning (Translation) first, then reveals the word (Term).
    [ObservableProperty]
    public partial bool ListShowMeaningFirst { get; set; }

    // What "Bu listeyle çalışmaya başla" studies. Index matches StudyMode enum
    // order: 0 NewAndReview, 1 NewOnly, 2 ReviewOnly, 3 LearnedOnly. Separate
    // from how the word list below is *browsed* — that's always grouped by
    // status, not filtered by this.
    [ObservableProperty]
    public partial int ListStudyModeIndex { get; set; }

    // The word list is always grouped into these three — not a single-select
    // filter — so the user can see every category at once.
    public ObservableCollection<WordRowViewModel> NewWords { get; } = [];

    public ObservableCollection<WordRowViewModel> ToRepeatWords { get; } = [];

    public ObservableCollection<WordRowViewModel> LearnedWords { get; } = [];

    // Recomputed alongside the collections above in ReloadWordsAsync — XAML's
    // StringFormat can't reference a live (non-static) binding source, so
    // these stand in for it here instead.
    [ObservableProperty]
    public partial string NewWordsHeading { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ToRepeatHeading { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LearnedHeading { get; set; } = string.Empty;

    // Purely a view preference for browsing here — not persisted, resets to
    // "Tüm kelimeler" every time a list is opened. 0 Tüm kelimeler, 1 Yeni,
    // 2 Tekrar edilecek, 3 Öğrenildi. Independent of ListStudyModeIndex.
    [ObservableProperty]
    public partial int WordFilterIndex { get; set; }

    [ObservableProperty]
    public partial bool ShowNewWordsSection { get; set; } = true;

    [ObservableProperty]
    public partial bool ShowToRepeatSection { get; set; } = true;

    [ObservableProperty]
    public partial bool ShowLearnedSection { get; set; } = true;

    // Whether the CSV about to be imported has a header row to skip.
    // Genuinely ambiguous from the file alone, so this is an explicit choice
    // rather than a guess — defaults to on since that's the common case
    // (and matches every sample file this app ships with).
    [ObservableProperty]
    public partial bool CsvHasHeaderRow { get; set; } = true;

    // The add/edit word form is collapsed by default to keep the list-detail
    // screen calm; it opens on "+ Kelime ekle" or "Düzenle".
    [ObservableProperty]
    public partial bool IsWordFormVisible { get; set; }

    // Drives whether the Durum picker shows at all — a newly added word is
    // always New, so there's nothing to choose there; only an existing word
    // being edited might need its status changed.
    [ObservableProperty]
    public partial bool IsEditingWord { get; set; }

    [ObservableProperty]
    public partial string NewTerm { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewTranslation { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewExample { get; set; } = string.Empty;

    // Index matches WordStatus enum order: 0 New, 1 Learned, 2 ToRepeat.
    [ObservableProperty]
    public partial int NewStatusIndex { get; set; }

    [ObservableProperty]
    public partial string FormTitle { get; set; } = Strings.Instance.AddWordFormTitle;

    [ObservableProperty]
    public partial string SaveButtonText { get; set; } = Strings.Instance.AddSaveButton;

    // NotifyPropertyChangedFor so the status line can collapse to zero height
    // (via HasStatusMessage) instead of always reserving a blank text row.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    public partial string StatusMessage { get; set; } = string.Empty;

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    public SettingsViewModel(
        WordService wordService,
        WordListService listService,
        SettingsService settingsService,
        StartupService startupService,
        Func<Task> onCardShouldReload,
        Action onResetCardBounds,
        Action onShowCard)
    {
        _wordService = wordService;
        _listService = listService;
        _settingsService = settingsService;
        _startupService = startupService;
        _onCardShouldReload = onCardShouldReload;
        _onResetCardBounds = onResetCardBounds;
        _onShowCard = onShowCard;
    }

    public async Task InitializeAsync()
    {
        var settings = await _settingsService.GetAsync();

        DailyWordCount = settings.DailyWordCount;
        StudyAllLists = settings.StudyAllLists;
        StartOnBoot = _startupService.IsEnabled();
        CloseCardWhenQuotaReached = settings.CloseCardWhenQuotaReached;
        LanguageIndex = (int)settings.Language;
        _activeListId = settings.ActiveListId;

        _isLoaded = true;

        await ReloadListsAsync();
    }

    private async Task ReloadListsAsync()
    {
        var summaries = await _listService.GetAllAsync();
        Lists.Clear();
        foreach (var summary in summaries)
        {
            Lists.Add(new WordListRowViewModel(summary, summary.Id == _activeListId, OpenListAsync, StartStudyingAsync, RestartListAsync, DeleteListAsync));
        }

        if (OpenList is not null)
        {
            OpenList = Lists.FirstOrDefault(l => l.Id == OpenList.Id);
        }
    }

    private async Task OpenListAsync(WordListRowViewModel row)
    {
        OpenList = row;
        ClearWordForm();

        _isLoadingListDetail = true;
        var list = await _listService.GetByIdAsync(row.Id);
        ListShowMeaningFirst = !(list?.ShowTermFirst ?? true);
        ListStudyModeIndex = (int)(list?.StudyMode ?? StudyMode.NewAndReview);
        WordFilterIndex = 0;
        _isLoadingListDetail = false;

        await ReloadWordsAsync(row.Id);
    }

    [RelayCommand]
    private void CloseList()
    {
        OpenList = null;
        NewWords.Clear();
        ToRepeatWords.Clear();
        LearnedWords.Clear();
        ClearWordForm();
    }

    private async Task StartStudyingAsync(WordListRowViewModel row)
    {
        _activeListId = row.Id;
        await _settingsService.UpdateAsync(s => s.ActiveListId = row.Id);
        StatusMessage = string.Format(Strings.Instance.ListNowActiveFormat, row.Name);
        await ReloadListsAsync();
        await _onCardShouldReload();
        _onShowCard();
    }

    // Distinct from StartStudyingAsync: wipes every word's progress back to
    // New first, so the same list can be studied from scratch rather than
    // only ever resumed where it was left off.
    private async Task RestartListAsync(WordListRowViewModel row)
    {
        await _wordService.ResetListProgressAsync(row.Id);
        _activeListId = row.Id;
        await _settingsService.UpdateAsync(s => s.ActiveListId = row.Id);
        StatusMessage = string.Format(Strings.Instance.ListRestartedFormat, row.Name);

        await ReloadListsAsync();
        if (OpenList?.Id == row.Id)
        {
            await ReloadWordsAsync(row.Id);
        }

        await _onCardShouldReload();
        _onShowCard();
    }

    private async Task DeleteListAsync(WordListRowViewModel row)
    {
        await _listService.DeleteAsync(row.Id);

        if (OpenList?.Id == row.Id)
        {
            OpenList = null;
            NewWords.Clear();
            ToRepeatWords.Clear();
            LearnedWords.Clear();
        }

        var wasActive = _activeListId == row.Id;
        if (wasActive)
        {
            _activeListId = null;
            await _settingsService.UpdateAsync(s => s.ActiveListId = null);
        }

        await ReloadListsAsync();

        if (wasActive)
        {
            await _onCardShouldReload();
        }
    }

    [RelayCommand]
    private async Task CreateList()
    {
        var name = NewListName.Trim();
        if (name.Length == 0) return;

        await _listService.CreateAsync(name);
        NewListName = string.Empty;
        await ReloadListsAsync();
    }

    // Always shows every word, grouped by status into the three collections —
    // this is a browser/manager view, not filtered by ListStudyModeIndex.
    private async Task ReloadWordsAsync(Guid listId)
    {
        var words = await _wordService.GetByListAsync(listId);

        NewWords.Clear();
        ToRepeatWords.Clear();
        LearnedWords.Clear();

        foreach (var word in words)
        {
            var row = new WordRowViewModel(word, EditWord, DeleteWordAsync);
            var bucket = word.Status switch
            {
                WordStatus.ToRepeat => ToRepeatWords,
                WordStatus.Learned => LearnedWords,
                _ => NewWords,
            };
            bucket.Add(row);
        }

        NewWordsHeading = string.Format(Strings.Instance.NewWordsGroupFormat, NewWords.Count);
        ToRepeatHeading = string.Format(Strings.Instance.ToRepeatGroupFormat, ToRepeatWords.Count);
        LearnedHeading = string.Format(Strings.Instance.LearnedGroupFormat, LearnedWords.Count);
    }

    partial void OnDailyWordCountChanged(int value)
    {
        if (!_isLoaded) return;
        _ = _settingsService.UpdateAsync(s => s.DailyWordCount = value);
    }

    partial void OnStudyAllListsChanged(bool value)
    {
        if (!_isLoaded) return;
        _ = _settingsService.UpdateAsync(s => s.StudyAllLists = value);
        _ = _onCardShouldReload();
    }

    partial void OnListShowMeaningFirstChanged(bool value)
    {
        if (_isLoadingListDetail || OpenList is null) return;
        _ = _listService.SetShowTermFirstAsync(OpenList.Id, !value);
        _ = _onCardShouldReload();
    }

    partial void OnListStudyModeIndexChanged(int value)
    {
        if (_isLoadingListDetail || OpenList is null) return;
        _ = _listService.SetStudyModeAsync(OpenList.Id, (StudyMode)value);
        _ = _onCardShouldReload();
    }

    partial void OnWordFilterIndexChanged(int value)
    {
        ShowNewWordsSection = value is 0 or 1;
        ShowToRepeatSection = value is 0 or 2;
        ShowLearnedSection = value is 0 or 3;
    }

    partial void OnStartOnBootChanged(bool value)
    {
        if (!_isLoaded) return;
        _startupService.SetEnabled(value);
    }

    partial void OnCloseCardWhenQuotaReachedChanged(bool value)
    {
        if (!_isLoaded) return;
        _ = _settingsService.UpdateAsync(s => s.CloseCardWhenQuotaReached = value);
    }

    partial void OnLanguageIndexChanged(int value)
    {
        var language = (AppLanguage)value;
        Strings.Instance.CurrentLanguage = language;
        if (!_isLoaded) return;
        _ = _settingsService.UpdateAsync(s => s.Language = language);
    }

    [RelayCommand]
    private async Task ResetCardBounds()
    {
        await _settingsService.ResetCardBoundsAsync();
        _onResetCardBounds();
        StatusMessage = Strings.Instance.CardBoundsReset;
    }

    [RelayCommand]
    private void StartAddingWord()
    {
        ClearWordForm();
        IsWordFormVisible = true;
    }

    [RelayCommand]
    private async Task SaveWord()
    {
        if (OpenList is null) return;

        var term = NewTerm.Trim();
        var translation = NewTranslation.Trim();

        if (term.Length == 0 || translation.Length == 0)
        {
            StatusMessage = Strings.Instance.TermTranslationRequired;
            return;
        }

        if (_editingWordId is { } id)
        {
            await _wordService.UpdateAsync(id, term, translation, NewExample, (WordStatus)NewStatusIndex);
        }
        else
        {
            await _wordService.AddAsync(OpenList.Id, term, translation, NewExample, (WordStatus)NewStatusIndex);
        }

        var listId = OpenList.Id;
        ClearWordForm();
        await ReloadWordsAsync(listId);
        await ReloadListsAsync();
        await _onCardShouldReload();
    }

    private void EditWord(WordEntry word)
    {
        _editingWordId = word.Id;
        NewTerm = word.Term;
        NewTranslation = word.Translation;
        NewExample = word.Example ?? string.Empty;
        NewStatusIndex = (int)word.Status;
        FormTitle = Strings.Instance.EditWordFormTitle;
        SaveButtonText = Strings.Instance.UpdateSaveButton;
        StatusMessage = string.Empty;
        IsWordFormVisible = true;
        IsEditingWord = true;
    }

    private async Task DeleteWordAsync(WordEntry word)
    {
        await _wordService.DeleteAsync(word.Id);
        if (_editingWordId == word.Id)
        {
            ClearWordForm();
        }

        if (OpenList is not null)
        {
            var listId = OpenList.Id;
            await ReloadWordsAsync(listId);
            await ReloadListsAsync();
            await _onCardShouldReload();
        }
    }

    [RelayCommand]
    private void CancelEdit() => ClearWordForm();

    private void ClearWordForm()
    {
        _editingWordId = null;
        NewTerm = string.Empty;
        NewTranslation = string.Empty;
        NewExample = string.Empty;
        NewStatusIndex = 0;
        FormTitle = Strings.Instance.AddWordFormTitle;
        SaveButtonText = Strings.Instance.AddSaveButton;
        StatusMessage = string.Empty;
        IsWordFormVisible = false;
        IsEditingWord = false;
    }

    public async Task ImportFromFileAsync(string filePath)
    {
        if (OpenList is null) return;

        try
        {
            var listId = OpenList.Id;
            var count = await _wordService.ImportFromCsvAsync(listId, filePath, CsvHasHeaderRow);
            StatusMessage = string.Format(Strings.Instance.ImportedCountFormat, count);
            await ReloadWordsAsync(listId);
            await ReloadListsAsync();
            await _onCardShouldReload();
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Strings.Instance.ImportFailedFormat, ex.Message);
        }
    }
}
