using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using OpenCover.Framework.Model;

/// <summary>
/// Основная панель управления заказами
/// Отображает активные, открывает подготовку, фиксирует прибыль и настроение.
public class OrdersPanelUI : MonoBehaviour
{
    [Header("Основные элементы")]
    [SerializeField] private GameObject ordersPanel;

    [SerializeField] private Button suburbButton;
    [SerializeField] private Button cityButton;
    [SerializeField] private Button centerButton;
    [SerializeField] private Button specialButton;
    [SerializeField] private Button backButton;

    [Header("Панели категорий")]
    [SerializeField] private GameObject suburbOrdersPanel;
    [SerializeField] private GameObject cityOrdersPanel;
    [SerializeField] private GameObject centerOrdersPanel;
    [SerializeField] private GameObject specialOrdersPanel;

    public GameObject SuburbOrdersPanel => suburbOrdersPanel;

    [Header("Список активных заказов")]
    [SerializeField] private Transform ordersContainer;
    [SerializeField] private GameObject orderItemPrefab;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Панель подготовки заказа")]
    [SerializeField] private OrderPreparationUI orderPreparationPanel;

    public static OrdersPanelUI Instance;
    public List<OrderItemUI> ActiveOrderItems { get; private set; } = new();

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        UpdateCategoryAccess();
        PopulateActiveOrders();
    }

    private void Start()
    {
        suburbButton.onClick.AddListener(OpenSuburbOrders);
        cityButton.onClick.AddListener(OpenCityOrders);
        centerButton.onClick.AddListener(OpenCenterOrders);
        specialButton.onClick.AddListener(OpenSpecialOrders);

        backButton.onClick.AddListener(ClosePanel);
    }

    // ============================================================
    // === Генерация списка активных заказов ===
    // ============================================================
    private void PopulateActiveOrders()
    {
        foreach (Transform child in ordersContainer)
            Destroy(child.gameObject);

        ActiveOrderItems.Clear();

        var activeOrders = GameManager.Instance.Data.activeOrders;
        foreach (var order in activeOrders)
        {
            var itemGO = Instantiate(orderItemPrefab, ordersContainer);
            var ui = itemGO.GetComponent<OrderItemUI>();
            ui.Setup(order, this);
            ActiveOrderItems.Add(ui);
        }
    }

    // ============================================================
    // === Проверка доступных категорий ===
    // ============================================================
    private void UpdateCategoryAccess()
    {
        GameData data = GameManager.Instance.Data;

        suburbButton.interactable = true;

        cityButton.interactable =
            data.playerLevel >= 3 &&
            data.completedOrders.Contains("Посёлковый дом городского типа");

        centerButton.interactable =
            data.playerLevel >= 6 &&
            data.completedOrders.Contains("Дом Великого Работяги");

        specialButton.interactable =
            data.playerLevel >= 8 &&
            data.completedOrders.Contains("Мой дом");
    }

    // ============================================================
    // === Открытие панелей категорий ===
    // ============================================================

    public void OpenSuburbOrders()
    {
        if (ordersPanel != null)
            ordersPanel.SetActive(false);

        if (suburbOrdersPanel != null)
            suburbOrdersPanel.SetActive(true);
        else
            Debug.LogWarning("⚠️ Не привязана панель SuburbOrdersPanel в инспекторе!");
    }

    public void OpenCityOrders()
    {
        if (!cityButton.interactable)
        {
            Debug.Log("⚠️ Город пока недоступен");
            return;
        }

        ordersPanel.SetActive(false);

        if (cityOrdersPanel != null)
            cityOrdersPanel.SetActive(true);
        else
            Debug.LogWarning("⚠️ Не привязана панель CityOrdersPanel!");
    }

    public void OpenCenterOrders()
    {
        if (!centerButton.interactable)
        {
            Debug.Log("⚠️ Центр пока недоступен");
            return;
        }

        ordersPanel.SetActive(false);

        if (centerOrdersPanel != null)
            centerOrdersPanel.SetActive(true);
        else
            Debug.LogWarning("⚠️ Не привязана панель CenterOrdersPanel!");
    }

    public void OpenSpecialOrders()
    {
        if (!specialButton.interactable)
        {
            Debug.Log("⚠️ Спец. заказы пока недоступны");
            return;
        }

        ordersPanel.SetActive(false);

        if (specialOrdersPanel != null)
            specialOrdersPanel.SetActive(true);
        else
            Debug.LogWarning("⚠️ Не привязана панель SpecialOrdersPanel!");
    }

    // ============================================================
    // === Возврат в меню заказов ===
    // ============================================================
    public void ReturnToOrdersMenu()
    {
        if (ordersPanel != null)
            ordersPanel.SetActive(true);

        if (suburbOrdersPanel != null)
            suburbOrdersPanel.SetActive(false);
        if (cityOrdersPanel != null)
            cityOrdersPanel.SetActive(false);
        if (centerOrdersPanel != null)
            centerOrdersPanel.SetActive(false);
        if (specialOrdersPanel != null)
            specialOrdersPanel.SetActive(false);

        RefreshActiveOrders();
        Debug.Log("📋 Возврат в главное меню заказов");
    }

    // ============================================================
    // === Закрытие панели ===
    // ============================================================
    private void ClosePanel()
    {
        ordersPanel.SetActive(false);
        GameManager.Instance.IsUIOpen = false;
        Time.timeScale = GameManager.Instance.CurrentGame.timeScaleBeforePause;
    }

    // ============================================================
    // === Обновление активных заказов ===
    // ============================================================
    public void RefreshActiveOrders()
    {
        foreach (Transform child in ordersContainer)
            Destroy(child.gameObject);

        ActiveOrderItems.Clear();

        var activeOrders = GameManager.Instance.Data.activeOrders;
        foreach (var order in activeOrders)
        {
            var itemGO = Instantiate(orderItemPrefab, ordersContainer);
            var ui = itemGO.GetComponent<OrderItemUI>();
            ui.Setup(order, this);
            ActiveOrderItems.Add(ui);
        }
    }

    // ============================================================
    // === Открытие панели подготовки ===
    // ============================================================
    public void OpenPreparation(OrderData order, bool viewOnly = false)
    {
        if (orderPreparationPanel == null)
        {
            Debug.LogWarning("⚠ Не привязана панель подготовки (OrderPreparationUI)!");
            return;
        }

        OrdersDatabase db = Resources.Load<OrdersDatabase>("Databases/OrdersDatabase");
        OrderInfo info = null;

        if (db != null)
        {
            info = db.suburbOrders.FirstOrDefault(o => o.address == order.orderName)
                ?? db.cityOrders.FirstOrDefault(o => o.address == order.orderName)
                ?? db.centerOrders.FirstOrDefault(o => o.address == order.orderName)
                ?? db.specialOrders.FirstOrDefault(o => o.address == order.orderName);
        }

        SuburbOrderData temp = new SuburbOrderData()
        {
            id = info?.id ?? order.orderName,
            address = info?.address ?? order.orderName,
            description = info?.description ?? "Описание недоступно.",
            payment = info?.payment ?? order.payment,
            duration = info?.duration ?? order.totalDays,
            difficulty = info?.difficulty ?? 1,
            requiredWorkers = info?.requiredWorkers ?? new List<RequiredWorker>(),
            requiredVehicles = info?.requiredVehicles ?? new List<RequiredVehicle>(),
            requiredMaterials = info?.requiredMaterials ?? new List<RequiredMaterial>()
        };

        orderPreparationPanel.OnConfirm -= OnOrderPreparationConfirmed;
        if (!viewOnly)
            orderPreparationPanel.OnConfirm += OnOrderPreparationConfirmed;

        orderPreparationPanel.OpenFromActive(order, temp);
        orderPreparationPanel.SetViewMode(viewOnly);

        if (ordersPanel != null) ordersPanel.SetActive(false);
        if (suburbOrdersPanel != null) suburbOrdersPanel.SetActive(false);
        if (cityOrdersPanel != null) cityOrdersPanel.SetActive(false);
        if (centerOrdersPanel != null) centerOrdersPanel.SetActive(false);
        if (specialOrdersPanel != null) specialOrdersPanel.SetActive(false);
    }

    // ============================================================
    // === Подтверждение настройки и старт заказа ===
    // ============================================================
    private void OnOrderPreparationConfirmed(OrderPreparationResult result)
    {
        if (result == null)
        {
            Debug.LogWarning("⚠ OrderPreparationResult пуст — возможно отменено.");
            return;
        }

        var data = GameManager.Instance.Data;
        var order = data.activeOrders
            .FirstOrDefault(o => o.orderName == result.address || o.orderId == result.orderId);

        if (order == null)
        {
            Debug.LogWarning($"⚠ Не найден заказ для {result.address}");
            return;
        }

        order.isStarted = true;
        order.isActive = true;
        order.isCompleted = false;
        order.remainingDays = Mathf.Max(1, result.plannedDurationDays);
        order.totalDays = result.limitDays;
        order.brigadeName = result.brigadeName;
        order.netProfit = Mathf.RoundToInt(result.netProfit);
        order.address = result.address;
        order.daysPassed = 0;
        order.progress = 0;

        // Назначаем бригаду
        var brigade = data.allBrigades.FirstOrDefault(b => b.name == result.brigadeName);
        if (brigade != null)
        {
            brigade.isWorking = true;
            brigade.currentOrderId = order.orderId;
            var foreman = data.foremen.FirstOrDefault(f => f.id == brigade.foremanId);
            if (foreman != null) foreman.isBusy = true;
        }

        SaveManager.SaveGame(data, GameManager.Instance.CurrentSlot);

        foreach (var item in ActiveOrderItems)
        {
            if (item.CurrentOrder != null &&
                item.CurrentOrder.orderName == order.orderName)
            {
                item.CurrentOrder.isStarted = true;
                item.CurrentOrder.isActive = true;
                item.SetActiveMode();
            }
        }

        // Закрываем панели
        if (suburbOrdersPanel != null) suburbOrdersPanel.SetActive(false);
        if (cityOrdersPanel != null) cityOrdersPanel.SetActive(false);
        if (centerOrdersPanel != null) centerOrdersPanel.SetActive(false);
        if (specialOrdersPanel != null) specialOrdersPanel.SetActive(false);

        if (ordersPanel != null) ordersPanel.SetActive(true);

        RefreshActiveOrders();
    }

    // ============================================================
    // === Автоматическая проверка завершения ===
    // ============================================================
    private void Update()
    {
        var data = GameManager.Instance?.Data;
        if (data == null || data.activeOrders == null)
            return;

        foreach (var order in data.activeOrders.ToList())
        {
            if (order.isStarted && order.daysPassed >= order.remainingDays)
                CompleteOrder(order);
        }
    }

    // ============================================================
    // === Завершение заказа ===
    // ============================================================
    private void CompleteOrder(OrderData order)
    {
        var data = GameManager.Instance.Data;
        if (data == null) return;

        // Освобождаем бригаду
        var brigade = data.allBrigades.FirstOrDefault(b => b.name == order.brigadeName);
        if (brigade != null)
        {
            brigade.isWorking = false;
            brigade.currentOrderId = "";
            var foreman = data.foremen.FirstOrDefault(f => f.id == brigade.foremanId);
            if (foreman != null) foreman.isBusy = false;
        }

        // Прибыль
        int profit = Mathf.Max(order.netProfit, 0);
        data.money += profit;

        // XP
        data.playerXP += 10;
        data.suburbXP += 10;

        // Завершение
        data.activeOrders.Remove(order);
        data.completedOrders.Add(order.address);

        HUDController.Instance?.ShowToast($"🏗 Заказ завершён: {order.address}\n+{profit}$, +10 XP");

        // Восстанавливаем технику
        OrdersDatabase db = Resources.Load<OrdersDatabase>("Databases/OrdersDatabase");
        OrderInfo info = null;

        if (db != null)
        {
            info = db.suburbOrders.FirstOrDefault(o => o.address == order.orderName)
                ?? db.cityOrders.FirstOrDefault(o => o.address == order.orderName)
                ?? db.centerOrders.FirstOrDefault(o => o.address == order.orderName)
                ?? db.specialOrders.FirstOrDefault(o => o.address == order.orderName);
        }

        if (info != null)
        {
            foreach (var req in info.requiredVehicles)
            {
                int need = Mathf.Max(1, req.count);

                foreach (var v in data.ownedVehicles)
                {
                    if (v == null) continue;
                    if (v.id != req.vehicleId) continue;
                    if (v.isUnderRepair) continue;
                    if (v.inGarage) continue;

                    v.inGarage = true;
                    need--;
                    if (need <= 0) break;
                }
            }
        }

        SaveManager.SaveGame(data, GameManager.Instance.CurrentSlot);
        RefreshActiveOrders();

        if (BrigadePanelUI.Instance != null)
            BrigadePanelUI.Instance.RefreshBrigadeList();
    }
}
