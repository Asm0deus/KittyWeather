// version 1.0.0
using UnityEngine;
using Newtonsoft.Json; // Используем Newtonsoft для надежного парсинга

public static class JsonHelper
{
    // Настраиваем параметры сериализации один раз при старте класса
    private static readonly JsonSerializerSettings settings = new JsonSerializerSettings()
    {
        NullValueHandling = NullValueHandling.Ignore,       // Игнорировать null-значения
        DefaultValueHandling = DefaultValueHandling.Include,// Включать значения по умолчанию

        // ГЛАВНОЕ ИСПРАВЛЕНИЕ: Это позволяет парсить строки "Gems" в enum CurrencyType.Gems
        Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() },

        // Дополнительно: Не чувствительность к регистру (чтобы server_data совпадало с ServerData)
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
    };

    /// <summary>
    /// Парсит JSON строку в массив объектов (например, список товаров)
    /// </summary>
    public static T[] ParseArray<T>(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            // JsonConvert автоматически обработает Enum-строки благодаря настройкам выше
            return JsonConvert.DeserializeObject<T[]>(json, settings);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[JsonHelper] Ошибка при парсинге массива: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Парсит JSON строку в одиночный объект
    /// </summary>
    public static T ParseObject<T>(string json) where T : class
    {
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            return JsonConvert.DeserializeObject<T>(json, settings);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[JsonHelper] Ошибка при парсинге объекта: {e.Message}");
            return null;
        }
    }
}