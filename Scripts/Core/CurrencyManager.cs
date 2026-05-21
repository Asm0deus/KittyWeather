// version 1.0.0
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public enum CurrencyType { Coins, Gems }

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    // Событие для обновления UI
    public event Action<CurrencyType> OnBalanceChanged;

    // Внутреннее хранилище баланса
    private readonly Dictionary<CurrencyType, int> _balances = new()
    {
        { CurrencyType.Coins, 500 },
        { CurrencyType.Gems, 0 }
    };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadBalances();

        // Подписываемся на загрузку конфига, чтобы сразу применить курсы
        EconomyConfigLoader.OnConfigLoaded += _ => ForceRefresh();
    }

    void OnDestroy()
    {
        EconomyConfigLoader.OnConfigLoaded -= _ => ForceRefresh();
    }

    // === ГЕТТЕРЫ ===
    public int GetBalance(CurrencyType type) => _balances.TryGetValue(type, out int v) ? v : 0;
    public int Coins => GetBalance(CurrencyType.Coins);
    public int Gems => GetBalance(CurrencyType.Gems);

    public void ForceRefresh()
    {
        OnBalanceChanged?.Invoke(CurrencyType.Coins);
        OnBalanceChanged?.Invoke(CurrencyType.Gems);
    }

    // === ТРАТА ===
    public bool TrySpend(CurrencyType type, int amount)
    {
        if (amount <= 0) return true;
        if (_balances.TryGetValue(type, out int balance) && balance >= amount)
        {
            _balances[type] -= amount;
            SaveLocal();
            OnBalanceChanged?.Invoke(type);
            return true;
        }
        return false;
    }

    // === КОНВЕРТАЦИЯ (использует только курс из CDN) ===
    public bool ConvertGemsToCoins(int gemsAmount)
    {
        if (gemsAmount <= 0) return false;

        int coinsPerGem = EconomyConfigLoader.GetGemsToCoinsRate(gemsAmount);
        int coinsToReceive = gemsAmount * coinsPerGem;

        if (TrySpend(CurrencyType.Gems, gemsAmount))
        {
            AddCurrency(CurrencyType.Coins, coinsToReceive);
            Debug.Log($"[Economy] Конвертация: {gemsAmount}💎 → {coinsToReceive}🐟 (курс: x{coinsPerGem})");
            return true;
        }
        return false;
    }

    // === ПОПОЛНЕНИЕ ===
    public void AddCurrency(CurrencyType type, int amount)
    {
        if (amount <= 0) return;
        if (!_balances.ContainsKey(type)) _balances[type] = 0;
        _balances[type] += amount;
        SaveLocal();
        OnBalanceChanged?.Invoke(type);
    }

    // === СОХРАНЕНИЕ / ЗАГРУЗКА ===
    void SaveLocal()
    {
        PlayerPrefs.SetInt("CoinsBal", Coins);
        PlayerPrefs.SetInt("GemsBal", Gems);
        PlayerPrefs.SetInt("LastLocalSaveTime", (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        PlayerPrefs.Save();
    }

    void LoadBalances()
    {
        _balances[CurrencyType.Coins] = PlayerPrefs.GetInt("CoinsBal", 500);
        _balances[CurrencyType.Gems] = PlayerPrefs.GetInt("GemsBal", 0);
        ForceRefresh();
    }

    // Сохранение в облако
    public async Task SaveToCloud(string userId, List<string> purchasedItems)
    {
        if (FirestoreManager.Instance == null) return;

        var data = new PlayerData
        {
            userId = userId,
            coins = Coins,
            gems = Gems,
            lastSyncTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            purchasedItems = purchasedItems ?? new List<string>()
            // achievements сохраняется отдельно через AchievementTracker
        };

        await FirestoreManager.Instance.SaveAsync(data);
    }

    // Синхронизация из облака
    public async void SyncFromCloud(string userId)
    {
        if (FirestoreManager.Instance == null) return;
        var cloudData = await FirestoreManager.Instance.LoadAsync();

        long localTime = PlayerPrefs.GetInt("LastLocalSaveTime", 0);

        // Сервер побеждает, если он новее. Иначе пушим локальный прогресс.
        if (cloudData.lastSyncTimestamp > localTime)
        {
            _balances[CurrencyType.Coins] = cloudData.coins;
            _balances[CurrencyType.Gems] = cloudData.gems;
            SaveLocal();
            ForceRefresh();
            Debug.Log("[Sync] Загружено из облака (новее).");
        }
        else
        {
            Debug.Log("[Sync] Локальные данные новее. Отправляю в облако...");
            await SaveToCloud(userId, ShopManager.Instance.GetPurchasedIds());
        }
    }
}