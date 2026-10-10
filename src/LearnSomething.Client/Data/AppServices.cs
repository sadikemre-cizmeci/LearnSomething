using LearnSomething.Client.Services;

namespace LearnSomething.Client.Data;

public record AppServices(
    WordService WordService,
    WordListService ListService,
    SettingsService SettingsService,
    WordQueueService QueueService,
    StartupService StartupService,
    AppSettingsEntity Settings);
