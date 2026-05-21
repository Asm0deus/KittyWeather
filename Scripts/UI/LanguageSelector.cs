// LanguageSelector.cs
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

//[RequireComponent(typeof(TMP_Dropdown))]
public class LanguageSelector : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown _dropdown;// languageDropdown;
    private bool _isUpdating = false;
    //[SerializeField] private float initTimeout = 5f; // Макс. время ожидания инициализации

    private bool _isInitialized = false;

    private void Start()
    {
        //CultureInfo currentCulture = CultureInfo.CurrentUICulture;
        //LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(currentCulture);

        _dropdown = GetComponent<TMP_Dropdown>();
        _dropdown.ClearOptions();

        // Заполняем список из пакета локализации
        var locales = LocalizationSettings.AvailableLocales?.Locales;
        if (locales == null || locales.Count == 0) return;

        // FIX: Используем Identifier.Code для кода и ToString() для отображения
        var options = locales.Select(l => new TMP_Dropdown.OptionData
        {
            text = GetLocaleDisplayName(l), // "Русский", "English" и т.д.
        }).ToList();

        _dropdown.AddOptions(options);

        // Выбираем текущий язык
        var current = LocalizationSettings.SelectedLocale;
        int idx = locales.IndexOf(current);
        if (idx >= 0) _dropdown.value = idx;

        _dropdown.onValueChanged.AddListener(OnDropdownChanged);

        //// Выбираем текущий язык
        //var current = LocalizationSettings.SelectedLocale;
        //int idx = locales.IndexOf(current);
        //if (idx >= 0) _dropdown.value = idx;

        //// Подписываемся на смену
        //_dropdown.onValueChanged.AddListener(OnDropdownChanged);

        //// Ждём, пока LocalizationManager загрузит конфиг
        ////StartCoroutine(WaitForLocalizationAndSetup());
    }

    // Хелпер: красивое имя локали
    private string GetLocaleDisplayName(UnityEngine.Localization.Locale locale)
    {
        // Можно кастомизировать: взять из Resources или хардкод-словаря
        return locale.Identifier.Code switch
        {
            "ru" => "Русский",
            "en" => "English",
            "es" => "Español",
            "fr" => "Français",
            _ => locale.ToString() // Фоллбэк
        };
    }

    private void OnDropdownChanged(int index)
    {
        if (_isUpdating) return;
        var locales = LocalizationSettings.AvailableLocales.Locales;
        if (index >= 0 && index < locales.Count)
        {
            _isUpdating = true;
            LocalizationSettings.SelectedLocale = locales[index];
            _isUpdating = false;
        }
    }
    /*
    private IEnumerator WaitForLocalizationAndSetup()
    {
        float timer = 0f;

        // Ждём, пока появятся доступные языки
        while (LocalizationManager.AvailableLanguages.Count == 0 && timer < initTimeout)
        {
            yield return null;
            timer += Time.deltaTime;
        }

        if (LocalizationManager.AvailableLanguages.Count > 0)
        {
            SetupDropdown();
            _isInitialized = true;
        }
        else
        {
            Debug.LogWarning("[LanguageSelector] Таймаут: языки не загрузились. Используем дефолт.");
            // Фоллбэк: хардкод-список
            SetupDropdownWithFallback();
            _isInitialized = true;
        }
    }

    private void SetupDropdown()
    {
        if (languageDropdown == null) return;

        // Получаем отображаемые названия
        var names = LocalizationManager.GetAvailableLanguageNames();
        var codes = LocalizationManager.GetAvailableLanguageCodes();

        if (names.Count == 0) return;

        // Очищаем и заполняем
        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(names);

        // Выбираем текущий язык
        int currentIndex = codes.IndexOf(LocalizationManager.CurrentLanguage);
        if (currentIndex >= 0) languageDropdown.value = currentIndex;

        // Подписываемся на изменение
        languageDropdown.onValueChanged.RemoveAllListeners();
        languageDropdown.onValueChanged.AddListener(OnLanguageSelected);
    }

    private void SetupDropdownWithFallback()
    {
        // Хардкод-фоллбэк, если конфиг не загрузился
        var fallbackNames = new List<string> { "Русский", "English", "Español", "Français" };
        var fallbackCodes = new List<string> { "ru", "en", "es", "fr" };

        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(fallbackNames);

        int currentIndex = fallbackCodes.IndexOf(LocalizationManager.CurrentLanguage);
        if (currentIndex >= 0) languageDropdown.value = currentIndex;

        languageDropdown.onValueChanged.RemoveAllListeners();
        languageDropdown.onValueChanged.AddListener(OnLanguageSelected);
    }

    private void OnLanguageSelected(int index)
    {
        if (!_isInitialized) return;

        var codes = LocalizationManager.AvailableLanguages.Count > 0
            ? LocalizationManager.GetAvailableLanguageCodes()
            : new List<string> { "ru", "en", "es", "fr" };

        if (index >= 0 && index < codes.Count)
        {
            bool success = LocalizationManager.SetLanguage(codes[index]);
            if (success)
            {
                // NOTE: Здесь можно вызвать событие для обновления всего UI
                // UIManager.Instance?.RefreshAllLocalizedTexts();
                Debug.Log($"[LanguageSelector] Язык изменён на {codes[index]}");
            }
        }
    }

    // Публичный метод для принудительного обновления (если конфиг загрузился позже)
    public void Refresh()
    {
        if (LocalizationManager.AvailableLanguages.Count > 0)
            SetupDropdown();
    }*/
}