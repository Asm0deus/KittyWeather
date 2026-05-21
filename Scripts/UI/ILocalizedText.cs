// ILocalizedText.cs
using UnityEngine;

/// <summary>
/// Интерфейс для UI-компонентов, которые нужно обновлять при смене языка.
/// </summary>
public interface ILocalizedText
{
    /// <summary>
    /// Перезагрузить текст в соответствии с текущим языком.
    /// </summary>
    void RefreshLocalization();
}