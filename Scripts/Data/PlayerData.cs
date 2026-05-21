using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class PlayerData
{
    public string userId { get; set; } = "";
    public int coins { get; set; } = 500;
    public int gems { get; set; } = 0;
    public long lastSyncTimestamp { get; set; } = 0; // Unix time для оффлайн-merge
    public List<string> purchasedItems { get; set; } = new(); // Покупки
    public Dictionary<string, AchievementProgress> achievements { get; set; } = new(); // Прогресс ачивок

    public Dictionary<string, object> ToFirestoreMap()
    {
        return new Dictionary<string, object>
    {
        { "userId", userId },
        { "coins", coins },
        { "gems", gems },
        { "lastSyncTimestamp", lastSyncTimestamp },
        { "purchasedItems", purchasedItems ?? new List<string>() },
        //{ "unlockedAchievements", unlockedAchievements ?? new List<string>() }
        // achievements можно добавить отдельно, если нужно
    };
    }

    // Статический метод для обратной конвертации (если читаешь из Firestore)
    public static PlayerData FromFirestoreMap(Dictionary<string, object> map)
    {
        if (map == null) return new PlayerData();

        var data = new PlayerData();

        if (map.TryGetValue("userId", out var uid) && uid is string) data.userId = uid.ToString();
        if (map.TryGetValue("coins", out var c) && c is long) data.coins = (int)(long)c; // Firestore хранит int как long
        if (map.TryGetValue("gems", out var g) && g is long) data.gems = (int)(long)g;
        if (map.TryGetValue("lastSyncTimestamp", out var ts) && ts is long) data.lastSyncTimestamp = (long)ts;
        if (map.TryGetValue("purchasedItems", out var pi) && pi is List<object> pil)
            data.purchasedItems = pil.Cast<string>().ToList();
        /*if (map.TryGetValue("unlockedAchievements", out var ua) && ua is List<object> ual)
            data.unlockedAchievements = ual.Cast<string>().ToList();*/

        return data;
    }
}