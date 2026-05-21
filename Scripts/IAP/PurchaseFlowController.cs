// PurchaseFlowController.cs — ЕДИНЫЙ КОНТРОЛЕР: Попапы + IAP v5
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Localization;

public class PurchaseFlowController : MonoBehaviour
{
    public static PurchaseFlowController Instance { get; private set; }

    // IAP v5 статус (нужен для CurrencyPackManager)
    public bool IsInitialized => _isInitialized && _storeController != null;

    [Header("Ссылки на системы")]
    [SerializeField] private CurrencyManager currencyManager;
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private CurrencyPackManager packManager;
    [SerializeField] private CurrencyShopController currencyShop;

    [Header("Popup: Подтверждение")]
    [SerializeField] private GameObject confirmationPopup;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI confirmItemNameText;
    [SerializeField] private TextMeshProUGUI confirmItemDescriptionText;
    [SerializeField] private TextMeshProUGUI confirmItemPriceText;
    [SerializeField] private Button confirmBuyBtn;
    [SerializeField] private Button confirmCancelBtn;

    [SerializeField] private GameObject lightingPremium;
    [SerializeField] private GameObject iconPremium;
    [SerializeField] private GameObject iconBase;
    [SerializeField] private Ricimi.Gradient gradientBase;
    [SerializeField] private Ricimi.Gradient gradientOutline;
    [SerializeField] private Color basicBtnColorOne, basicBtnColorSecond, basicBtnOutlineOne, basicBtnOutlineSecond;
    [SerializeField] private Color premiumBtnColorOne, premiumBtnColorSecond, premiumBtnOutlineOne, premiumBtnOutlineSecond;

    [Header("Popup: Недостаточно средств")]
    [SerializeField] private GameObject insufficientPopup;
    [SerializeField] private TextMeshProUGUI insufficientText;
    [SerializeField] private Button insufficientGoToShopBtn;
    [SerializeField] private Button insufficientCancelBtn;

    // IAP v5 поля
    private StoreController _storeController;
    private bool _isInitialized = false;
    private readonly Dictionary<string, CurrencyPack> _packsById = new();
    private TaskCompletionSource<bool> _purchaseTcs;

    // Локальное состояние
    private ShopItemData _pendingShopItem;
    private string _pendingPackId;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Кэшируем паки для IAP
        if (packManager != null)
        {
            foreach (var pack in packManager.GetPacks())
            {
                if (!string.IsNullOrEmpty(pack.id))
                    _packsById[pack.id] = pack;
            }
        }

        // Привязываем кнопки попапов
        if (confirmBuyBtn != null) confirmBuyBtn.onClick.AddListener(OnConfirmBuy);
        if (confirmCancelBtn != null) confirmCancelBtn.onClick.AddListener(CloseAllPopups);
        if (insufficientGoToShopBtn != null) insufficientGoToShopBtn.onClick.AddListener(OpenCurrencyShop);
        if (insufficientCancelBtn != null) insufficientCancelBtn.onClick.AddListener(CloseAllPopups);

