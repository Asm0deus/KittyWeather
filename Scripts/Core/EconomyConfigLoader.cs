using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Collections;
using Newtonsoft.Json;
using System.Collections.Generic;

// === МОДЕЛИ ДАННЫХ (синхронизированы с EconomyConfig.json) ===
[Serializable]
public class EconomyConfig
{
    public string version;
    public CurrenciesData currencies;
    public StartingBalance startingBalance;
}

[Serializable]
public class CurrenciesData
{
    // Ключи в JSON строковые ("0.99", "30"), поэтому Dictionary<string, int>
    public Dictionary<string, int> usdToGems;
    public Dictionary<string, int> gemsToCoins;
    public int defaultGemsToCoinsRate;
}

[Serializable]
public class StartingBalance
{
    public int coins;
    public int gems;
}

public class EconomyConfigLoader : MonoBehaviour
{
    public static EconomyConfigLoader Instance { get; private set; }
    public static EconomyConfig Current { get; private set; }
    public static event Action<EconomyConfig> OnConfigLoaded;

    [Header("CDN URL")]
    [SerializeField] private string configUrl = "https://cdn.jsdelivr.net/gh/Asm0deus/KittyWeather/EconomyConfig.json";
    [Header("Fallback")]
    [SerializeField] private string defaultFileName = "EconomyConfig_default.json";
    [SerializeField] private string cacheFileName = "economy_config_cache.json";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start() => StartCoroutine(LoadWithFallback());

    // ЕДИНАЯ КОРУТИНА: последовательный перебор источников без вложенных возвратов
    IEnumerator LoadWithFallback()
    {
        bool success = false;

        // 1. Пробуем CDN
        using (var req = UnityWebRequest.Get(configUrl))
        {
            req.timeout = 8;
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                success = TryParseAndApply(req.downloadHandler.text);
                if (success) SaveToCache(req.downloadHandler.text);
            }
        }

        // 2. Пробуем локальный кэш
        if (!success)
        {
            string cachePath = Path.Combine(Application.persistentDataPath, cacheFileName);
            if (File.Exists(cachePath)) success = TryParseAndApply(File.ReadAllText(cachePath));
        }

        // 3. Пробуем StreamingAssets (фоллбэк для оффлайна/первого запуска)
        if (!success)
        {
            string srcPath = Path.Combine(Application.streamingAssetsPath, defaultFileName);
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var req = UnityWebRequest.Get(srcPath))
                {
                    yield return req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success) success = TryParseAndApply(req.downloadHandler.text);
                }
            }
            else if (File.Exists(srcPath)) success = TryParseAndApply(File.ReadAllText(srcPath));
        }

        // 4. Последний рубеж: встроенный хардкод (для разработки)
        if (!success) ApplyHardcodedDefault();

        // Уведомляем подписчиков (в т.ч. GameBootstrap)
        OnConfigLoaded?.Invoke(Current);
        Debug.Log($"[Economy] ✅ Конфиг готов. Версия: {Current?.version ?? "N/A"}");
    }

    private bool TryParseAndApply(string json)
    {
        try
        {
            json = json.Trim('\ufeff', '\u200b', ' ', '\n', '\r');
            var cfg = JsonConvert.DeserializeObject<EconomyConfig>(json);
            if (cfg != null) { Current = cfg; return true; }
        }
        catch (Exception e) { Debug.LogError($"[Economy] Ошибка парсинга: {e.Message}"); }
        return false;
    }


    private void SaveToCache(string json)
    {
        try { File.WriteAllText(Path.Combine(Application.persistentDataPath, cacheFileName), json); }
        catch (Exception e) { Debug.LogError($"[Economy] Кэш не сохранён: {e.Message}"); }
    }

    private void ApplyHardcodedDefault()
    {
        Debug.LogWarning("[Economy] ⚠️ Все источники недоступны. Применён хардкод-дефолт с бонусной шкалой.");

        Current = new EconomyConfig
        {
            version = "dev-fallback-v1",
            currencies = new CurrenciesData
            {
                // Бонусная шкала: чем больше покупаешь, тем выгоднее курс
                // Ключи — строки, как в JSON: "0.99", "30", etc.
                usdToGems = new System.Collections.Generic.Dictionary<string, int>
            {
                { "0.99", 30 },   // 1$ = 30💎
                { "1.99", 70 },   // 2$ = 70💎 (+10 бонус)
                { "4.99", 160 },  // 5$ = 160💎 (+20 бонус)
                { "9.99", 380 },  // 10$ = 380💎 (+40 бонус)
                { "19.99", 950 }  // 20$ = 950💎 (+100 бонус)
            },
                gemsToCoins = new System.Collections.Generic.Dictionary<string, int>
            {
                { "30", 300 },    // 30💎 = 300🐟 (x10)
                { "75", 825 },    // 75💎 = 825🐟 (x11)
                { "140", 1570 },  // 140💎 = 1570🐟 (x11.2)
                { "180", 2090 },  // 180💎 = 2090🐟 (x11.6)
                { "380", 4720 }   // 380💎 = 4720🐟 (x12.4) — максимальный бонус
            },
                defaultGemsToCoinsRate = 10 // Фоллбэк для неизвестных значений
            },
            startingBalance = new StartingBalance { coins = 500, gems = 0 }
        };
    }

    // Динамический поиск курса конвертации гемов в коины
    public static int GetGemsToCoinsRate(int gemsAmount)
    {
        if (Current?.currencies?.gemsToCoins == null) return Current?.currencies?.defaultGemsToCoinsRate ?? 10;

        if (Current.currencies.gemsToCoins.TryGetValue(gemsAmount.ToString(), out int rate))
            return rate;

        return Current.currencies.defaultGemsToCoinsRate;
    }

    // Динамический поиск курса USD -> Gems
    public static int GetUsdToGemsRate(float usdPrice)
    {
        if (Current?.currencies?.usdToGems == null) return 0;
        string key = usdPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        if (Current.currencies.usdToGems.TryGetValue(key, out int gems)) return gems;
        return 0;
    }
}