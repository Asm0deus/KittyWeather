// version 1.0.0
using System;
using Newtonsoft.Json;

[Serializable]
public class AchievementDefinition
{
    [JsonProperty("id")] public string Id { get; set; }
    [JsonProperty("iconName")] public string IconName { get; set; }
    [JsonProperty("conditionType")] public string ConditionType { get; set; } // "daily_login", "collect_items", etc.
    [JsonProperty("targetValue")] public float TargetValue { get; set; }
    [JsonProperty("rewardCoins")] public int RewardCoins { get; set; }
}

[Serializable]
public class AchievementListWrapper
{
    [JsonProperty("definitions")]
    public AchievementDefinition[] Definitions { get; set; }
}