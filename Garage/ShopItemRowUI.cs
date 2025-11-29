using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemRowUI : MonoBehaviour
{
    [Header("Элементы UI")]
    public TMP_Text nameText;
    public TMP_Text priceText;
    public TMP_InputField countInput;
    public Button minusButton;
    public Button plusButton;
    public Button addButton;

    // 🟡 Новое поле для связи с ResourceShopUI
    public string itemId;
}
