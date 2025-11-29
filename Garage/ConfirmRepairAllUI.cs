using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ConfirmRepairAllUI : MonoBehaviour
{
    public static ConfirmRepairAllUI Instance;

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button confirmBtn;
    [SerializeField] private Button cancelBtn;

    private GaragePanelController garage;
    private int totalCost;

    private void Awake()
    {
        Instance = this;

        if (panel != null)
            panel.SetActive(false);

        if (confirmBtn != null)
            confirmBtn.onClick.AddListener(OnConfirm);

        if (cancelBtn != null)
            cancelBtn.onClick.AddListener(OnCancel);
    }

    public void Show(GaragePanelController controller, int price)
    {
        garage = controller;
        totalCost = price;

        if (priceText != null)
            priceText.text = $"Стоимость ремонта: {price:N0}$";

        if (panel != null)
            panel.SetActive(true);
    }

    private void OnCancel()
    {
        if (panel != null)
            panel.SetActive(false);

        garage = null;
        totalCost = 0;
    }

    private void OnConfirm()
    {
        if (panel != null)
            panel.SetActive(false);

        if (garage != null)
            garage.RepairAllConfirmed(totalCost);

        garage = null;
        totalCost = 0;
    }
}
