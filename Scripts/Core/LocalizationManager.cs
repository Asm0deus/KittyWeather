//using UnityEngine;
//using UnityEngine.Networking;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Newtonsoft.Json;

//[Serializable]
//public class LocalizationConfig
//{
//    public string version;
//    public string defaultLanguage;
//    public List<LanguageInfo> availableLanguages;
//    public Dictionary<string, string> fallbackMap; // {"pt": "es", "de": "en"} — если нет перевода, брать из этого
//}

//[Serializable]
//public class LanguageInfo
//{
//    public string code; // "ru", "en", "es", "fr"
//    public string displayName; // "Русский", "English", "Español", "Français"
//    public bool enabled; // Можно отключить язык без пересборки
//}

//public static class LocalizationManager
//{
//    public static string CurrentLanguage { get; private set; } = "en";
//    public static List<LanguageInfo> AvailableLanguages { get; private set; } = new();

//    private static LocalizationConfig _config;
//    private const string CONFIG_URL = "https://cdn.jsdelivr.net/gh/Asm0deus/KittyWeather/localization_config.json";
//    private const string PREFS_KEY = "PlayerLanguage";

//    public static event Action OnLanguageChanged;

//    /// <summary>
//    /// Инициализация (вызывать из GameBootstrap)
//    /// </summary>
//    public static async void Init()
//    {
//        // 1. Загружаем конфиг языков (если есть)
//        await LoadConfigAsync();

//        // 2. Определяем язык игрока
//        string savedLang = PlayerPrefs.GetString(PREFS_KEY, "");

//        if (!string.IsNullOrEmpty(savedLang) && IsLanguageEnabled(savedLang))
//        {
//            CurrentLanguage = savedLang;
//        }
//        else
//        {
//            // Автоопределение по системе
//            CurrentLanguage = DetectSystemLanguage();
//            PlayerPrefs.SetString(PREFS_KEY, CurrentLanguage);
//            PlayerPrefs.Save();
//        }

//        Debug.Log($"[Localization] Язык установлен: {CurrentLanguage}");
//    }

//    /// <summary>
//    /// Загрузка конфига языков с CDN
//    /// </summary>
//    private static async System.Threading.Tasks.Task LoadConfigAsync()
//    {
//        try
//        {
//            using (var req = UnityWebRequest.Get(CONFIG_URL))
//            {
//                req.timeout = 5;
//                var operation = req.SendWebRequest();
//                while (!operation.isDone) await System.Threading.Tasks.Task.Yield();

//                if (req.result == UnityWebRequest.Result.Success)
//                {
//                    _config = JsonConvert.DeserializeObject<LocalizationConfig>(req.downloadHandler.text);
//                    if (_config?.availableLanguages != null)
//                    {
//                        AvailableLanguages = _config.availableLanguages.Where(l => l.enabled).ToList();
//                        Debug.Log($"[Localization] Загружено {AvailableLanguages.Count} языков из конфига");
//                    }
//                }
//            }
//        }
//        catch (Exception e)
//        {
//            Debug.LogWarning($"[Localization] Не удалось загрузить конфиг: {e.Message}. Используем дефолт.");
//        }

//        // Фоллбэк: если конфиг не загрузился — хардкод-список
//        if (AvailableLanguages.Count == 0)
//        {
//            AvailableLanguages = new List<LanguageInfo>
//            {
//                new LanguageInfo { code = "ru", displayName = "Русский", enabled = true },
//                new LanguageInfo { code = "en", displayName = "English", enabled = true },
//                new LanguageInfo { code = "es", displayName = "Español", enabled = true },
//                new LanguageInfo { code = "fr", displayName = "Français", enabled = true }
//            };
//            Debug.Log("[Localization] Использован хардкод-список языков");
//        }
//    }

//    /// <summary>
//    /// Автоопределение языка по системе
//    /// </summary>
//    private static string DetectSystemLanguage()
//    {
//        var systemLang = Application.systemLanguage switch
//        {
//            SystemLanguage.Russian => "ru",
//            SystemLanguage.English => "en",
//            SystemLanguage.Spanish => "es",
//            SystemLanguage.French => "fr",
//            SystemLanguage.German => "de",
//            SystemLanguage.Portuguese => "pt",
//            SystemLanguage.Italian => "it",
//            SystemLanguage.Japanese => "ja",
//            SystemLanguage.Korean => "ko",
//            SystemLanguage.Chinese => "zh",
//            _ => "en"
//        };

//        // Если язык есть в доступных — возвращаем его
//        if (IsLanguageEnabled(systemLang)) return systemLang;

//        // Иначе пробуем фоллбэк-мапу (pt → es, de → en и т.д.)
//        if (_config?.fallbackMap?.ContainsKey(systemLang) == true)
//        {
//            string fallback = _config.fallbackMap[systemLang];
//            if (IsLanguageEnabled(fallback)) return fallback;
//        }

//        // Иначе дефолт из конфига или "en"
//        return _config?.defaultLanguage ?? "en";
//    }

//    /// <summary>
//    /// Проверка, включён ли язык в конфиге
//    /// </summary>
//    public static bool IsLanguageEnabled(string code)
//    {
//        if (AvailableLanguages.Count == 0) return true; // Если список пуст — все включены
//        return AvailableLanguages.Any(l => l.code == code && l.enabled);
//    }

//    /// <summary>
//    /// Смена языка в рантайме
//    /// </summary>
//    public static bool SetLanguage(string code)
//    {
//        if (!IsLanguageEnabled(code))
//        {
//            Debug.LogWarning($"[Localization] Язык {code} недоступен или отключён");
//            return false;
//        }

//        CurrentLanguage = code;
//        PlayerPrefs.SetString(PREFS_KEY, code);
//        PlayerPrefs.Save();

//        Debug.Log($"[Localization] Язык изменён на {code}");

//        // Уведомляем всех подписчиков
//        OnLanguageChanged?.Invoke();

//        return true;
//    }

//    /// <summary>
//    /// Получить список кодов доступных языков (для UI-выпадашки)
//    /// </summary>
//    public static List<string> GetAvailableLanguageCodes()
//    {
//        return AvailableLanguages.Where(l => l.enabled).Select(l => l.code).ToList();
//    }

//    /// <summary>
//    /// Получить отображаемые названия языков (для UI-выпадашки)
//    /// </summary>
//    public static List<string> GetAvailableLanguageNames()
//    {
//        return AvailableLanguages.Where(l => l.enabled).Select(l => l.displayName).ToList();
//    }
//}