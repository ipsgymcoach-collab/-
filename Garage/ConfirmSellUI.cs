using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ConfirmSellUI : MonoBehaviour
{
    public static ConfirmSellUI Instance;

    [Header("UI")]
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private GameObject panel;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private VehicleData pendingVehicle;
    private GaragePanelController garage;

    private void Awake()
    {
        Instance = this;

        panel.SetActive(false);
        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(OnCancel);
    }

    public void Show(VehicleData v, int price, GaragePanelController controller)
    {
        pendingVehicle = v;
        garage = controller;

        priceText.text = $"Цена продажи: {price:N0}$";
        panel.SetActive(true);
    }

    private void OnCancel()
    {
        panel.SetActive(false);
        pendingVehicle = null;
    }

    private void OnConfirm()
    {
        panel.SetActive(false);

        if (pendingVehicle != null && garage != null)
            garage.TrySell(pendingVehicle);

        pendingVehicle = null;
    }
}
