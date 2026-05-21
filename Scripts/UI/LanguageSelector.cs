// version 1.0.0
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class LanguageSelector : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown _dropdown;// languageDropdown;
    private bool _isUpdating = false;
    //[SerializeField] private float initTimeout = 5f; // Макс. время ожидания инициализации

    private bool _isInitialized = false;

    private void Start()
    {
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
}