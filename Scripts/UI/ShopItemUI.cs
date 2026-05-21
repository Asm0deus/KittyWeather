// version 1.0.0
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Localization;

public class ShopItemUI : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI currentPriceText;
    public TextMeshProUGUI oldPriceText;
    public Image iconImage;
    public GameObject oldPriceBlock;
    public GameObject purshIcon;
    public Button buyButton;
    public TextMeshProUGUI buyButtonText;
    public RectTransform buyButtonTextRect;
    public GameObject lightingPremium;
    public GameObject iconPremium;
    public GameObject iconBase;
    public Ricimi.Gradient gradientBase;
    public Ricimi.Gradient gradientOutline;

    public Color basicBtnColorOne, basicBtnColorSecond, basicBtnOutlineOne, basicBtnOutlineSecond;
    public Color premiumBtnColorOne, premiumBtnColorSecond, premiumBtnOutlineOne, premiumBtnOutlineSecond;
    public Color inactiveBtnColorOne, inactiveBtnColorSecond, inactiveBtnOutlineOne, inactiveBtnOutlineSecond;

    private ShopItemData _item;
    private System.Action<ShopItemData> _onBuy;

    // Ссылки на локализованные строки
    private LocalizedString _nameRef;
    private LocalizedString _descRef;

    public void Setup(ShopItemData item, System.Action<ShopItemData> onBuy)
    {
        _item = item;
        _onBuy = onBuy;

        // Инициализируем биндинг к Unity Localization
        _nameRef = LocalizationBridge.GetShopItemName(item.id);
        _descRef = LocalizationBridge.GetShopItemDesc(item.id);

        // Подписываемся на смену языка (автоматически обновит текст)
        _nameRef.StringChanged += text => nameText.text = text;
        _descRef.StringChanged += text => descText.text = text;

        // Загружаем текущие значения сразу
        nameText.text = _nameRef.GetLocalizedString();
        descText.text = _descRef.GetLocalizedString();

        // Передаём скидку из ShopManager при расчёте цены
        float discount = ShopManager.Instance?.CurrentSale?.enabled == true &&
                         ShopManager.Instance.IsItemOnSale(item)
            ? ShopManager.Instance.CurrentSale.discountPercent
            : 0f;


        currentPriceText.text = item.GetFinalPrice(discount).ToString();// item.price.ToString();

        oldPriceBlock.SetActive(item.isDiscounted && item.oldPrice > 0);
        if (oldPriceBlock.activeSelf) oldPriceText.text = item.oldPrice.ToString();

        bool isPurchased = item.isPurchased;
        buyButton.interactable = !isPurchased;
        buyButtonText.text = isPurchased ? "Куплено" : item.GetFinalPrice(discount).ToString();//item.price.ToString();

        float rt = isPurchased ? 15f : 65f;
        buyButtonTextRect.offsetMin = new Vector2(rt, buyButtonTextRect.offsetMin.y);

        if (!isPurchased)
        {
            bool isGems = item.currencyType == CurrencyType.Gems;
            Color[] colors = isGems ? 
                new Color[] { premiumBtnColorOne, premiumBtnColorSecond, premiumBtnOutlineOne, premiumBtnOutlineSecond } :
                new Color[] { basicBtnColorOne, basicBtnColorSecond, basicBtnOutlineOne, basicBtnOutlineSecond };
            SetButtonColors(colors[0], colors[1], colors[2], colors[3]);
            lightingPremium.SetActive(isGems);
            iconPremium.SetActive(isGems);
            iconBase.SetActive(!isGems);
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => _onBuy?.Invoke(_item));
        }
        else
        {
            SetButtonColors(inactiveBtnColorOne, inactiveBtnColorSecond, inactiveBtnOutlineOne, inactiveBtnOutlineSecond);
            purshIcon.SetActive(true);
            lightingPremium.SetActive(false);
        }

        LoadIconAsync(item.iconSpriteName);
    }

    void SetButtonColors(Color c1, Color c2, Color o1, Color o2)
    {
        if (gradientBase != null) { gradientBase.Color1 = c1; gradientBase.Color2 = c2; }
        if (gradientOutline != null) { gradientOutline.Color1 = o1; gradientOutline.Color2 = o2; }
    }

    public void LoadIconAsync(string iconName)
    {
        if (!iconImage) return;
        iconImage.sprite = IconLoader.LoadFromResources(iconName);
    }

    // Очистка подписок при уничтожении объекта
    void OnDestroy()
    {
        if (_nameRef != null) _nameRef.StringChanged -= null;
        if (_descRef != null) _descRef.StringChanged -= null;
    }
}