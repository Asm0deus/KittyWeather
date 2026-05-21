// version 1.0.0
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
    public string category;
    public string iconSpriteName;

    // Вычисляемые свойства для обратной совместимости с UI
    public int price;
    public CurrencyType currencyType;
    public bool isDiscounted;
    public int oldPrice;

    [JsonProperty("enabled")] public bool enabled = true;

    [NonSerialized] public bool isPurchased;

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