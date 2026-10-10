using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LearnSomething.Client.Localization;
using LearnSomething.Client.Services;

namespace LearnSomething.Client.ViewModels;

public class WordListRowViewModel : ObservableObject
{
    public Guid Id { get; }

    public string Name { get; }

    public int WordCount { get; }

    // XAML's StringFormat can't reference a live (non-static) binding source,
    // so this is computed here instead — same "reopen to refresh" limitation
    // as before for this one piece of text specifically.
    public string WordCountLabel => string.Format(Strings.Instance.WordCountFormat, WordCount);

    public bool IsActive { get; }

    // Whether this list is worth offering "restart from scratch" for.
    public bool HasProgress { get; }

    // A list is worth "continuing" (and worth offering a restart on) once
    // it's either the one currently active or carries progress from an
    // earlier session — an untouched, never-active list gets neither: just
    // a single "Start" action, nothing to resume or reset yet.
    public bool IsInPlay => IsActive || HasProgress;

    public string StartStudyingLabel => IsInPlay ? Strings.Instance.StartStudyingLabelContinue : Strings.Instance.StartStudyingLabelStart;

    public IRelayCommand OpenCommand { get; }

    public IRelayCommand StartStudyingCommand { get; }

    public IRelayCommand RestartCommand { get; }

    public IRelayCommand DeleteCommand { get; }

    public WordListRowViewModel(
        WordListSummary summary,
        bool isActive,
        Func<WordListRowViewModel, Task> onOpen,
        Func<WordListRowViewModel, Task> onStartStudying,
        Func<WordListRowViewModel, Task> onRestart,
        Func<WordListRowViewModel, Task> onDelete)
    {
        Id = summary.Id;
        Name = summary.Name;
        WordCount = summary.WordCount;
        IsActive = isActive;
        HasProgress = summary.HasProgress;

        OpenCommand = new AsyncRelayCommand(() => onOpen(this));
        StartStudyingCommand = new AsyncRelayCommand(() => onStartStudying(this));
        RestartCommand = new AsyncRelayCommand(() => onRestart(this));
        DeleteCommand = new AsyncRelayCommand(() => onDelete(this));
    }
}
