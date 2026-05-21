// version 1.0.0
using System.Collections.Generic;

[System.Serializable]
public class ShopConfig
{
    public Dictionary<int, int> gemsToCoins; // { 100: 10000, 500: 50000 }
    public List<IapItem> iapItems;
}

public class IapItem
{
    public string productId;
    public int gemsAmount;
    public float localCurrencyCost; // Для отображения в UI
}