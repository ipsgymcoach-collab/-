using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GarageMenuController : MonoBehaviour
{
    [Header("Камеры / Точки обзора")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform defaultView;
    [SerializeField] private Transform marketView;
    [SerializeField] private Transform garageView;
    [SerializeField] private Transform garryView;
    [SerializeField] private Transform anatoliyView;

    [Header("HUD (ТОЛЬКО CanvasHUD!)")]
    [SerializeField] private GameObject hudPanel;

    [Header("Кнопки меню")]
    [SerializeField] private Button marketButton;
    [SerializeField] private Button garageButton;
    [SerializeField] private Button officeButton;
    [SerializeField] private Button garryButton;
    [SerializeField] private Button anatoliyButton;

    [Header("UI контроллеры")]
    [SerializeField] private MarketUIControllerSergei marketUIController;
    [SerializeField] private GarageUIControllerEddy eddyUIController;
    [SerializeField] private ResourcesUIControllerGarry garryUIController;
    [SerializeField] private GarageUIControllerAnatoliy anatoliyUIController;

    private void Start()
    {
        marketButton?.onClick.AddListener(OpenMarket);
        garageButton?.onClick.AddListener(OpenGarage);
        garryButton?.onClick.AddListener(OpenGarry);
        anatoliyButton?.onClick.AddListener(OpenAnatoliy);
        officeButton?.onClick.AddListener(ReturnToOffice);

        // При входе в сцену HUD всегда включён
        if (hudPanel) hudPanel.SetActive(true);
    }

    private void HideHUD()
    {
        if (hudPanel && hudPanel.activeSelf)
            hudPanel.SetActive(false);
    }

    private void ShowHUD()
    {
        if (hudPanel && !hudPanel.activeSelf)
            hudPanel.SetActive(true);
    }

    // ========================= NPC =========================

    public void OpenMarket()
    {
        mainCamera.transform.SetPositionAndRotation(marketView.position, marketView.rotation);
        HideHUD();
        marketUIController?.StartMarketEvent();
    }

    public void OpenGarage()
    {
        mainCamera.transform.SetPositionAndRotation(garageView.position, garageView.rotation);
        HideHUD();
        eddyUIController?.StartEddyEvent();
    }
    public void OpenAnatoliy()
    {
        mainCamera.transform.SetPositionAndRotation(anatoliyView.position, anatoliyView.rotation);
        HideHUD();
        anatoliyUIController?.StartAnatoliyEvent();
    }

    public void OpenGarry()
    {
        mainCamera.transform.SetPositionAndRotation(garryView.position, garryView.rotation);
        HideHUD();
        garryUIController?.StartGarryEvent();
    }

    // ========================= Вернуться =========================

    public void ReturnCameraToDefault()
    {
        mainCamera.transform.SetPositionAndRotation(defaultView.position, defaultView.rotation);
        ShowHUD();
    }

    public void ReturnToOffice()
    {
        SceneManager.LoadScene("OfficeScene");
    }
}
