using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

/// <summary>
/// Элемент в списке активных заказов.
/// Показывает адрес, сроки, прогресс, настроение, прибыль, работников,
/// и автоматически обновляется каждый игровой день.
/// </summary>
public class OrderItemUI : MonoBehaviour
{
    [Header("UI элементы")]
    [SerializeField] private TMP_Text orderNameText;
    [SerializeField] private TMP_Text deadlineText;    // срок заказчика
    [SerializeField] private TMP_Text progressText;    // % выполнения
    [SerializeField] private TMP_Text daysLeftText;    // дни по плану игрока
    [SerializeField] private TMP_Text moodText;
    [SerializeField] private TMP_Text paymentText;
    [SerializeField] private TMP_Text workersText;

    [Header("Кнопки управления")]
    [SerializeField] private Button setupButton;
    [SerializeField] private Button infoButton;
    [SerializeField] private Button visitButton;

    private OrderData currentOrder;
    private OrdersPanelUI parentPanel;
    private OrderInfo linkedOrder;

    public OrderData CurrentOrder => currentOrder;

    // ==============================================================
    public void Setup(OrderData order, OrdersPanelUI panel)
    {
        currentOrder = order;
        parentPanel = panel;

        orderNameText.text = order.orderName;
        deadlineText.text = $"{order.totalDays} дн.";   // срок заказчика
        progressText.text = $"{GetProgressPercent(order)}%";
        daysLeftText.text = $"{Mathf.Max(0, order.remainingDays - order.daysPassed)} дн.";
        moodText.text = $"{order.workersMood}%";
        paymentText.text = $"{order.payment:N0}$";
        workersText.text = $"{order.currentWorkers}/{order.maxWorkers}";

        // Поиск описания в базе
        var db = Resources.Load<OrdersDatabase>("Databases/OrdersDatabase");
        if (db != null)
        {
            linkedOrder = db.suburbOrders.FirstOrDefault(o => o.address == order.orderName)
                ?? db.cityOrders.FirstOrDefault(o => o.address == order.orderName)
                ?? db.centerOrders.FirstOrDefault(o => o.address == order.orderName)
                ?? db.specialOrders.FirstOrDefault(o => o.address == order.orderName);
        }

        setupButton.onClick.RemoveAllListeners();
        infoButton.onClick.RemoveAllListeners();
        visitButton.onClick.RemoveAllListeners();

        setupButton.onClick.AddListener(OnSetupClicked);
        infoButton.onClick.AddListener(OnInfoClicked);
        visitButton.onClick.AddListener(OnVisitClicked);

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        bool started = currentOrder != null && currentOrder.isActive;
        if (setupButton) setupButton.gameObject.SetActive(!started);
        if (infoButton) infoButton.gameObject.SetActive(started);
        if (visitButton) visitButton.gameObject.SetActive(started);
    }

    private void OnSetupClicked()
    {
        if (currentOrder == null) return;
        // Открываем в режиме настройки (интерактив)
        OrdersPanelUI.Instance.OpenPreparation(currentOrder, viewOnly: false);
    }

    private void OnInfoClicked()
    {
        if (currentOrder == null) return;
        // Открываем в режиме просмотра (всё заблокировано)
        OrdersPanelUI.Instance.OpenPreparation(currentOrder, viewOnly: true);
    }

