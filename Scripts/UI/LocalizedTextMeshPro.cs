using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// Компонент для автоматической локализации TextMeshProUGUI.
/// В инспекторе задаёшь TableReference и EntryReference — компонент сам обновляется при смене языка.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedTextMeshPro : MonoBehaviour, ILocalizedText
{
    [Header("Localization Settings")]
    [SerializeField] private string tableReference = "UI_Texts"; // Имя таблицы в Unity Localization
    [SerializeField] private string entryReference = ""; // Ключ строки, например "shop.buy_button"

    [Header("Опции")]
    [Tooltip("Обновлять текст сразу при старте?")]
    [SerializeField] private bool refreshOnStart = true;
    [Tooltip("Подписываться на автообновление при смене языка?")]
    [SerializeField] private bool autoUpdate = true;

    private TextMeshProUGUI _tmp;
    private LocalizedString _localizedRef;
    private bool _isRegistered = false;

    void Awake()
    {
        _tmp = GetComponent<TextMeshProUGUI>();
        InitializeLocalizedString();
    }

    void Start()
    {
        if (refreshOnStart) RefreshLocalization();
        Register();
    }

    private void InitializeLocalizedString()
    {
        if (!string.IsNullOrEmpty(tableReference) && !string.IsNullOrEmpty(entryReference))
        {
            _localizedRef = new LocalizedString(tableReference, entryReference);

            if (autoUpdate && _localizedRef != null)
            {
                // Подписываемся на автообновление при смене языка
                _localizedRef.StringChanged += UpdateText;
            }
        }
    }

    private void UpdateText(string newText)
    {
        if (_tmp != null) _tmp.text = newText;
    }

    void OnEnable() => Register();
    void OnDisable() => Unregister();

    private void Register()
    {
        if (!_isRegistered && UIManager.Instance != null)
        {
            UIManager.Register(this);
            _isRegistered = true;
        }
    }

    private void Unregister()
    {
        if (_isRegistered && UIManager.Instance != null)
        {
            UIManager.Unregister(this);
            _isRegistered = false;
        }

        // Отписываемся от события, чтобы не было утечек
        if (_localizedRef != null && autoUpdate)
        {
            _localizedRef.StringChanged -= UpdateText;
        }
    }

    public void RefreshLocalization()
    {
        if (_tmp != null && _localizedRef != null)
        {
            _tmp.text = _localizedRef.GetLocalizedString();
        }
    }

    // Публичный метод для смены ключа в рантайме
    public void SetEntryReference(string newTable, string newKey)
    {
        if (_localizedRef != null && autoUpdate)
            _localizedRef.StringChanged -= UpdateText;

        tableReference = newTable;
        entryReference = newKey;
        InitializeLocalizedString();
        RefreshLocalization();
    }

    // Публичный метод для ручной установки текста (если нужно)
    public void SetLocalizedText(string table, string key) => SetEntryReference(table, key);
}