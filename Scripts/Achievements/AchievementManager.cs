// version 1.0.0
using System;
using UnityEngine;

/// <summary>
/// УСТАРЕВШИЙ КЛАСС. Не использовать в новом коде.
/// Для загрузки ачивок RemoteAchievementLoader.cs (CDN + JSON + кэш).
/// Этот класс оставлен только для обратной совместимости и будет удалён в следующей версии.
/// </summary>

[Obsolete("Use RemoteAchievementLoader instead. CSV-based loading is deprecated.", false)]
public class AchievementManager : MonoBehaviour
{
    void Awake()
    {
        Debug.LogWarning("[AchievementManager] ⚠️ Этот класс устарел! Используйте RemoteAchievementLoader для загрузки ачивок через CDN.");
        Destroy(gameObject); // Автоматически удаляем, чтобы не мешал
    }

    // Пустые методы-заглушки, чтобы не ломать ссылки в инспекторе при переходе
    public void LoadAndBuildList() { }
    public void BuildUI() { }
}