    // ==============================================================
    // Применение результата из окна подготовки
    public void ApplyPreparationResult(OrderPreparationResult result)
    {
        if (currentOrder == null || result == null) return;

        // === Данные заказа по результатам выбора игрока ===
        currentOrder.remainingDays = result.plannedDurationDays;  // план игрока
        currentOrder.totalDays = result.limitDays;                // контрактный срок
        currentOrder.payment = result.netProfit;
        currentOrder.workersMood = Mathf.Clamp(result.brigadeMood + result.moodDelta, 0, 100);

        // 🔹 Сохраняем связку с бригадой и плановое изменение настроения:
        currentOrder.moodDeltaPlanned = result.moodDelta;
        currentOrder.brigadeId = null;
        var data = GameManager.Instance?.Data;
        if (data != null && !string.IsNullOrEmpty(result.brigadeName))
        {
            var b = data.GetBrigadeByName(result.brigadeName);
            if (b != null) currentOrder.brigadeId = b.id;
        }

        // === Статусы ===
        currentOrder.isStarted = true;    // 🟢 теперь заказ считается начатым
        currentOrder.isActive = true;
        currentOrder.isCompleted = false;
        currentOrder.daysPassed = 0;
        currentOrder.progress = 0;

        // === Обновляем отображение ===
        if (deadlineText) deadlineText.text = $"{currentOrder.totalDays} дн.";
        if (daysLeftText) daysLeftText.text = $"{currentOrder.remainingDays} дн.";
        if (moodText) moodText.text = $"{currentOrder.workersMood}%";
        if (paymentText) paymentText.text = $"{currentOrder.payment:N0}$";
        if (progressText) progressText.text = "0%";
        if (workersText) workersText.text = $"{currentOrder.currentWorkers}/{currentOrder.maxWorkers}";

        // === Синхронизируем с GameData ===
        var gm = GameManager.Instance;
        if (gm != null && gm.Data != null)
        {
            var inData = gm.Data.activeOrders.FirstOrDefault(o => o.orderName == currentOrder.orderName);
            if (inData != null)
            {
                inData.remainingDays = currentOrder.remainingDays;
                inData.totalDays = currentOrder.totalDays;
                inData.payment = currentOrder.payment;
                inData.workersMood = currentOrder.workersMood;
                inData.brigadeId = currentOrder.brigadeId;
                inData.moodDeltaPlanned = currentOrder.moodDeltaPlanned;
                inData.isActive = true;
                inData.isStarted = true;     // 🟢 фиксируем и в сейве
                inData.isCompleted = false;
                inData.daysPassed = 0;
                inData.progress = 0;
            }
        }

        // === Обновляем кнопки ===
        SetActiveMode(); // 🔹 вместо UpdateButtons(), чтобы точно сменить "Настроить" → "Инфо/Визит"
        Debug.Log($"🏗️ Заказ '{currentOrder.orderName}' запущен. Кнопки обновлены.");
    }


    public void SetActiveMode()
    {
        if (setupButton != null) setupButton.gameObject.SetActive(false);
        if (infoButton != null) infoButton.gameObject.SetActive(true);
        if (visitButton != null) visitButton.gameObject.SetActive(true);
    }

    // ==============================================================
    // Автоматическое обновление каждый день
    public void UpdateProgressByTime(int daysPassed)
    {
        if (currentOrder == null || currentOrder.remainingDays <= 0) return;

        currentOrder.daysPassed = Mathf.Clamp(daysPassed, 0, currentOrder.remainingDays);

        float progress = Mathf.Clamp01((float)currentOrder.daysPassed / currentOrder.remainingDays);
        int percent = Mathf.RoundToInt(progress * 100);

        int remaining = Mathf.Max(0, currentOrder.remainingDays - currentOrder.daysPassed);
        daysLeftText.text = $"{remaining} дн.";

        if (currentOrder.daysPassed >= currentOrder.remainingDays)
        {
            currentOrder.isCompleted = true;
            progressText.text = "✅ Завершено";
        }
    }

    // ==============================================================
    private int GetProgressPercent(OrderData order)
    {
        if (order.remainingDays <= 0) return 0;
        float progress = Mathf.Clamp01((float)order.daysPassed / order.remainingDays);
        return Mathf.RoundToInt(progress * 100f);
    }

    private void OnVisitClicked()
    {
        if (currentOrder == null) return;
        Debug.Log($"🚧 Игрок посетил объект: {currentOrder.orderName}");
        // Здесь позже можно сделать переход к сцене объекта (если будет реализовано)
    }
}
