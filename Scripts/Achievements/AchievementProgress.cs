// version 1.0.0
using System;
using Newtonsoft.Json;

[Serializable]
public class AchievementProgress
{
    [JsonProperty("progress")] public float Progress { get; set; } = 0;
    [JsonProperty("unlocked")] public bool Unlocked { get; set; } = false;
    [JsonProperty("unlockedDate")] public string UnlockedDate { get; set; } = "";
    [JsonProperty("rarity")] public float Rarity { get; set; } = 0.0f;

    [JsonIgnore]
    public DateTime UnlockedDateTime
    {
        get => string.IsNullOrEmpty(UnlockedDate) ? DateTime.MinValue : DateTime.Parse(UnlockedDate);
        set => UnlockedDate = value.ToString("o");
    }
}