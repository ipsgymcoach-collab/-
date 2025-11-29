using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SuburbOrderItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text addressText;
    [SerializeField] private TMP_Text paymentText;
    [SerializeField] private TMP_Text deadlineText;
    [SerializeField] private Image easyIcon;
    [SerializeField] private Image mediumIcon;
    [SerializeField] private Image hardIcon;
    [SerializeField] private Button selectButton;

    private SuburbOrderData currentOrder;
    private SuburbOrdersPanelUI parentPanel;



    public void Setup(SuburbOrderData order, SuburbOrdersPanelUI panel)
    {
        currentOrder = order;
        parentPanel = panel;

        addressText.text = order.address;
        paymentText.text = $"Оплата: {order.payment}$";
        deadlineText.text = $"{order.duration} дн.";

        easyIcon.gameObject.SetActive(order.difficulty == 1);
        mediumIcon.gameObject.SetActive(order.difficulty == 2);
        hardIcon.gameObject.SetActive(order.difficulty == 3);

        selectButton.onClick.AddListener(() => parentPanel.ShowOrderDetails(order));
    }
}
