using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Универсальная панель заказов для категории:
/// Пригород / Город / Центр / Спец.
/// Архитектура повторяет SuburbOrdersPanelUI, но параметризована через OrderCategory.
/// </summary>
public class OrdersCategoryPanelUI : MonoBehaviour
{
    [Header("Категория")]
    [SerializeField] private OrderCategory category = OrderCategory.Suburb;
    [SerializeField] private OrderCategoryConfig categoryConfig;

    [Header("Основные блоки")]
    [SerializeField] private GameObject categoryPanel;   // панель этой категории (аналог suburbPanel)
    [SerializeField] private GameObject ordersPanel;     // общая панель категорий (аналог ordersPanel)
    [SerializeField] private Button backButton;
    [SerializeField] private Button backToCategoriesButton;

    [Header("Уровень категории")]
    [SerializeField] private Button categoryLevelButton;

    [Header("Левая колонка (список заказов)")]
    [SerializeField] private Transform ordersContainer;
    [SerializeField] private GameObject orderItemPrefab;   // префаб с CategoryOrderItemUI
    [SerializeField] private TMP_Text nextOrdersText;
    [SerializeField] private TMP_Text hintText;

    [Header("Иконки сложности")]
    [SerializeField] private Image easyIcon;
    [SerializeField] private Image mediumIcon;
    [SerializeField] private Image hardIcon;

    [Header("Правая колонка (детали заказа)")]
    [SerializeField] private GameObject rightColumn;
    [SerializeField] private TMP_Text addressText;
    [SerializeField] private Image photoImage;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text workersText;
    [SerializeField] private TMP_Text vehiclesText;
    [SerializeField] private TMP_Text materialsText;
    [SerializeField] private TMP_Text paymentText;
    [SerializeField] private TMP_Text prepaymentText;
    [SerializeField] private TMP_Text qualityText;
    [SerializeField] private TMP_Text deadlineText;

    [Header("Элементы управления")]
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;

    [Header("Окно принятия заказа")]
    [SerializeField] private GameObject confirmAcceptedWindow;
    [SerializeField] private TMP_Text confirmAcceptedText;
    [SerializeField] private Button confirmAcceptedOkButton;

    [Header("Настройки предоплаты")]
    [Range(0f, 1f)]
    [SerializeField] private float prepaymentPercent = 0.2f; // 20 %

    /// <summary>
    /// Локальный state на случай, если в GameData нет полей для этой категории.
    /// Для Пригорода используем именно GameData.suburbOrdersState.
    /// </summary>
    private GameData.OrdersCategoryState localState = new GameData.OrdersCategoryState();

    /// <summary>
    /// Состояние категории (список id заказов + таймер обновления).
    /// Для Suburb → GameData.suburbOrdersState,
    /// для остальных категорий пока используется локальный state (до расширения GameData).
    /// </summary>
    private GameData.OrdersCategoryState CategoryState
    {
        get
        {
            var data = GameManager.Instance?.Data;
            if (data == null)
                return localState;

            switch (category)
            {
                case OrderCategory.Suburb:
                    if (data.suburbOrdersState == null)
                        data.suburbOrdersState = new GameData.OrdersCategoryState();
                    return data.suburbOrdersState;

                // ⚠ ПОЗЖЕ: когда добавим поля в GameData, раскомментируем и подправим:
                //
                // case OrderCategory.City:
                //     if (data.cityOrdersState == null)
                //         data.cityOrdersState = new GameData.OrdersCategoryState();
                //     return data.cityOrdersState;
                //
                // case OrderCategory.Center:
                //     if (data.centerOrdersState == null)
                //         data.centerOrdersState = new GameData.OrdersCategoryState();
                //     return data.centerOrdersState;
                //
                // case OrderCategory.Special:
                //     if (data.specialOrdersState == null)
                //         data.specialOrdersState = new GameData.OrdersCategoryState();
                //     return data.specialOrdersState;

                default:
                    // Временный fallback, пока GameData не расширена
                    return localState;
            }
        }
    }

