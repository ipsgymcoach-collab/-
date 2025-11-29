using UnityEngine;
using UnityEngine.UI;

public class BookShopTabs : MonoBehaviour
{
    [Header("Контенты вкладок")]
    public GameObject contentTerra;
    public GameObject contentGarage;
    public GameObject contentWarehouse;

    [Header("Кнопки вкладок (Image на Button)")]
    public Button terraButton;
    public Button garageButton;
    public Button warehouseButton;

    [Header("Цвета")]
    public Color activeColor = new Color(0.75f, 0.75f, 0.75f); // слегка серый
    public Color normalColor = Color.white;

    private void OnEnable()
    {
        // При открытии панели — сразу Terra
        ShowTerra();
    }

    public void ShowTerra()
    {
        contentTerra.SetActive(true);
        contentGarage.SetActive(false);
        contentWarehouse.SetActive(false);

        SetActiveTab(terraButton);
        SetInactiveTab(garageButton);
        SetInactiveTab(warehouseButton);
    }

    public void ShowGarage()
    {
        contentTerra.SetActive(false);
        contentGarage.SetActive(true);
        contentWarehouse.SetActive(false);

        SetInactiveTab(terraButton);
        SetActiveTab(garageButton);
        SetInactiveTab(warehouseButton);
    }

    public void ShowWarehouse()
    {
        contentTerra.SetActive(false);
        contentGarage.SetActive(false);
        contentWarehouse.SetActive(true);

        SetInactiveTab(terraButton);
        SetInactiveTab(garageButton);
        SetActiveTab(warehouseButton);
    }

    private void SetActiveTab(Button btn)
    {
        btn.interactable = false;
        btn.image.color = activeColor;
    }

    private void SetInactiveTab(Button btn)
    {
        btn.interactable = true;
        btn.image.color = normalColor;
    }
}
