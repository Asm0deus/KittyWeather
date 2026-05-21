using UnityEngine;
using TMPro;

public class CurrencyDisplay : MonoBehaviour
{
    [SerializeField] private CurrencyType currencyType;
    [SerializeField] private TextMeshProUGUI balanceText;
    [SerializeField] private CurrencyShopController currencyShop; // Ссылка на контроллер магазина валюты

    private async void Start()
    {
        // Ждём, пока все системы готовы
        await GameBootstrap.WaitForReadyAsync();

        // Теперь можно безопасно подписываться на события
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnBalanceChanged += UpdateDisplay;
            UpdateDisplay();
        }
    }

    private void OnEnable()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnBalanceChanged += UpdateDisplay;
        UpdateDisplay();
    }

    private void OnDisable()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnBalanceChanged -= UpdateDisplay;
    }

    private void UpdateDisplay(CurrencyType type = default)
    {
        if (balanceText != null && CurrencyManager.Instance != null)
        {
            if (type == CurrencyType.Coins || type == CurrencyType.Gems)
                balanceText.text = CurrencyManager.Instance.GetBalance(currencyType).ToString();
        }
    }

    public void OpenCurrencyShop()
    {
        currencyShop?.OpenShop();
    }
}