    private List<SuburbOrderData> currentOrders = new List<SuburbOrderData>();
    private SuburbOrderData selectedOrder;

    public static OrdersCategoryPanelUI Instance; // если нужна глобальная ссылка на последнюю открытую панель

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(ClosePanel);

        if (backToCategoriesButton != null)
            backToCategoriesButton.onClick.AddListener(BackToCategories);

        if (acceptButton != null)
            acceptButton.onClick.AddListener(OnAcceptOrder);

        if (declineButton != null)
            declineButton.onClick.AddListener(OnDeclineOrder);

        if (categoryLevelButton != null)
            categoryLevelButton.onClick.AddListener(OnCategoryLevelButton);

        if (confirmAcceptedWindow != null)
            confirmAcceptedWindow.SetActive(false);

        if (rightColumn != null)
            rightColumn.SetActive(false);

        GenerateOrders();
        ShowOrdersList();
        UpdateNextOrdersText();
    }

    // === Кнопка "Уровень категории" ===
    private void OnCategoryLevelButton()
    {
        string catName = categoryConfig != null && !string.IsNullOrEmpty(categoryConfig.displayName)
            ? categoryConfig.displayName
            : category.ToString();

        Debug.Log($"📊 Открываю панель уровня категории [{catName}] (ещё в разработке)");
        // Позже здесь вызовем CategoryLevelPanelUI.Instance.Open(category);
    }

    private void UpdateNextOrdersText()
    {
        if (nextOrdersText == null) return;

        int days = CategoryState.daysUntilNewOrders;
        string catName = categoryConfig != null && !string.IsNullOrEmpty(categoryConfig.displayName)
            ? categoryConfig.displayName
            : category.ToString();

        nextOrdersText.text = $"Новые заказы в категории \"{catName}\" будут через {days} дней";
    }

    // === Генерация заказов для категории ===
    private void GenerateOrders()
    {
        currentOrders.Clear();

        OrdersDatabase db = Resources.Load<OrdersDatabase>("Databases/OrdersDatabase");
        if (db == null)
        {
            Debug.LogError("❌ OrdersDatabase не найден (Resources/Databases/OrdersDatabase)!");
            return;
        }

        // Если в state уже есть зафиксированные id — восстанавливаем их
        if (CategoryState.currentOrderIds != null && CategoryState.currentOrderIds.Count > 0)
        {
            var fromList = GetOrdersListForCategory(db);
            foreach (string id in CategoryState.currentOrderIds)
            {
                var info = fromList.FirstOrDefault(o => o.id == id);
                if (info != null)
                    currentOrders.Add(Convert(info));
            }
            return;
        }

        // Иначе собираем новые заказы случайно
        List<OrderInfo> available = new List<OrderInfo>(GetOrdersListForCategory(db));

        // Убираем уже выполненные
        var completed = GameManager.Instance?.Data?.completedOrders;
        if (completed != null && completed.Count > 0)
        {
            available.RemoveAll(o => completed.Contains(o.id));
        }

        int orderSlots = categoryConfig != null ? Mathf.Max(1, categoryConfig.orderSlots) : 4;
        List<string> picked = new List<string>();

        for (int i = 0; i < orderSlots && available.Count > 0; i++)
        {
            int index = Random.Range(0, available.Count);
            var info = available[index];
            available.RemoveAt(index);

            var order = Convert(info);
            currentOrders.Add(order);
            picked.Add(info.id);
        }

        CategoryState.currentOrderIds = picked;
    }

    /// <summary>
    /// Возвращает список OrderInfo для нужной категории из OrdersDatabase.
    /// </summary>
    private List<OrderInfo> GetOrdersListForCategory(OrdersDatabase db)
    {
        switch (category)
        {
            case OrderCategory.Suburb: return db.suburbOrders;
            case OrderCategory.City: return db.cityOrders;
            case OrderCategory.Center: return db.centerOrders;
            case OrderCategory.Special: return db.specialOrders;
            default: return db.suburbOrders;
        }
    }

    private SuburbOrderData Convert(OrderInfo info)
    {
        return new SuburbOrderData
        {
            id = info.id,
            address = info.address,
            description = info.description,
            payment = info.payment,
            duration = info.duration,
            difficulty = info.difficulty,
            requiredWorkers = info.requiredWorkers,
            requiredVehicles = info.requiredVehicles,
            requiredMaterials = info.requiredMaterials
        };
    }

    private void ShowOrdersList()
    {
        if (ordersContainer == null || orderItemPrefab == null)
            return;

        foreach (Transform child in ordersContainer)
            Destroy(child.gameObject);

        foreach (var order in currentOrders)
        {
            GameObject item = Instantiate(orderItemPrefab, ordersContainer);
            var ui = item.GetComponent<CategoryOrderItemUI>();
            if (ui != null)
                ui.Setup(order, this);
        }

        if (hintText != null)
            hintText.text = "Для большего количества заказов наймите больше менеджеров проекта";
    }

    /// <summary>
    /// Показ правой карточки с деталями заказа.
    /// </summary>
    public void ShowOrderDetails(SuburbOrderData order)
    {
        selectedOrder = order;

        if (rightColumn != null)
            rightColumn.SetActive(true);

        if (addressText != null)
            addressText.text = order.address;

        if (descriptionText != null)
            descriptionText.text = order.description;

        if (paymentText != null)
            paymentText.text = $"💵 Оплата: {order.payment:N0}$";

        int prepayment = Mathf.RoundToInt(order.payment * prepaymentPercent);
        if (prepaymentText != null)
            prepaymentText.text = $"💰 Предоплата: {prepayment:N0}$ ({prepaymentPercent * 100}%)";

        if (deadlineText != null)
            deadlineText.text = $"Срок: {order.duration} дн.";

        if (qualityText != null)
            qualityText.text = "Мин. качество: 80%";

        if (easyIcon != null) easyIcon.gameObject.SetActive(order.difficulty == 1);
        if (mediumIcon != null) mediumIcon.gameObject.SetActive(order.difficulty == 2);
        if (hardIcon != null) hardIcon.gameObject.SetActive(order.difficulty == 3);

        // Рабочие
        if (workersText != null)
        {
            workersText.text = "Рабочие:\n";
            foreach (var w in order.requiredWorkers)
            {
                string prof = GameManager.Instance.GetProfessionNameById(w.workerId);
                int have = GameManager.Instance.Data.hiredWorkers.Count(x => x.professionId == w.workerId);
                int free = GameManager.Instance.Data.hiredWorkers.Count(x => x.professionId == w.workerId && !x.isBusy);

                string state = have == 0 ? "(нет)" :
                               free == 0 ? "(занят)" :
                               $"({free} свободных)";

                workersText.text += $"• {prof} — {have}/{w.count} {state}\n";
            }
        }

        // Техника
        if (vehiclesText != null)
        {
            vehiclesText.text = "Техника:\n";
            foreach (var v in order.requiredVehicles)
            {
                string name = VehicleDatabase.Instance?.GetVehicleNameById(v.vehicleId) ?? v.vehicleId;

                int have = GameManager.Instance.Data.ownedVehicles
                    .Count(x => x.id == v.vehicleId);

                int free = GameManager.Instance.Data.ownedVehicles
                    .Count(x => x.id == v.vehicleId
                             && x.inGarage
                             && !x.isUnderRepair);

                string state = have == 0 ? "(нет)" :
                               free == 0 ? "(занята / в ремонте)" :
                               $"({free} свободных)";

                vehiclesText.text += $"• {name} — {have}/{v.count} {state}\n";
            }
        }

        // Материалы
        if (materialsText != null)
        {
            materialsText.text = "Материалы:\n";
            foreach (var m in order.requiredMaterials)
            {
                int have = GameManager.Instance.Data.GetResourceQuantity(m.materialId);
                string name = GameManager.Instance.GetMaterialNameById(m.materialId);
                materialsText.text += $"• {name} — {have}/{m.count}\n";
            }
        }

        if (photoImage != null)
            photoImage.color = Color.white;

        if (acceptButton != null)
            acceptButton.interactable = true;
    }

    // === Принятие заказа ===
    private void OnAcceptOrder()
    {
        if (selectedOrder == null) return;

        int prepayment = Mathf.RoundToInt(selectedOrder.payment * prepaymentPercent);

        // Деньги
        GameManager.Instance.Data.money += prepayment;
        HUDController.Instance?.UpdateMoney(GameManager.Instance.Data.money);
        HUDController.Instance?.ShowToast($"💰 Получена предоплата: +{prepayment:N0}$");

        // Окно подтверждения
        if (confirmAcceptedWindow != null)
            confirmAcceptedWindow.SetActive(true);

        if (confirmAcceptedText != null)
        {
            string catName = categoryConfig != null && !string.IsNullOrEmpty(categoryConfig.displayName)
                ? categoryConfig.displayName
                : category.ToString();

            confirmAcceptedText.text =
                $"💰 Предоплата {prepayment:N0}$ ({prepaymentPercent * 100}% от суммы) " +
                $"зачислена на ваш счёт.\n\n" +
                $"Заказ <b>{selectedOrder.address}</b> (категория <b>{catName}</b>) " +
                $"добавлен в <b>Активные заказы</b> и ожидает подготовки.";
        }

        if (confirmAcceptedOkButton != null)
        {
            confirmAcceptedOkButton.onClick.RemoveAllListeners();
            confirmAcceptedOkButton.onClick.AddListener(() =>
            {
                if (confirmAcceptedWindow != null)
                    confirmAcceptedWindow.SetActive(false);

                AddInactiveOrder();
            });
        }

        SaveManager.SaveGame(GameManager.Instance.Data, GameManager.Instance.CurrentSlot);
    }

    // === Добавление в activeOrders (как неактивный, до подготовки) ===
    private void AddInactiveOrder()
    {
        var activeOrder = new OrderData
        {
            orderName = selectedOrder.address,
            totalDays = selectedOrder.duration,
            progress = 0,
            currentWorkers = 0,
            maxWorkers = 3,
            payment = selectedOrder.payment,
            workersMood = 100,
            isActive = false,
            isCompleted = false
        };

        GameManager.Instance.Data.activeOrders.Add(activeOrder);

        currentOrders.Remove(selectedOrder);
        CategoryState.currentOrderIds.Remove(selectedOrder.id);
        ShowOrdersList();

        if (rightColumn != null)
            rightColumn.SetActive(false);

        SaveManager.SaveGame(GameManager.Instance.Data, GameManager.Instance.CurrentSlot);
    }

    private void OnDeclineOrder()
    {
        if (selectedOrder == null) return;

        currentOrders.Remove(selectedOrder);
        CategoryState.currentOrderIds.Remove(selectedOrder.id);
        ShowOrdersList();

        if (rightColumn != null)
            rightColumn.SetActive(false);
    }

    private void ClosePanel()
    {
        if (categoryPanel != null)
            categoryPanel.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.IsUIOpen = false;

        Time.timeScale = 1f;
    }

    private void BackToCategories()
    {
        if (categoryPanel != null)
            categoryPanel.SetActive(false);

        if (ordersPanel != null)
            ordersPanel.SetActive(true);
    }

    /// <summary>
    /// Вызывается контроллером времени раз в день (аналогично SuburbOrdersPanelUI.OnNewGameDay).
    /// </summary>
    public void OnNewGameDay(int currentDay)
    {
        if (CategoryState.daysUntilNewOrders > 0)
        {
            CategoryState.daysUntilNewOrders--;
            UpdateNextOrdersText();

            if (CategoryState.daysUntilNewOrders == 0)
            {
                CategoryState.currentOrderIds.Clear();
                CategoryState.daysUntilNewOrders = 30;
                GenerateOrders();
                ShowOrdersList();
                Debug.Log($"📦 Заказы автоматически обновлены для категории [{category}]");
            }
        }
    }
}
