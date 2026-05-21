using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ShopUIController : MonoBehaviour
{
    [Header("References")]
    public ShopManager shopManager;
    public ShopItemUI itemPrefab;
    public Transform contentPanel;

    [Header("Tabs")]
    public Button tabDecorations, tabInterior, tabSkins, tabSounds;
    public Toggle showAllToggle;

    private string _lastCategory = "decorations";

    private void Start()
    {
        if (shopManager == null) Debug.LogError("[ShopUI] ShopManager not assigned!");

        shopManager.OnItemPurchased += RefreshCurrentCategory;
        if (showAllToggle != null) showAllToggle.onValueChanged.AddListener(_ => RefreshCurrentCategory());

        SetupTabs();
        LoadCategory("decorations");
    }

    private void SetupTabs()
    {
        tabDecorations?.onClick.AddListener(() => LoadCategory("decorations"));
        tabInterior?.onClick.AddListener(() => LoadCategory("interior"));
        tabSkins?.onClick.AddListener(() => LoadCategory("skins"));
        tabSounds?.onClick.AddListener(() => LoadCategory("sounds"));
    }

    private void LoadCategory(string category)
    {
        _lastCategory = category;
        ClearList();
        bool showPurchased = showAllToggle != null && showAllToggle.isOn;
        var items = shopManager.GetCategoryItems(category, showPurchased);
        BuildList(items);
    }

    private void RefreshCurrentCategory() => LoadCategory(_lastCategory);
    private void ClearList() { foreach (Transform c in contentPanel) Destroy(c.gameObject); }

    private void BuildList(List<ShopItemData> items)
    {
        if (items == null || items.Count == 0) return;
        foreach (var item in items)
        {
            var instance = Instantiate(itemPrefab, contentPanel);
            // ВАЖНО: передаём вызов в единый контроллер попапов
            instance.Setup(item, (data) => PurchaseFlowController.Instance?.InitiatePurchase(data));
            //inst.Setup(item, data => shopManager.PurchaseItem(data));
        }
    }
}