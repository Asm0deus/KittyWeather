// LocalizationBridge.cs
using UnityEngine.Localization;

/// <summary>
/// Мост между JSON-данными и таблицами Unity Localization.
/// Автоматически создаёт ссылки на локализованные строки по ID предмета.
/// </summary>
public static class LocalizationBridge
{
    // Магазин
    public static LocalizedString GetShopItemName(string id) => new LocalizedString("Shop_Items", $"{id}.name");
    public static LocalizedString GetShopItemDesc(string id) => new LocalizedString("Shop_Items", $"{id}.desc");

    // Ачивки
    public static LocalizedString GetAchievementTitle(string id) => new LocalizedString("Achievements", $"{id}.title");
    public static LocalizedString GetAchievementDesc(string id) => new LocalizedString("Achievements", $"{id}.desc");
}