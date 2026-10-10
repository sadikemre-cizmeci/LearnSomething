using System.ComponentModel;
using System.Reflection;
using LearnSomething.Shared;

namespace LearnSomething.Client.Localization;

// Every user-facing string in the app, TR/EN. A live singleton (not a plain
// static class) so XAML can bind directly to it via {Binding Source={x:Static
// loc:Strings.Instance}, Path=X} and have the whole UI update the instant the
// language changes — no restart, no reopening a window. CurrentLanguage's
// setter raises PropertyChanged for every string property (found once via
// reflection) so every live binding refreshes together.
public sealed class Strings : INotifyPropertyChanged
{
    public static Strings Instance { get; } = new();

    private static readonly PropertyInfo[] StringProperties = typeof(Strings)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.PropertyType == typeof(string))
        .ToArray();

    public event PropertyChangedEventHandler? PropertyChanged;

    private AppLanguage _currentLanguage = AppLanguage.Turkish;

    public AppLanguage CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage == value) return;
            _currentLanguage = value;
            foreach (var property in StringProperties)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property.Name));
            }
        }
    }

    private Strings()
    {
    }

    private string T(string tr, string en) => CurrentLanguage == AppLanguage.Turkish ? tr : en;

    // --- Window titles ---
    public string CardWindowTitle => "LearnSomething";
    public string SettingsWindowTitle => T("LearnSomething — Ayarlar", "LearnSomething — Settings");

    // --- Card ---
    public string CardCloseTooltip => T("Kapat", "Close");
    public string CardReveal => T("Göster", "Show");
    public string CardGoalReached => T("Hedefine ulaştın 🌿", "You've reached your goal 🌿");
    public string CardAllFinishedHeading => T("Tüm kelimeleri bitirdin! 🎉", "You've finished every word! 🎉");
    public string ListFinishedLineFormat => T("\"{0}\": {1} modunda tüm kelimeler çalışıldı.", "\"{0}\": you've studied every word in {1} mode.");
    public string CardNoListChosen => T("Henüz çalışılacak bir liste seçilmedi", "No list has been chosen to study yet");
    public string CardLearned => T("Öğrendim", "Learned");
    public string CardRepeat => T("Tekrar et", "Repeat");
    public string CardContinue => T("Devam et", "Continue");
    public string CardOpenSettings => T("Ayarları Aç", "Open Settings");
    public string CardSettingsTooltip => T("Ayarlar", "Settings");

    // --- Tab headers ---
    public string TabHome => T("Anasayfa", "Home");
    public string TabSettings => T("Ayarlar", "Settings");
    public string TabLists => T("Listeler", "Lists");
    public string TabStartStudying => T("Çalışmaya Başla", "Start Studying");
    public string TabAddEditWords => T("Kelime Ekle/Düzenle", "Add/Edit Words");

    // --- Home tab ---
    public string HomeTitle => T("LearnSomething'e hoş geldiniz", "Welcome to LearnSomething");
    public string HomeDescription => T(
        "LearnSomething, günde birkaç kelime öğrenerek küçük adımlarla ilerlemenizi sağlayan basit bir kelime kartı uygulamasıdır. Bir liste seçin, kartı açın, kelimeyi görün, anlamını hatırlamaya çalışın.",
        "LearnSomething is a simple flashcard app that helps you make steady progress by learning a few words a day. Pick a list, open the card, see a word, and try to recall its meaning.");
    public string ContactHeading => T("İletişim", "Contact");
    public string ContactEmail => "csadikemre@proton.me";

    // --- Ayarlar tab ---
    public string DailyGoalHeading => T("Günlük hedef", "Daily goal");
    public string DailyWordCountLabel => T("Günlük görmek istediğim kelime sayısı", "Number of words I want to see daily");
    public string StudyAllListsLabel => T("Görmek istenen kelime sayısınca tüm listeleri çalış", "Study all lists for the requested word count");
    public string StudyAllListsExplanation => T(
        "Açıksa bu sayı her listeye ayrı ayrı uygulanır (ör. 5 yazdıysanız her listeden 5 kelime gösterilir); kapalıysa yalnızca üzerinde çalıştığınız liste kullanılır.",
        "When on, this number applies to each list separately (e.g. entering 5 shows 5 words from every list); when off, only the list you're currently studying is used.");
    public string SystemHeading => T("Sistem", "System");
    public string StartOnBootLabel => T("Bilgisayar açılınca uygulamayı otomatik başlat", "Automatically start the app when the computer turns on");
    public string CloseCardWhenQuotaReachedLabel => T("Hedeflenen kelime sınırına ulaşınca kartı kapat", "Close the card once the daily word goal is reached");
    public string LanguageHeading => T("Dil", "Language");
    public string CardHeading => T("Kart", "Card");
    public string ResetCardBoundsButton => T("Ayarları varsayılana getir", "Reset to default");
    public string ResetCardBoundsExplanation => T(
        "Kartın sürükleyip boyutlandırdığınız konum/boyutunu sıfırlar; bir sonraki açılışta yine sağ üst köşede, varsayılan boyutta belirir.",
        "Resets the position/size you dragged and resized the card to; it will appear again in the top-right corner at its default size next time it opens.");

    // --- Listeler tab ---
    public string NewListNamePlaceholder => T("Yeni liste adı", "New list name");
    public string CreateListButton => T("Liste oluştur", "Create list");
    public string CurrentlyStudyingBadge => T("✓ Üzerinde çalışılıyor", "✓ Currently studying");
    public string WordCountFormat => T("{0} kelime", "{0} words");
    public string OpenButton => T("Aç", "Open");
    public string StartStudyingLabelContinue => T("Çalışmaya devam et", "Continue studying");
    public string StartStudyingLabelStart => T("Çalışmaya başla", "Start studying");
    public string DeleteButton => T("Sil", "Delete");
    public string BackButton => T("← Geri", "← Back");
    public string RestartButton => T("Baştan başla", "Restart");
    public string RestartExplanation => T(
        "Baştan başla, bu listedeki tüm kelimeleri yeniden 'Yeni' yapar.",
        "Restart resets every word in this list back to 'New'.");
    public string StudyModeLabel => T("Çalışma modu", "Study mode");
    public string StudyModeNewAndReview => T("Yeni kelimeler + tekrarlar", "New words + reviews");
    public string StudyModeNewOnly => T("Sadece yeni kelimeler", "New words only");
    public string StudyModeReviewOnly => T("Sadece tekrarlar", "Reviews only");
    public string StudyModeLearnedOnly => T("Sadece öğrendiklerim", "Learned words only");
    public string ShowMeaningFirstLabel => T("Önce anlamı ve daha sonra kelimeyi göster", "Show the meaning first, then the word");
    public string ImportCsvButton => T("CSV içe aktar", "Import CSV");
    public string FilePickerTitle => T("Kelime CSV dosyası seç", "Choose a word CSV file");
    public string ImportCsvTooltip => T(
        "CSV formatı: ilk sütun kelime, ikinci sütun anlam, üçüncü sütun örnek cümle (opsiyonel).",
        "CSV format: first column is the word, second is the meaning, third is an optional example sentence.");
    public string CsvHasHeaderRowLabel => T("İlk satır başlık (atlanır)", "First row is a header (skipped)");
    public string AddWordButton => T("+ Kelime ekle", "+ Add word");
    public string RefreshButton => T("Yenile", "Refresh");
    public string FilterAll => T("Tüm kelimeler", "All words");
    public string FilterNew => T("Yeni", "New");
    public string FilterToRepeat => T("Tekrar edilecek", "To repeat");
    public string FilterLearned => T("Öğrenildi", "Learned");
    public string WordsHeading => T("Kelimeler", "Words");
    public string TermPlaceholder => T("Kelime (Term)", "Word (Term)");
    public string TranslationPlaceholder => T("Anlam (Translation)", "Meaning (Translation)");
    public string ExamplePlaceholder => T("Örnek cümle (opsiyonel)", "Example sentence (optional)");
    public string StatusLabel => T("Durum", "Status");
    public string StatusNew => T("Yeni", "New");
    public string StatusLearned => T("Öğrendim", "Learned");
    public string StatusToRepeat => T("Tekrar et", "Repeat");
    public string AddSaveButton => T("Ekle", "Add");
    public string UpdateSaveButton => T("Güncelle", "Update");
    public string CancelButton => T("İptal", "Cancel");
    public string EditButton => T("Düzenle", "Edit");
    public string NewWordsGroupFormat => T("Yeni ({0})", "New ({0})");
    public string ToRepeatGroupFormat => T("Tekrar edilecek ({0})", "To repeat ({0})");
    public string LearnedGroupFormat => T("Öğrenildi ({0})", "Learned ({0})");
    public string AddWordFormTitle => T("Yeni kelime ekle", "Add new word");
    public string EditWordFormTitle => T("Kelimeyi düzenle", "Edit word");

    // --- Dynamic status messages ---
    public string ListNowActiveFormat => T("\"{0}\" artık çalışılan liste.", "\"{0}\" is now the active list.");
    public string ListRestartedFormat => T("\"{0}\" sıfırlandı ve baştan başlatıldı.", "\"{0}\" was reset and restarted.");
    public string TermTranslationRequired => T("Kelime ve anlam boş olamaz.", "Word and meaning can't be empty.");
    public string ImportedCountFormat => T("{0} yeni kelime içe aktarıldı.", "{0} new words imported.");
    public string ImportFailedFormat => T("İçe aktarma başarısız: {0}", "Import failed: {0}");
    public string CsvMissingTermFormat => T("Satır {0}: kelime sütunu boş olamaz.", "Row {0}: the word column can't be empty.");
    public string CsvMissingTranslationFormat => T("Satır {0}: anlam sütunu boş olamaz.", "Row {0}: the meaning column can't be empty.");
    public string CardBoundsReset => T("Kart varsayılan boyut ve konuma döndü.", "Card was reset to its default size and position.");
}
