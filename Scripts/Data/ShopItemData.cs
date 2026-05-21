using Newtonsoft.Json;
using System;
using UnityEngine;

[Serializable]
public class LocalizedStringUI
{
    public string ru;
    public string en;
    public string es;
    public string fr;

    //[JsonIgnore]
    //public string Value => LocalizationManager.CurrentLanguage switch
    //{
    //    "ru" => ru,
    //    "en" => en,
    //    "es" => es,
    //    "fr" => fr,
    //    _ => en // fallback
    //};

    // Хелпер-метод для получения значения по коду языка (если нужно)
    public string GetValueForLanguage(string langCode)
    {
        return langCode switch
        {
            "ru" => ru,
            "en" => en,
            "es" => es,
            "fr" => fr,
            _ => en
        };
    }
}

[Serializable]
public class ShopItemData
{
    public string id;
    //public LocalizedStringUI name;
    //public LocalizedStringUI description;
    public string category;
    public string iconSpriteName;

    // Вычисляемые свойства для обратной совместимости с UI
    public int price;
    public CurrencyType currencyType;
    public bool isDiscounted;
    public int oldPrice;

    [JsonProperty("enabled")] public bool enabled = true;

    [NonSerialized] public bool isPurchased;

    //[JsonIgnore] public string DisplayName => name?.Value ?? "Unknown";
    //[JsonIgnore] public string DisplayDescription => description?.Value ?? "";

    //[JsonIgnore]
    public int GetFinalPrice(float discountPercent = 0)
    {
        if (discountPercent > 0 && !isDiscounted)
            return Mathf.RoundToInt(price * (1f - discountPercent / 100f));
        return price;
    }
}


[Serializable]
public class ShopDataWrapper
{
    public ShopItemData[] items;
}