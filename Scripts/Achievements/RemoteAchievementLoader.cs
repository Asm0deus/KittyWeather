// version 1.0.0
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class RemoteAchievementLoader : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private string serverUrl = "https://cdn.jsdelivr.net/gh/Asm0deus/KittyWeather/achievements_def.json";
    [Header("UI")]
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Transform contentPanel;

    [Header("Cache")]
    [SerializeField] private string cacheFileName = "achievements_cache.json";
    [SerializeField] private string defaultCacheFile = "achievements_def.json";

    private async void Start()
    {
        await GameBootstrap.WaitForReadyAsync();
        StartCoroutine(FetchData());
    }

    IEnumerator FetchData()
    {
        using (UnityWebRequest req = UnityWebRequest.Get(serverUrl))
        {
            req.timeout = 10;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                if (TryParseAndSet(json))
                {
                    SaveToCache(json);
                    Debug.Log("[Achievements] Updated & cached.");
                }
                else LoadFromCache();
            }
            else
            {
                Debug.LogWarning("[Achievements] Network error. Fallback to cache.");
                LoadFromCache();
            }
        }
    }

    private void RefreshAchievementsUI()
    {
        // Пересобираем UI с новыми языками
        BuildUI();
    }

    bool TryParseAndSet(string json)
    {
        try
        {
            json = json.Trim('\ufeff', '\u200b', ' ', '\n', '\r');

            // Пробуем парсить новый формат (AchievementDefinition)
            var wrapper = JsonHelper.ParseObject<AchievementListWrapper>(json);

            if (wrapper?.Definitions != null && wrapper.Definitions.Length > 0)
            {
                // ✅ Запускаем инициализацию трекера и ЖДЁМ её завершения перед сборкой UI
                var initTask = AchievementTracker.Instance.InitializeAsync(new List<AchievementDefinition>(wrapper.Definitions));

                // В корутине можно ждать асинхронную задачу
                StartCoroutine(WaitForInitAndBuild(initTask));
                return true;
            }

            Debug.LogError("[Achievements] ❌ Не найдено определений в файле");
            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Achievements] Parse error: {e.Message}");
            return false;
        }
    }

    // Хелпер: ждём инициализацию трекера, потом строим UI
    IEnumerator WaitForInitAndBuild(Task initTask)
    {
        while (!initTask.IsCompleted) yield return null;

        if (initTask.IsCompletedSuccessfully)
        {
            BuildUI();
        }
        else if (initTask.Exception != null)
        {
            Debug.LogError($"[Achievements] Ошибка инициализации трекера: {initTask.Exception.Message}");
        }
    }

    void SaveToCache(string json)
    {
        try { File.WriteAllText(Path.Combine(Application.persistentDataPath, cacheFileName), json); }
        catch (System.Exception e) { Debug.LogError($"[Achievements] Cache save failed: {e.Message}"); }
    }

    void LoadFromCache()
    {
        string cachePath = Path.Combine(Application.persistentDataPath, cacheFileName);
        if (File.Exists(cachePath))
        {
            string json = File.ReadAllText(cachePath).Trim('\ufeff', ' ', '\n', '\r');
            if (TryParseAndSet(json)) return;
        }

        Debug.Log("[Achievements] Cache missing. Loading default...");
        string defPath = Path.Combine(Application.streamingAssetsPath, defaultCacheFile);
        if (Application.platform == RuntimePlatform.Android)
            StartCoroutine(LoadDefaultAndroid(defPath));
        else if (File.Exists(defPath))
            TryParseAndSet(File.ReadAllText(defPath).Trim('\ufeff', ' ', '\n', '\r'));
    }

    IEnumerator LoadDefaultAndroid(string srcPath)
    {
        using (UnityWebRequest req = UnityWebRequest.Get(srcPath))
        {
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
                TryParseAndSet(req.downloadHandler.text.Trim('\ufeff', ' ', '\n', '\r'));
        }
    }

    // Строим UI из данных трекера, а не из локального кэша
    void BuildUI()
    {
        if (AchievementTracker.Instance == null || !AchievementTracker.Instance.Achievements.Any())
        {
            Debug.LogWarning("[Achievements] ⚠️ Трекер пуст или не инициализирован");
            return;
        }

        // Очищаем старый контент
        foreach (Transform child in contentPanel) Destroy(child.gameObject);

        // Сортировка: сначала открытые (по редкости), потом заблокированные (по прогрессу)
        var unlocked = AchievementTracker.Instance.Achievements.Values
            .Where(x => x.IsUnlocked)
            .OrderBy(x => x.ProgressData.Rarity);

        var locked = AchievementTracker.Instance.Achievements.Values
            .Where(x => !x.IsUnlocked)
            .OrderByDescending(x => x.ProgressData.Progress);

        foreach (var runtimeAch in unlocked.Concat(locked))
        {
            var inst = Instantiate(itemPrefab, contentPanel);
            var ui = inst.GetComponent<AchievementUIItemJson>();
            if (ui != null)
            {
                ui.SetData(runtimeAch); // Передаём RuntimeAchievement
            }
            else
            {
                Debug.LogWarning("[Achievements] ⚠️ У префаба нет компонента AchievementUIItemJson");
            }
        }

        Debug.Log($"[Achievements] 🎨 Построено {AchievementTracker.Instance.Achievements.Count} элементов UI");
    }
}