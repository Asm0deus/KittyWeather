using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;

public class CurrencyShopController : MonoBehaviour
{
    public static CurrencyShopController Instance { get; private set; }

    [Header("UI Ссылки")]
    [SerializeField] private GameObject currencyShopPanel;
    [SerializeField] private GameObject conversionPopup;
    [SerializeField] private GameObject purchasePopup;

    [Header("Тексты попапов")]
    [SerializeField] private TextMeshProUGUI conversionInfoText;
    [SerializeField] private TextMeshProUGUI purchaseInfoText;
    [SerializeField] private TextMeshProUGUI conversionButtonText;
    [SerializeField] private TextMeshProUGUI purchaseButtonText;
    [SerializeField] private TextMeshProUGUI purchaseCurrencyText;

    [Header("Кнопки попапов")]
    [SerializeField] private Button conversionConfirmBtn;
    [SerializeField] private Button conversionLaterBtn;
    [SerializeField] private Button purchaseConfirmBtn;
    [SerializeField] private Button purchaseLaterBtn;

    [Header("Блоки позиций")]
    [SerializeField] private CurrencyShopPosition[] conversionPositions;
    [SerializeField] private CurrencyShopPosition[] purchasePositions;

    [Header("Тултип (уведомления)")]
    [SerializeField] private GameObject tooltipPanel;          // Панель тултипа (по умолчанию неактивна)
    [SerializeField] private CanvasGroup tooltipCanvasGroup;   
    [SerializeField] private TextMeshProUGUI tooltipText;      // Текст сообщения
    [SerializeField] private float tooltipDuration = 2.5f;     // Сколько секунд показывать
    [SerializeField] private AnimationCurve tooltipFadeCurve;  // Кривая затухания (опционально)

    [SerializeField] private LocalizedString localizedTooltipText;

    private int _pendingConversionAmount = 0;
    private float _pendingPurchaseCost = 0;
    private string _pendingIapProductId = "";
    private Coroutine _tooltipCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        SetupButtons();
        SetupShopPositions();
        if (currencyShopPanel != null) currencyShopPanel.SetActive(false);

        if (tooltipPanel != null) tooltipPanel.SetActive(false);

