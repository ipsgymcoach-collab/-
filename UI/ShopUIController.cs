using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class ShopUIController : MonoBehaviour
{
    [Header("Основные панели")]
    [SerializeField] private GameObject shopRoot;
    [SerializeField] private GameObject contentRoot;
    [SerializeField] private GameObject itemCardPrefab;
    [SerializeField] private TMP_InputField searchInput;
    [SerializeField] private Button closeButton;

    [Header("Кнопки категорий")]
    [SerializeField] private Button allButton;
    [SerializeField] private Button workingButton;
    [SerializeField] private Button transportButton;

    [Header("Кнопки сортировки")]
    [SerializeField] private Button sortByLevelButton;
    [SerializeField] private Button sortByPriceButton;

    [Header("Цвета UI")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.8f, 0.3f);

    [Header("Информация игрока")]
    [SerializeField] private TMP_Text playerMoneyText;
    [SerializeField] private TMP_Text playerLevelText;

    [Header("Уведомления (Toast)")]
    [SerializeField] private CanvasGroup toastPanel;
    [SerializeField] private TMP_Text toastText;
    [SerializeField] private float toastDuration = 3f;
    [SerializeField] private float toastFadeSpeed = 3f;

    [Header("Панель информации о технике")]
    [SerializeField] private VehicleInfoPanel vehicleInfoPanel; // 🟡 Новое поле

    private MarketUIControllerSergei sergeiController;
    private List<VehicleData> currentList = new List<VehicleData>();
    private VehicleGroup? currentGroup = null;
    private bool sortByLevelAsc = true;
    private bool sortByPriceAsc = true;
    private string currentSearch = "";
    private bool initialized = false;
    private GameData data;

    private void OnEnable()
    {
        if (!initialized)
        {
            initialized = true;
            StartCoroutine(InitializeAfterFrame());
        }
    }

    private IEnumerator InitializeAfterFrame()
    {
        yield return null;
        currentGroup = null;
        currentSearch = "";
        sortByLevelAsc = true;
        sortByPriceAsc = true;
        OnCategorySelected(null);
        UpdateCategoryButtonsUI();
        if (searchInput != null)
            searchInput.text = "";
    }

    private void Awake()
    {
        allButton.onClick.AddListener(() => OnCategorySelected(null));
        workingButton.onClick.AddListener(() => OnCategorySelected(VehicleGroup.Working));
        transportButton.onClick.AddListener(() => OnCategorySelected(VehicleGroup.Transport));

        sortByLevelButton.onClick.AddListener(ToggleSortByLevel);
        sortByPriceButton.onClick.AddListener(ToggleSortByPrice);

        if (searchInput != null)
            searchInput.onValueChanged.AddListener(OnSearchChanged);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseShop);
    }

    public void OpenShop(MarketUIControllerSergei sergei)
    {
        sergeiController = sergei;
        gameObject.SetActive(true);
        data = GameManager.Instance.CurrentGame;
        UpdatePlayerInfo();
        currentGroup = null;
        currentSearch = "";
        OnCategorySelected(null);
        UpdateCategoryButtonsUI();
    }

    public void UpdatePlayerInfo()
    {
        if (data == null) return;
        if (playerMoneyText != null)
            playerMoneyText.text = "$ " + data.money.ToString("N0");
        if (playerLevelText != null)
            playerLevelText.text = "Лицензия: " + data.level;
    }

    public void UpdateMoneyUI() => UpdatePlayerInfo();

    private void RefreshList()
    {
        foreach (Transform child in contentRoot.transform)
            Destroy(child.gameObject);

        GameData data = GameManager.Instance.CurrentGame;
        if (data == null || data.vehicles == null)
            return;

        int playerLevel = data.level;
        IEnumerable<VehicleData> vehicles = data.vehicles;

        if (currentGroup.HasValue)
            vehicles = vehicles.Where(v => v.group == currentGroup.Value);

        if (!string.IsNullOrEmpty(currentSearch))
            vehicles = vehicles.Where(v => v.name.ToLower().Contains(currentSearch.ToLower()));

        IOrderedEnumerable<VehicleData> ordered = vehicles.OrderBy(v => 0);
        if (sortByLevelAsc)
            ordered = vehicles.OrderBy(v => v.unlockLevel);
        else
            ordered = vehicles.OrderByDescending(v => v.unlockLevel);
        if (sortByPriceAsc)
            ordered = ordered.ThenBy(v => v.price);
        else
            ordered = ordered.ThenByDescending(v => v.price);
        vehicles = ordered;

        foreach (var v in vehicles)
        {
            GameObject card = Instantiate(itemCardPrefab, contentRoot.transform);
            ShopItemCardUI cardUI = card.GetComponent<ShopItemCardUI>();
            cardUI.Setup(v, data, playerLevel);
        }

        UpdateCategoryButtonsUI();
    }

    private void OnCategorySelected(VehicleGroup? group)
    {
        currentGroup = group;
        RefreshList();
    }

    private void ToggleSortByLevel()
    {
        sortByLevelAsc = !sortByLevelAsc;
        RefreshList();
    }

    private void ToggleSortByPrice()
    {
        sortByPriceAsc = !sortByPriceAsc;
        RefreshList();
    }

    private void OnSearchChanged(string value)
    {
        currentSearch = value;
        RefreshList();
    }

    private void UpdateCategoryButtonsUI()
    {
        allButton.image.color = normalColor;
        workingButton.image.color = normalColor;
        transportButton.image.color = normalColor;

        if (currentGroup == null) allButton.image.color = selectedColor;
        else if (currentGroup == VehicleGroup.Working) workingButton.image.color = selectedColor;
        else if (currentGroup == VehicleGroup.Transport) transportButton.image.color = selectedColor;
    }

    public void CloseShop()
    {
        SaveManager.SaveGame(GameManager.Instance.CurrentGame, GameManager.Instance.CurrentSlot);
        gameObject.SetActive(false);
        if (sergeiController != null)
            sergeiController.ShowChoicePanel();
        if (HUDController.Instance != null)
            HUDController.Instance.EnableControls();
    }

    public void ShowToast(string message)
    {
        if (toastPanel == null || toastText == null)
            return;
        StopAllCoroutines();
        StartCoroutine(ShowToastCoroutine(message));
    }

    private IEnumerator ShowToastCoroutine(string message)
    {
        toastText.text = message;
        toastPanel.gameObject.SetActive(true);
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * toastFadeSpeed;
            toastPanel.alpha = Mathf.Lerp(0, 1, t);
            yield return null;
        }
        yield return new WaitForSeconds(toastDuration);
        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * toastFadeSpeed;
            toastPanel.alpha = Mathf.Lerp(1, 0, t);
            yield return null;
        }
        toastPanel.gameObject.SetActive(false);
    }

    public void TryBuyVehicle(VehicleData vehicle)
    {
        if (data == null) data = GameManager.Instance.CurrentGame;
        if (data == null) return;

        if (data.money < vehicle.price)
        {
            ShowToast("Недостаточно средств!");
            return;
        }

        data.money -= vehicle.price;
        if (!data.vehicles.Contains(vehicle))
            data.vehicles.Add(vehicle);

        SaveManager.SaveGame(data, GameManager.Instance.CurrentSlot);
        ShowToast(vehicle.name + " куплен!");
        UpdatePlayerInfo();
    }

    // 🟡 Новый метод: открытие панели описания
    public void OpenVehicleInfo(VehicleData vehicle)
    {
        if (vehicleInfoPanel != null)
        {
            vehicleInfoPanel.Show(vehicle, this);
        }
        else
        {
            Debug.LogError("[ShopUIController] VehicleInfoPanel не назначен!");
        }
    }

}