        CloseAllPopups();
    }

    void Start()
    {
        // Инициализируем IAP асинхронно
        _ = InitializeIAPAsync();
    }

    // === IAP v5 ИНИЦИАЛИЗАЦИЯ ===
    private async Task<bool> InitializeIAPAsync()
    {
        if (_isInitialized) return true;

        try
        {
            _storeController = UnityIAPServices.StoreController();

            _storeController.OnProductsFetched += OnProductsFetched;
            _storeController.OnPurchasePending += OnPurchasePending;
            _storeController.OnPurchaseFailed += OnPurchaseFailed;
            _storeController.OnPurchasesFetched += OnPurchasesFetched;

            await _storeController.Connect();

            var productsToFetch = new List<ProductDefinition>();
            foreach (var packId in _packsById.Keys)
                productsToFetch.Add(new ProductDefinition(packId, ProductType.Consumable));

            if (productsToFetch.Count > 0)
                _storeController.FetchProducts(productsToFetch);
            else
                _isInitialized = true;

            return _isInitialized;
        }
        catch (Exception e)
        {
            Debug.LogError($"[IAP] Init failed: {e.Message}");
            return false;
        }
    }

    private void OnProductsFetched(List<Product> products)
    {
        Debug.Log($"[IAP] Fetched {products.Count} products.");
        _isInitialized = true;
        _storeController?.FetchPurchases();
    }

    private void OnPurchasesFetched(Orders orders)
    {
        foreach (var order in orders.ConfirmedOrders)
        {
            foreach (var item in order.CartOrdered.Items())
            {
                ValidateAndGrant(item.Product.definition.id, order.Info.Receipt);
            }
        }
    }

    private void OnPurchasePending(PendingOrder pendingOrder)
    {
        foreach (var item in pendingOrder.CartOrdered.Items())
        {
            ValidateAndGrant(item.Product.definition.id, pendingOrder.Info.Receipt);
        }
        _storeController?.ConfirmPurchase(pendingOrder);
    }

    private void OnPurchaseFailed(FailedOrder failedOrder)
    {
        foreach (var item in failedOrder.CartOrdered.Items())
            Debug.LogWarning($"[IAP] Failed: {item.Product.definition.id}");
        _purchaseTcs?.TrySetResult(false);
    }

    // === ПУБЛИЧНЫЕ МЕТОДЫ ДЛЯ ДРУГИХ СКРИПТОВ ===

    // Для CurrencyShopController (синхронный вызов)
    public void BuyProduct(string productId)
    {
        _ = BuyCurrencyPackAsync(productId);
    }

    // Для внутренних нужд (асинхронный)
    public async Task<bool> BuyCurrencyPackAsync(string packId)
    {
        if (!IsInitialized || _storeController == null)
        {
            Debug.LogError("[IAP] Not initialized");
            return false;
        }

        if (!_packsById.TryGetValue(packId, out var pack))
        {
            Debug.LogError($"[IAP] Pack {packId} not found");
            return false;
        }

        var products = _storeController.GetProducts();
        var product = products.FirstOrDefault(p => p.definition.id == packId);
        if (product == null)
        {
            Debug.LogError($"[IAP] Product {packId} not found in store");
            return false;
        }

        _purchaseTcs = new TaskCompletionSource<bool>();
        var cart = new Cart(new CartItem(product, 1));
        _storeController.Purchase(cart);

        return await _purchaseTcs.Task;
    }

    // === ПОКУПКА ПРЕДМЕТА МАГАЗИНА (с попапами) ===

    public void InitiatePurchase(ShopItemData item)
    {
        _pendingShopItem = item;
        if (currencyManager == null || shopManager == null)
        {
            Debug.LogError("[PurchaseFlow] currencyManager или shopManager не назначены!");
            return;
        }

        int balance = currencyManager.GetBalance(item.currencyType);

        if (balance >= item.price)
        {
            //SetupButtonBuyByItem(item);
            //confirmItemNameText.text = item.DisplayDescription;
            //confirmItemDescriptionText.text = item.DisplayDescription;

            // Получаем текст напрямую из таблиц Unity Localization по ID предмета
            var nameRef = new LocalizedString { TableReference = "Shop_Items", TableEntryReference = $"{item.id}.name" };
            var descRef = new LocalizedString { TableReference = "Shop_Items", TableEntryReference = $"{item.id}.desc" };

            confirmItemNameText.text = nameRef.GetLocalizedString();
            confirmItemDescriptionText.text = descRef.GetLocalizedString();

            iconImage.sprite = IconLoader.LoadFromResources(item.iconSpriteName);
            bool isGems = item.currencyType == CurrencyType.Gems ? true : false;
            Color[] colors = isGems ? 
                new Color[] { premiumBtnColorOne, premiumBtnColorSecond, premiumBtnOutlineOne, premiumBtnOutlineSecond } :
                new Color[] { basicBtnColorOne, basicBtnColorSecond, basicBtnOutlineOne, basicBtnOutlineSecond };
            SetButtonColors(colors[0], colors[1], colors[2], colors[3]);
            lightingPremium.SetActive(isGems);
            iconPremium.SetActive(isGems);
            iconBase.SetActive(!isGems);
            confirmItemPriceText.text = $"{item.price}";

            confirmationPopup.SetActive(true);
        }
        else
        {
            int missing = item.price - balance;
            string currName = item.currencyType == CurrencyType.Gems ? "гемов" : "монет";

            // Название предмета тоже берём из локализации
            var nameRef = new LocalizedString { TableReference = "Shop_Items", TableEntryReference = $"{item.id}.name" };
            string itemName = nameRef.GetLocalizedString();

            insufficientText.text = $"Вам не хватает {missing} {currName} для покупки \n {itemName}.";
            insufficientPopup.SetActive(true);

            //insufficientText.text = $"Вам не хватает {missing} {currName} для покупки \n {item.name}.";
            //insufficientPopup.SetActive(true);
        }
    }

    private void SetupButtonBuyByItem(ShopItemData item)
    {
        iconImage.sprite = IconLoader.LoadFromResources(_pendingShopItem.iconSpriteName);
    }


    void SetButtonColors(Color c1, Color c2, Color o1, Color o2)
    {
        if (gradientBase != null) { gradientBase.Color1 = c1; gradientBase.Color2 = c2; }
        if (gradientOutline != null) { gradientOutline.Color1 = o1; gradientOutline.Color2 = o2; }
    }

    private void OnConfirmBuy()
    {
        if (_pendingShopItem == null) return;

        bool success = shopManager.PurchaseItem(_pendingShopItem);
        if (success)
        {
            // Логируем ID, так как name теперь в таблице, а не в модели
            Debug.Log($"[PurchaseFlow] ✅ Успешная покупка: {_pendingShopItem.id}");
        }
        else
        {
            Debug.LogWarning("[PurchaseFlow] ⚠️ Покупка отменена системой.");
        }
        CloseAllPopups();
    }

    private void OpenCurrencyShop()
    {
        currencyShop?.OpenShop();
        CloseAllPopups();
    }

    public void CloseAllPopups()
    {
        if (confirmationPopup != null) confirmationPopup.SetActive(false);
        if (insufficientPopup != null) insufficientPopup.SetActive(false);
        _pendingShopItem = null;
    }

    // === СЕРВЕРНАЯ ВАЛИДАЦИЯ (заглушка под Cloud Functions) ===
    private async void ValidateAndGrant(string productId, string receipt)
    {
        // TODO: await IAPValidator.ValidateAsync(productId, receipt);
        await Task.Delay(300);
        bool serverOk = true;

        if (serverOk && _packsById.TryGetValue(productId, out var pack))
        {
            currencyManager?.AddCurrency(pack.currency, pack.amount);
            packManager?.ProcessSuccessfulPurchase(pack);
            _purchaseTcs?.TrySetResult(true);
        }
        else
        {
            Debug.LogError($"[IAP] Server validation failed for {productId}");
            _purchaseTcs?.TrySetResult(false);
        }
    }
}