using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class CurrencyPack
{
    public string id;
    public string displayName;
    public CurrencyType currency;
    public int amount;
    public float usdPrice;
}

public class CurrencyPackManager : MonoBehaviour
{
    public static CurrencyPackManager Instance { get; private set; }
    public event Action<CurrencyPack> OnPackPurchased;

    [Header("Тестовые паки (фоллбэк)")]
    [SerializeField]
    private CurrencyPack[] testPacks = new CurrencyPack[]
    {
        new CurrencyPack { id = "coins_100", displayName = "+100", currency = CurrencyType.Coins, amount = 100 },
        new CurrencyPack { id = "coins_500", displayName = "+500", currency = CurrencyType.Coins, amount = 500 },
        new CurrencyPack { id = "gems_50",  displayName = "+50",  currency = CurrencyType.Gems,  amount = 50, usdPrice = 0.99f },
        new CurrencyPack { id = "gems_250", displayName = "+250", currency = CurrencyType.Gems,  amount = 250, usdPrice = 4.99f }
    };

    private List<CurrencyPack> _activePacks = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        LoadPacks();
        EconomyConfigLoader.OnConfigLoaded += ApplyPacksFromConfig;
    }

    void OnDestroy() => EconomyConfigLoader.OnConfigLoaded -= ApplyPacksFromConfig;

    private void LoadPacks() => _activePacks = new List<CurrencyPack>(testPacks);

    private void ApplyPacksFromConfig(EconomyConfig config)
    {
        //if (config?.dailyBonus?.gemPacks != null)
        //{
        //    _activePacks.Clear();
        //    foreach (var gp in config.dailyBonus.gemPacks)
        //    {
        //        _activePacks.Add(new CurrencyPack
        //        {
        //            id = gp.id,
        //            displayName = $"+{gp.gemAmount} 💎",
        //            currency = CurrencyType.Gems,
        //            amount = gp.gemAmount,
        //            usdPrice = gp.usdPrice
        //        });
        //    }
        //    // Оставляем коины из дефолта, если их нет в конфиге
        //    _activePacks.AddRange(testPacks.Where(p => p.currency == CurrencyType.Coins));
        //    Debug.Log($"[PackManager] Загружено {_activePacks.Count} паков из конфига.");
        //}
    }

    public List<CurrencyPack> GetPacks() => _activePacks;

    public void RequestPurchase(string packId)
    {
        var pack = _activePacks.Find(p => p.id == packId);
        if (pack == null) { Debug.LogError($"[PackManager] Пак {packId} не найден!"); return; }

        var flow = FindFirstObjectByType<PurchaseFlowController>();

        // Теперь свойство IsInitialized существует и публично
        if (flow != null && flow.IsInitialized)
            StartCoroutine(StartPurchaseCoroutine(pack));
        else
            Debug.LogError("[PackManager] IAP не инициализирован или PurchaseFlowController отсутствует.");
    }

    private System.Collections.IEnumerator StartPurchaseCoroutine(CurrencyPack pack)
    {
        yield return new WaitForSeconds(0.1f);
        ProcessSuccessfulPurchase(pack);
    }

    public void ProcessSuccessfulPurchase(CurrencyPack pack)
    {
        CurrencyManager.Instance.AddCurrency(pack.currency, pack.amount);
        OnPackPurchased?.Invoke(pack);
        Debug.Log($"[Economy] Начислено {pack.amount} {pack.currency} за пак {pack.id}");
    }
}