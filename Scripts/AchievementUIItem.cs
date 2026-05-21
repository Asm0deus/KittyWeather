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

    // Кеш спрайтов (если нужно грузить динамически)
    // public Sprite[] icons; 

    //public void SetData(AchievementManager.AchievementData data)
    //{
    //    nameText.text = data.name;
    //    descriptionText.text = data.description;

    //    if (data.isUnlocked == true)
    //    {
    //        lockedGroup.SetActive(false);
    //        unlockedGroup.SetActive(true);

    //        rarityText.text = $"{data.rarity * 100}%";
    //        dateTimeText.text = data.date;
    //        timeText.text = data.time;

    //        // Тут логика установки иконки
    //        // achievementIcon.sprite = LoadSprite(data.iconSpriteName);
    //    }
    //    else
    //    {
    //        lockedGroup.SetActive(true);
    //        unlockedGroup.SetActive(false);

    //        progressText.text = $"{data.progress}%";
    //    }
    //}
}