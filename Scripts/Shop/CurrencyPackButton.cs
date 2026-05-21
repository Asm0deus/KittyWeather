using UnityEngine;
using UnityEngine.UI;

public class CurrencyPackButton : MonoBehaviour
{
    [SerializeField] private string packId; // "coins_100", "gems_50" è ò.ä.
    void Start() => GetComponent<Button>().onClick.AddListener(() => CurrencyPackManager.Instance.RequestPurchase(packId));
}