// version 1.0.0
using System;
using System.Globalization;

public static class AchievementDateFormatter
{
    //  эшируем CultureInfo, чтобы не создавать каждый раз
    private static readonly CultureInfo _ru = new("ru-RU");
    private static readonly CultureInfo _en = new("en-US");
    private static readonly CultureInfo _es = new("es-ES");
    private static readonly CultureInfo _fr = new("fr-FR");

    /// <summary>
    /// ‘орматирует дату ачивки: "28 дек." (RU) / "Dec 28" (EN)
    /// </summary>
    public static string FormatDate(string isoDate, string languageCode)
    {
        if (string.IsNullOrEmpty(isoDate)) return "Ч";

        try
        {
            // ѕарсим ISO 8601: "2024-05-19T10:30:00Z"
            var date = DateTime.Parse(isoDate);
            var culture = GetCulture(languageCode);

            // ‘ормат: "28 дек." / "Dec 28"
            // MMM = сокращЄнное название мес€ца, d = день без ведущего нул€
            return date.ToString("d MMM", culture);
        }
        catch
        {
            // ‘оллбэк: возвращаем как есть
            return isoDate;
        }
    }

    /// <summary>
    /// ‘орматирует врем€ ачивки: "13:25" (24h) / "1:25 PM" (12h)
    /// </summary>
    public static string FormatTime(string isoTime, string languageCode)
    {
        if (string.IsNullOrEmpty(isoTime)) return "Ч";

        try
        {
            var time = DateTime.Parse(isoTime);
            var culture = GetCulture(languageCode);

            // RU/ES/FR: 24-часовой формат "13:25"
            // EN: 12-часовой с AM/PM "1:25 PM"
            string format = languageCode == "en" ? "h:mm tt" : "HH:mm";
            return time.ToString(format, culture);
        }
        catch
        {
            return isoTime;
        }
    }

    private static CultureInfo GetCulture(string code)
    {
        return code switch
        {
            "ru" => _ru,
            "en" => _en,
            "es" => _es,
            "fr" => _fr,
            _ => _en // fallback
        };
    }
}