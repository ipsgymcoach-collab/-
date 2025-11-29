using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class SuburbOrdersPanelUI : MonoBehaviour
{
    [Header("Основные блоки")]
    [SerializeField] private GameObject suburbPanel;
    [SerializeField] private GameObject ordersPanel;
    [SerializeField] private Button backButton;
    [SerializeField] private Button backToCategoriesButton;

    [Header("Уровень категории")]
    [SerializeField] private Button categoryLevelButton;

    [Header("Левая колонка")]
    [SerializeField] private Transform ordersContainer;
    [SerializeField] private GameObject orderItemPrefab;
    [SerializeField] private TMP_Text nextOrdersText;
    [SerializeField] private TMP_Text hintText;

    [Header("Иконки сложности")]
    [SerializeField] private Image easyIcon;
    [SerializeField] private Image mediumIcon;
    [SerializeField] private Image hardIcon;

    [Header("Правая колонка (детали)")]
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
    [SerializeField] private float prepaymentPercent = 0.2f;

    private GameData.OrdersCategoryState SuburbState => GameManager.Instance.Data.suburbOrdersState;

    private List<SuburbOrderData> currentOrders = new List<SuburbOrderData>();
    private SuburbOrderData selectedOrder;

    public static SuburbOrdersPanelUI Instance;

    private void Awake() => Instance = this;

    private void Start()
    {
        backButton.onClick.AddListener(ClosePanel);
        backToCategoriesButton.onClick.AddListener(BackToCategories);

        acceptButton.onClick.AddListener(OnAcceptOrder);
        declineButton.onClick.AddListener(OnDeclineOrder);

        if (categoryLevelButton != null)
            categoryLevelButton.onClick.AddListener(OnCategoryLevelButton);

        confirmAcceptedWindow.SetActive(false);
        rightColumn.SetActive(false);

        GenerateOrders();
        ShowOrdersList();
        UpdateNextOrdersText();
    }

    private void OnCategoryLevelButton()
    {
        Debug.Log("📊 Открываю панель уровня категории (ещё не реализовано)");
    }

    private void UpdateNextOrdersText()
    {
        nextOrdersText.text =
            $"Новые заказы будут через {SuburbState.daysUntilNewOrders} дней";
    }

    // --------------------------------------------------------------------
    //                      ГЕНЕРАЦИЯ ЗАКАЗОВ
    // --------------------------------------------------------------------
    private void GenerateOrders()
    {
        currentOrders.Clear();

        OrdersDatabase db = Resources.Load<OrdersDatabase>("Databases/OrdersDatabase");

        if (db == null)
        {
            Debug.LogError("❌ Не найден OrdersDatabase в Resources/Databases/");
            return;
        }

        // Есть сохранённый список заказов?
        if (SuburbState.currentOrderIds != null &&
            SuburbState.currentOrderIds.Count > 0)
        {
            foreach (string id in SuburbState.currentOrderIds)
            {
                var info = db.suburbOrders.FirstOrDefault(o => o.id == id);
                if (info != null)
                    currentOrders.Add(Convert(info));
            }
            return;
        }

        // Создаём новый список
        List<OrderInfo> available = new List<OrderInfo>(db.suburbOrders);

        // Убираем уже завершённые
        foreach (string completedId in GameManager.Instance.Data.completedOrders)
            available.RemoveAll(o => o.id == completedId);

        int orderSlots = 4;
        List<string> picked = new();

        for (int i = 0; i < orderSlots && available.Count > 0; i++)
        {
            int index = Random.Range(0, available.Count);
            var info = available[index];

            available.RemoveAt(index);

            currentOrders.Add(Convert(info));
            picked.Add(info.id);
        }

        SuburbState.currentOrderIds = picked;
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
        foreach (Transform child in ordersContainer)
            Destroy(child.gameObject);

        foreach (var order in currentOrders)
        {
            var obj = Instantiate(orderItemPrefab, ordersContainer);
            var ui = obj.GetComponent<SuburbOrderItemUI>();
            ui.Setup(order, this);
        }

        hintText.text =
            "Для большего количества заказов наймите больше менеджеров проекта";
    }

    // --------------------------------------------------------------------
    //                     ОТОБРАЖЕНИЕ ДЕТАЛЕЙ ЗАКАЗА
    // --------------------------------------------------------------------
    public void ShowOrderDetails(SuburbOrderData order)
    {
        selectedOrder = order;
        rightColumn.SetActive(true);

        addressText.text = order.address;
        descriptionText.text = order.description;
        paymentText.text = $"💵 Оплата: {order.payment:N0}$";

        int prepay = Mathf.RoundToInt(order.payment * prepaymentPercent);
        prepaymentText.text = $"💰 Предоплата: {prepay:N0}$";

        deadlineText.text = $"Срок: {order.duration} дн.";
        qualityText.text = "Мин. качество: 80%";

        easyIcon.gameObject.SetActive(order.difficulty == 1);
        mediumIcon.gameObject.SetActive(order.difficulty == 2);
        hardIcon.gameObject.SetActive(order.difficulty == 3);

        workersText.text = "Рабочие:\n";
        foreach (var req in order.requiredWorkers)
        {
            string prof = GameManager.Instance.GetProfessionNameById(req.workerId);

            int have = GameManager.Instance.Data.hiredWorkers.Count(w => w.professionId == req.workerId);
            int free = GameManager.Instance.Data.hiredWorkers.Count(w => w.professionId == req.workerId && !w.isBusy);

            string state = have == 0 ? "(нет)" :
                           free == 0 ? "(заняты)" :
                           $"({free} свободных)";

            workersText.text += $"• {prof}: {have}/{req.count} {state}\n";
        }

        vehiclesText.text = "Техника:\n";
        foreach (var req in order.requiredVehicles)
        {
            string name = VehicleDatabase.Instance?.GetVehicleNameById(req.vehicleId) ?? req.vehicleId;

            int have = GameManager.Instance.Data.ownedVehicles.Count(v => v.id == req.vehicleId);
            int free = GameManager.Instance.Data.ownedVehicles
                .Count(v => v.id == req.vehicleId && v.inGarage && !v.isUnderRepair);

            string state = have == 0 ? "(нет)" :
                           free == 0 ? "(занята/в ремонте)" :
                           $"({free} свободных)";

            vehiclesText.text += $"• {name}: {have}/{req.count} {state}\n";
        }

        materialsText.text = "Материалы:\n";
        foreach (var req in order.requiredMaterials)
        {
            int have = GameManager.Instance.Data.GetResourceQuantity(req.materialId);
            string name = GameManager.Instance.GetMaterialNameById(req.materialId);

            materialsText.text += $"• {name}: {have}/{req.count}\n";
        }

        photoImage.color = Color.white;
    }

    // --------------------------------------------------------------------
    //                     ПРИНЯТИЕ ЗАКАЗА
    // --------------------------------------------------------------------
    private void OnAcceptOrder()
    {
        if (selectedOrder == null) return;

        int prepay = Mathf.RoundToInt(selectedOrder.payment * prepaymentPercent);

        GameManager.Instance.Data.money += prepay;
        HUDController.Instance.UpdateMoney(GameManager.Instance.Data.money);
        HUDController.Instance.ShowToast($"💰 Получена предоплата: +{prepay:N0}$");

        confirmAcceptedWindow.SetActive(true);

        confirmAcceptedText.text =
            $"💰 Предоплата {prepay:N0}$ зачислена.\n\n" +
            $"Заказ <b>{selectedOrder.address}</b> добавлен в активные.";

        confirmAcceptedOkButton.onClick.RemoveAllListeners();
        confirmAcceptedOkButton.onClick.AddListener(() =>
        {
            confirmAcceptedWindow.SetActive(false);
            AddInactiveOrder();
        });

        SaveManager.SaveGame(GameManager.Instance.Data, GameManager.Instance.CurrentSlot);
    }

    private void AddInactiveOrder()
    {
        var newOrder = new OrderData
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

        GameManager.Instance.Data.activeOrders.Add(newOrder);

        currentOrders.Remove(selectedOrder);
        SuburbState.currentOrderIds.Remove(selectedOrder.id);

        ShowOrdersList();
        rightColumn.SetActive(false);

        SaveManager.SaveGame(GameManager.Instance.Data, GameManager.Instance.CurrentSlot);
    }

    private void OnDeclineOrder()
    {
        if (selectedOrder == null) return;

        currentOrders.Remove(selectedOrder);
        SuburbState.currentOrderIds.Remove(selectedOrder.id);

        ShowOrdersList();
        rightColumn.SetActive(false);
    }

    // --------------------------------------------------------------------
    //                        НАВИГАЦИЯ
    // --------------------------------------------------------------------
    private void ClosePanel()
    {
        suburbPanel.SetActive(false);
        GameManager.Instance.IsUIOpen = false;
        Time.timeScale = 1f;
    }

    private void BackToCategories()
    {
        suburbPanel.SetActive(false);
        ordersPanel.SetActive(true);
    }

    // --------------------------------------------------------------------
    //                         ОБНОВЛЕНИЕ РАЗ В ДЕНЬ
    // --------------------------------------------------------------------
    public void OnNewGameDay(int currentDay)
    {
        if (SuburbState.daysUntilNewOrders > 0)
        {
            SuburbState.daysUntilNewOrders--;
            UpdateNextOrdersText();

            if (SuburbState.daysUntilNewOrders == 0)
            {
                SuburbState.currentOrderIds.Clear();
                SuburbState.daysUntilNewOrders = 30;

                GenerateOrders();
                ShowOrdersList();

                Debug.Log("📦 Заказы пригородной категории обновлены автоматически.");
            }
        }
    }
}

[System.Serializable]
public class SuburbOrderData
{
    public string id;
    public string address;
    public string description;
    public int payment;
    public int duration;
    public int difficulty;
    public List<RequiredWorker> requiredWorkers;
    public List<RequiredVehicle> requiredVehicles;
    public List<RequiredMaterial> requiredMaterials;
}