        // Если кривая не назначена — используем линейную
        if (tooltipFadeCurve == null || tooltipFadeCurve.keys.Length == 0)
            tooltipFadeCurve = AnimationCurve.Linear(0, 1, 1, 0);
    }

    private void SetupShopPositions()
    {
        foreach (var pos in conversionPositions) pos?.Setup(this);
        foreach (var pos in purchasePositions) pos?.Setup(this);
    }

    private void SetupButtons()
    {
        if (conversionLaterBtn != null) conversionLaterBtn.onClick.AddListener(CloseAllPopups);
        if (purchaseLaterBtn != null) purchaseLaterBtn.onClick.AddListener(CloseAllPopups);
        if (conversionConfirmBtn != null) conversionConfirmBtn.onClick.AddListener(ProcessConversion);
        if (purchaseConfirmBtn != null) purchaseConfirmBtn.onClick.AddListener(ProcessPurchase);
    }

    public void OpenShop()
    {
        if (currencyShopPanel != null) currencyShopPanel.SetActive(true);
    }

    public void CloseShop()
    {
        if (conversionPopup != null) conversionPopup.SetActive(false);
        if (purchasePopup != null) purchasePopup.SetActive(false);
        if (currencyShopPanel != null) currencyShopPanel.SetActive(false);
    }

    private void CloseAllPopups()
    {
        if (conversionPopup != null) conversionPopup.SetActive(false);
        if (purchasePopup != null) purchasePopup.SetActive(false);
    }

    // === СИСТЕМА ТУЛТИПОВ ===

    /// <summary>
    /// Показать временное уведомление с авто-скрытием
    /// </summary>
    public void ShowTooltip(string message, float duration = -1)
    {
        if (tooltipPanel == null || tooltipText == null)
        {
            Debug.LogWarning("[CurrencyShop] Tooltip UI not assigned!");
            return;
        }

        // Отменяем предыдущий тултип, если он ещё показывается
        if (_tooltipCoroutine != null) StopCoroutine(_tooltipCoroutine);

        tooltipText.text = message;
        tooltipPanel.SetActive(true);
        tooltipCanvasGroup.alpha = 1f; // Если есть CanvasGroup

        _tooltipCoroutine = StartCoroutine(TooltipFadeRoutine(duration > 0 ? duration : tooltipDuration));
    }

    private IEnumerator TooltipFadeRoutine(float duration)
    {
        // Ждём основную часть времени
        yield return new WaitForSeconds(duration * 0.7f);

        // Плавное затухание (если есть CanvasGroup)
        var cg = tooltipCanvasGroup;
        if (cg != null)
        {
            float elapsed = 0f;
            float fadeTime = duration * 0.3f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                cg.alpha = tooltipFadeCurve.Evaluate(elapsed / fadeTime);
                yield return null;
            }
        }
        else
        {
            // Простое скрытие без анимации
            yield return new WaitForSeconds(duration * 0.3f);
        }

        tooltipPanel.SetActive(false);
        _tooltipCoroutine = null;
    }

    // === КОНВЕРТАЦИЯ ===
    public void SelectConversion(int gemsAmount)
    {
        int currentGems = CurrencyManager.Instance.GetBalance(CurrencyType.Gems);

        if (currentGems < gemsAmount)
        {
            // Показываем тултип вместо лога
            int missing = gemsAmount - currentGems;
            ShowTooltip(localizedTooltipText.GetLocalizedString(missing), 1f);//  $"Недостаточно гемов! Нужно ещё {missing}", 1f);
            return;
        }

        _pendingConversionAmount = gemsAmount;
        int rate = EconomyConfigLoader.GetGemsToCoinsRate(gemsAmount);
        int coinsReceived = gemsAmount * rate;

        conversionInfoText.text = $"{coinsReceived}";
        conversionButtonText.text = $"{gemsAmount}";
        conversionPopup.SetActive(true);
    }

    private void ProcessConversion()
    {
        if (CurrencyManager.Instance.ConvertGemsToCoins(_pendingConversionAmount))
        {
            CloseAllPopups();
            ShowTooltip($"Конвертировано в {_pendingConversionAmount * EconomyConfigLoader.GetGemsToCoinsRate(_pendingConversionAmount)}", 2f);
        }
        else
        {
            ShowTooltip("Ошибка конвертации", 1.5f);
        }
    }

    // === ПОКУПКА ПАКОВ (IAP) ===
    public void SelectPurchase(string productId, int gemsAmount, float realMoneyCost, string currency)
    {
        _pendingIapProductId = productId;
        _pendingPurchaseCost = realMoneyCost;

        purchaseInfoText.text = $"{gemsAmount}";
        if (currency != null) purchaseCurrencyText.text = $"{currency}";
        purchaseButtonText.text = $"{realMoneyCost}";
        purchasePopup.SetActive(true);
    }

    private void ProcessPurchase()
    {
        Debug.Log($"[CurrencyShop] Запрос на покупку IAP: {_pendingIapProductId}");
        var flow = FindFirstObjectByType<PurchaseFlowController>();
        if (flow != null)
        {
            flow.BuyProduct(_pendingIapProductId);
            ShowTooltip("Обработка покупки...", 2f);
        }
        else
        {
            Debug.LogError("[CurrencyShop] PurchaseFlowController не найден!");
            ShowTooltip("Ошибка: система покупок не готова", 2f);
        }

        CloseAllPopups();
    }
}

[Serializable]
public class CurrencyShopPosition
{
    public string displayName;
    public int gemsAmount;
    public float realMoneyCost;
    public string iapProductId;
    public string iapCurrency;
    public Button button;

    public void Setup(CurrencyShopController controller)
    {
        if (button != null)
        {
            int gems = gemsAmount;
            float cost = realMoneyCost;
            string productId = iapProductId;
            string currency = iapCurrency;

            button.onClick.AddListener(() =>
            {
                if (cost > 0) controller.SelectPurchase(productId, gems, cost, currency);
                else controller.SelectConversion(gems);
            });
        }
    }
}