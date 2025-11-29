using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CartItemUI : MonoBehaviour
{
    [Header("Основные элементы строки корзины")]
    public Toggle checkToggle;
    public TMP_Text nameText;
    public TMP_Text countText;
    public TMP_Text priceText;
    public Button deleteButton;

    [Header("Редактирование количества")]
    public Button minusButton;
    public TMP_InputField amountInput;
    public Button plusButton;

    [Header("Удаление только выбранного количества")]
    public Button deleteSelectedButton;
}

