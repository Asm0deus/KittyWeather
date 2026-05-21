// version 1.0.0
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // Все зарегистрированные локализуемые компоненты
    private readonly HashSet<ILocalizedText> _localizedComponents = new();

    // Событие для внешних подписчиков (опционально)
    public static event System.Action OnLanguageChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Зарегистрировать компонент для авто-обновления при смене языка.
    /// Вызывать в Start()/OnEnable() компонентов.
    /// </summary>
    public static void Register(ILocalizedText component)
    {
        if (Instance != null && component != null)
            Instance._localizedComponents.Add(component);
    }

    /// <summary>
    /// Отменить регистрацию (вызывать в OnDisable()/OnDestroy()).
    /// </summary>
    public static void Unregister(ILocalizedText component)
    {
        if (Instance != null && component != null)
            Instance._localizedComponents.Remove(component);
    }

    /// <summary>
    /// Принудительно обновить все зарегистрированные тексты.
    /// </summary>
    public static void RefreshAllLocalizedTexts()
    {
        if (Instance == null) return;

        foreach (var comp in Instance._localizedComponents.ToList())
        {
            // Проверяем, не уничтожен ли объект
            if (comp is MonoBehaviour mb && mb == null) continue;
            comp?.RefreshLocalization();
        }

        // Уведомляем внешних подписчиков
        OnLanguageChanged?.Invoke();

        Debug.Log($"[UIManager] Обновлено {Instance._localizedComponents.Count} локализуемых компонентов");
    }
}