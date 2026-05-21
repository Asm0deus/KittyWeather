using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;


public class ShopManager : MonoBehaviour
{
    [Serializable]
    public class SaleConfig
    {
        public bool enabled;
        public int discountPercent;
        public string startTimestamp;
        public string endTimestamp;
        public string[] applicableCategories;
        public string[] excludeItemIds;
    }

    [Serializable]
    public class ShopDataWithSale
    {
        public string version;
        public SaleConfig saleConfig;
        public ShopItemData[] items;
    }

    public static ShopManager Instance { get; private set; } // Добавлено для доступа из ShopItemData

    [Header("Загрузка")]
    [SerializeField] private string serverUrl = ""; // Пустое для теста локально https://cdn.jsdelivr.net/gh/Asm0deus/KittyWeather/shop_default.json
    [SerializeField] private string localFileName = "shop_default.json";
    [Header("Ссылки")]
    [SerializeField] private CurrencyManager currencyManager;

    public Action<List<ShopItemData>> OnCategoryLoaded;
    public event Action OnItemPurchased;
    public event Action<ShopItemData> OnInventoryItemAdded; // Для будущего инвентаря

    private List<ShopItemData> allItems = new();
    private HashSet<string> purchasedIds = new();
    private const string CACHE_FILE = "shop_cache.json";

    public SaleConfig CurrentSale { get; private set; }
    public float SaleDiscountPercent => CurrentSale?.enabled == true ? CurrentSale.discountPercent : 0;


    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; // Инициализация синглтона
    }

    private async void Start()
    {
        LoadPurchasedItems();
        await GameBootstrap.WaitForReadyAsync();

        // Теперь EconomyConfig точно загружен, курсы доступны
        StartCoroutine(FetchData());
        //StartCoroutine(FetchData());
    }

    public bool IsItemOnSale(ShopItemData item)
    {
        if (CurrentSale == null || !CurrentSale.enabled) return false;

        // Проверка времени
        var now = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(CurrentSale.startTimestamp))
        {
            var start = DateTime.Parse(CurrentSale.startTimestamp);
            if (now < start) return false;
        }
        if (!string.IsNullOrEmpty(CurrentSale.endTimestamp))
        {
            var end = DateTime.Parse(CurrentSale.endTimestamp);
            if (now > end) return false;
        }

        // Проверка категории
        if (CurrentSale.applicableCategories?.Length > 0 &&
            !CurrentSale.applicableCategories.Contains(item.category))
            return false;

        // Проверка исключений
        if (CurrentSale.excludeItemIds?.Contains(item.id) == true)
            return false;

        return true;
    }

    IEnumerator FetchData()
    {
        string json = null;

        // 1. Сервер
        if (!string.IsNullOrEmpty(serverUrl))
        {
            using (var req = UnityWebRequest.Get(serverUrl))
            {
                req.timeout = 5;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    json = req.downloadHandler.text;
                }
            }
        }

        // 2. Кэш (если сервер недоступен или пуст)
        if (string.IsNullOrEmpty(json))
        {
            string cachePath = Path.Combine(Application.persistentDataPath, CACHE_FILE);
            if (File.Exists(cachePath)) json = File.ReadAllText(cachePath);
        }

        // 3. StreamingAssets (фоллбэк)
        if (string.IsNullOrEmpty(json))
        {
            string defaultPath = Path.Combine(Application.streamingAssetsPath, localFileName);
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var req = UnityWebRequest.Get(defaultPath))
                {
                    yield return req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success) json = req.downloadHandler.text;
                }
            }
            else if (File.Exists(defaultPath))
            {
                json = File.ReadAllText(defaultPath);
            }
        }

        if (!string.IsNullOrEmpty(json))
        {
            if (ParseAndApply(json)) SaveToCache(json);
        }
    }

    private bool ParseAndApply(string json)
    {
        try
        {
            json = json.Trim('\ufeff', '\u200b', ' ', '\n', '\r');

            // Пробуем парсить с обёрткой (если есть saleConfig)
            var wrapper = JsonHelper.ParseObject<ShopDataWithSale>(json);

            ShopItemData[] data = null;

            if (wrapper?.items != null)
            {
                data = wrapper.items;
                // Применяем акцию, если она есть в конфиге
                if (wrapper.saleConfig != null)
                {
                    CurrentSale = wrapper.saleConfig;
                    Debug.Log($"[Shop] Акция загружена: {CurrentSale.discountPercent}% до {CurrentSale.endTimestamp}");
                }
            }
            else
            {
                // Фоллбэк: парсим как простой массив (старый формат)
                data = JsonHelper.ParseArray<ShopItemData>(json);
                CurrentSale = null;
            }

            if (data == null) return false;

            allItems = new List<ShopItemData>();

            foreach (var item in data)
            {
                // Фильтрация отключенных товаров
                if (!item.enabled) continue;

                item.isPurchased = purchasedIds.Contains(item.id);
                allItems.Add(item);
            }

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Shop] Ошибка парсинга: {e.Message}");
            return false;
        }
    }

    private void SaveToCache(string json)
    {
        try
        {
            File.WriteAllText(Path.Combine(Application.persistentDataPath, CACHE_FILE), json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Shop] Не удалось сохранить кэш: {e.Message}");
        }
    }

    private void LoadPurchasedItems()
    {
        string saved = PlayerPrefs.GetString("PurchasedItems", "");
        if (!string.IsNullOrEmpty(saved))
            foreach (var id in saved.Split(','))
                if (!string.IsNullOrWhiteSpace(id)) purchasedIds.Add(id.Trim());
    }

    public bool PurchaseItem(ShopItemData item)
    {
        if (item == null || item.isPurchased || currencyManager == null) return false;

        bool success = currencyManager.TrySpend(item.currencyType, item.price);
        if (success)
        {
            item.isPurchased = true;
            if (!purchasedIds.Contains(item.id))
            {
                purchasedIds.Add(item.id);
                PlayerPrefs.SetString("PurchasedItems", string.Join(",", purchasedIds));
                PlayerPrefs.Save();
            }
            OnItemPurchased?.Invoke(); // Сообщаем UI, что купили
            OnInventoryItemAdded?.Invoke(item); // Событие для инвентаря
            return true;
        }
        return false; // Не хватило денег
    }

    public List<ShopItemData> GetCategoryItems(string category, bool showPurchased = false)
    {
        var filtered = allItems.Where(i => i.category == category).ToList();
        if (!showPurchased)
            filtered = filtered.Where(i => !i.isPurchased).ToList();
        return filtered;
    }

    // Метод для сохранения (отдает копию списка)
    public List<string> GetPurchasedIds()
    {
        return new List<string>(purchasedIds);
    }

    // Метод для загрузки (принимает список из облака)
    public void RestorePurchases(List<string> purchasedIdsFromCloud)
    {
        purchasedIds.Clear();
        foreach (var id in purchasedIdsFromCloud)
        {
            purchasedIds.Add(id);
        }

        // Обновляем статусы у всех загруженных предметов
        foreach (var item in allItems)
        {
            item.isPurchased = purchasedIds.Contains(item.id);
        }

        Debug.Log($"[Shop] Восстановлено {purchasedIds.Count} покупок из облака.");
    }
}