using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI-элемент одного заказа в списке категории (Пригород/Город/Центр/Спец).
/// Очень похож на SuburbOrderItemUI, но работает с OrdersCategoryPanelUI.
/// </summary>
public class CategoryOrderItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text addressText;
    [SerializeField] private TMP_Text paymentText;
    [SerializeField] private TMP_Text deadlineText;
    [SerializeField] private Image easyIcon;
    [SerializeField] private Image mediumIcon;
    [SerializeField] private Image hardIcon;
    [SerializeField] private Button selectButton;

    private SuburbOrderData currentOrder;
    private OrdersCategoryPanelUI parentPanel;

    /// <summary>
    /// Инициализация элемента данных.
    /// </summary>
    public void Setup(SuburbOrderData order, OrdersCategoryPanelUI panel)
    {
        currentOrder = order;
        parentPanel = panel;

        if (addressText != null)
            addressText.text = order.address;

        if (paymentText != null)
            paymentText.text = $"Оплата: {order.payment:N0}$";

        if (deadlineText != null)
            deadlineText.text = $"{order.duration} дн.";

        if (easyIcon != null) easyIcon.gameObject.SetActive(order.difficulty == 1);
        if (mediumIcon != null) mediumIcon.gameObject.SetActive(order.difficulty == 2);
        if (hardIcon != null) hardIcon.gameObject.SetActive(order.difficulty == 3);

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() =>
            {
                if (parentPanel != null)
                    parentPanel.ShowOrderDetails(order);
            });
        }
    }
}
