using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class AchievementUIItemJson : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;

    [Header("Locked State")]
    public GameObject lockedGroup;
    public TextMeshProUGUI progressText;

    [Header("Unlocked State")]
    public GameObject unlockedGroup;
    public TextMeshProUGUI rarityText;
    public TextMeshProUGUI dateTimeText;
    public TextMeshProUGUI timeText;
    public Image achievementIcon;

    private RuntimeAchievement _cachedData;

    /// <summary>
    /// Принимает RuntimeAchievement из трекера
    /// </summary>
    public void SetData(RuntimeAchievement runtimeAch)
    {
        if (runtimeAch == null)
        {
            Debug.LogWarning("[AchievementUI] Получен null данные");
            return;
        }

        _cachedData = runtimeAch;
        RefreshUI();
    }
    private void RefreshUI()
    {
        if (_cachedData == null) return;

        // Текст берётся из Unity Localization по ID ачивки
        // Ключи в таблице: {id}.title и {id}.desc
        var titleRef = new LocalizedString("Achievements", $"{_cachedData.Id}.title");
        var descRef = new LocalizedString("Achievements", $"{_cachedData.Id}.desc");

        nameText.text = titleRef.GetLocalizedString();
        descriptionText.text = descRef.GetLocalizedString();

        bool isUnlocked = _cachedData.IsUnlocked;
        lockedGroup.SetActive(!isUnlocked);
        unlockedGroup.SetActive(isUnlocked);

        if (isUnlocked)
        {
            rarityText.text = $"{_cachedData.ProgressData.Rarity * 100:F1}%";

            // Форматируем дату/время
            string lang = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale.Identifier.Code;
            string dateStr = _cachedData.UnlockedDateTime != DateTime.MinValue
                ? AchievementDateFormatter.FormatDate(_cachedData.UnlockedDateTime.ToString("o"), lang)
                : "—";
            string timeStr = _cachedData.UnlockedDateTime != DateTime.MinValue
                ? AchievementDateFormatter.FormatTime(_cachedData.UnlockedDateTime.ToString("o"), lang)
                : "—";

            dateTimeText.text = dateStr;
            timeText.text = timeStr;

            if (!string.IsNullOrEmpty(_cachedData.Definition.IconName) && achievementIcon != null)
                achievementIcon.sprite = IconLoader.LoadFromResources(_cachedData.Definition.IconName);
        }
        else
        {
            // Прогресс в процентах
            float target = _cachedData.Definition.TargetValue;
            float current = _cachedData.ProgressData.Progress;
            float percent = target > 0 ? (current / target * 100) : 0;
            progressText.text = $"{Mathf.Min(percent, 100):F0}%";
        }
    }

    // Автообновление при смене языка
    void OnEnable() => UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged += _ => RefreshUI();
    void OnDisable() => UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged -= _ => RefreshUI();
}