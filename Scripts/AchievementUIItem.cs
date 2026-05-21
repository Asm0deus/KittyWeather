// version 1.0.0
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// УСТАРЕВШИЙ КЛАСС. Не использовать в новом коде.
/// Для загрузки ачивок AchievementUIItemJson.cs.
/// Этот класс оставлен только для обратной совместимости и будет удалён в следующей версии.
/// </summary>
public class AchievementUIItem : MonoBehaviour
{
    [Header("Общие элементы")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;

    [Header("Элементы Locked")]
    public GameObject lockedGroup;
    public TextMeshProUGUI progressText;
    public Image lockIcon;

    [Header("Элементы Unlocked")]
    public GameObject unlockedGroup;
    public TextMeshProUGUI rarityText;
    public TextMeshProUGUI dateTimeText;
    public TextMeshProUGUI timeText;
    public Image achievementIcon;
}