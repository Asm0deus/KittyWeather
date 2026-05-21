// AchievementTracker.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

public class AchievementTracker : MonoBehaviour
{
    public static AchievementTracker Instance { get; private set; }

    // Объединённые данные: ID -> RuntimeAchievement RuntimeAchievement объединяет определение + прогресс
    public Dictionary<string, RuntimeAchievement> Achievements { get; private set; } = new();

    // События для UI и звуков
    public static event Action<AchievementDefinition> OnAchievementUnlocked;
    public static event Action<string, float> OnProgressUpdated;

    private const string PREFS_KEY = "AchievementProgressCache";
    private bool _isInitialized = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Инициализация: объединяет CDN-определения с сохранённым прогрессом
    /// </summary>
    public async Task InitializeAsync(List<AchievementDefinition> definitions)
    {
        if (_isInitialized) return;

        // 1. Загружаем прогресс (Firestore → PlayerPrefs)
        var localProgress = LoadLocalProgress();
        var cloudProgress = await LoadCloudProgressAsync();

        // 2. Сливаем данные
        Achievements.Clear();
        foreach (var def in definitions)
        {
            // Берём прогресс: облако > локальный кэш > новый
            AchievementProgress prog = null;
            if (cloudProgress?.TryGetValue(def.Id, out var p) == true) prog = p;
            else if (localProgress?.TryGetValue(def.Id, out p) == true) prog = p;
            else prog = new AchievementProgress();

            Achievements[def.Id] = new RuntimeAchievement(def, prog);
        }

        _isInitialized = true;
        Debug.Log($"[Achievements] ✅ Загружено {Achievements.Count} определений.");
    }

    /// <summary>
    /// Отслеживает прогресс по типу условия
    /// </summary>
    public void TrackProgress(string conditionType, float value = 1f)
    {
        if (!_isInitialized) return;

        foreach (var ach in Achievements.Values)
        {
            if (ach.IsUnlocked || ach.Definition.ConditionType != conditionType) continue;

            ach.Progress += value;
            OnProgressUpdated?.Invoke(ach.Id, ach.Progress);

            if (ach.Progress >= ach.Definition.TargetValue)
            {
                UnlockAchievement(ach);
            }
        }
        SaveLocalProgress(); // Кэшируем сразу
    }

    private void UnlockAchievement(RuntimeAchievement ach)
    {
        if (ach.IsUnlocked) return;

        ach.IsUnlocked = true;
        ach.UnlockedDateTime = DateTime.UtcNow;

        CurrencyManager.Instance?.AddCurrency(CurrencyType.Coins, ach.Definition.RewardCoins);

        SaveLocalProgress();
        OnAchievementUnlocked?.Invoke(ach.Definition);
        Debug.Log($"[Achievements] 🏆 Разблокировано: {ach.Id}");
    }

    // === СОХРАНЕНИЕ / ЗАГРУЗКА ===

    private async Task<Dictionary<string, AchievementProgress>> LoadCloudProgressAsync()
    {
        if (!GameBootstrap.CanUseManagers() || string.IsNullOrEmpty(FirebaseAuthManager.UserId))
            return null;
        try { return await FirestoreManager.Instance.LoadAchievementProgressAsync(); }
        catch { return null; }
    }

    private Dictionary<string, AchievementProgress> LoadLocalProgress()
    {
        string json = PlayerPrefs.GetString(PREFS_KEY, "");
        if (string.IsNullOrEmpty(json)) return new Dictionary<string, AchievementProgress>();
        try { return JsonConvert.DeserializeObject<Dictionary<string, AchievementProgress>>(json); }
        catch { return new Dictionary<string, AchievementProgress>(); }
    }

    private void SaveLocalProgress()
    {
        var dict = new Dictionary<string, AchievementProgress>();
        foreach (var kvp in Achievements) dict[kvp.Key] = kvp.Value.ProgressData;

        string json = JsonConvert.SerializeObject(dict);
        PlayerPrefs.SetString(PREFS_KEY, json);
        PlayerPrefs.Save();
    }

    public async void SyncToCloudAsync()
    {
        if (!GameBootstrap.CanUseManagers()) return;
        SaveLocalProgress();
        var dict = new Dictionary<string, AchievementProgress>();
        foreach (var kvp in Achievements) dict[kvp.Key] = kvp.Value.ProgressData;

        await FirestoreManager.Instance.SaveAchievementProgressAsync(dict);
        Debug.Log("[Achievements] 🔄 Прогресс синхронизирован с облаком.");
    }
}

// Вспомогательный класс для удобства: объединяет определение + прогресс
public class RuntimeAchievement
{
    public AchievementDefinition Definition { get; }
    public AchievementProgress ProgressData { get; }

    public RuntimeAchievement(AchievementDefinition def, AchievementProgress prog)
    {
        Definition = def;
        ProgressData = prog;
    }

    public string Id => Definition.Id;
    public float Progress { get => ProgressData.Progress; set => ProgressData.Progress = value; }
    public bool IsUnlocked { get => ProgressData.Unlocked; set => ProgressData.Unlocked = value; }
    public DateTime UnlockedDateTime { get => ProgressData.UnlockedDateTime; set => ProgressData.UnlockedDateTime = value; }
}