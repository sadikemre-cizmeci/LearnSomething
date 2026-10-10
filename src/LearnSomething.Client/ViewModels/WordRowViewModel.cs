using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LearnSomething.Client.Localization;
using LearnSomething.Shared;

namespace LearnSomething.Client.ViewModels;

// Wraps a WordEntry with its own zero-argument commands so the settings
// list's DataTemplate never needs to reach back up to the parent view model.
public class WordRowViewModel : ObservableObject
{
    public WordEntry Entry { get; }

    public string Term => Entry.Term;

    public string Translation => Entry.Translation;

    public string StatusLabel => Entry.Status switch
    {
        WordStatus.Learned => Strings.Instance.StatusLearned,
        WordStatus.ToRepeat => Strings.Instance.StatusToRepeat,
        _ => Strings.Instance.StatusNew,
    };

    public IRelayCommand EditCommand { get; }

    public IRelayCommand DeleteCommand { get; }

    public WordRowViewModel(WordEntry entry, Action<WordEntry> onEdit, Func<WordEntry, Task> onDelete)
    {
        Entry = entry;
        EditCommand = new RelayCommand(() => onEdit(entry));
        DeleteCommand = new AsyncRelayCommand(() => onDelete(entry));
    }
}
