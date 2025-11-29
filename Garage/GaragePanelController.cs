using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Контроллер панели «книжки» в гараже:
/// - Вкладки: Все / Рабочая / Транспортные
/// - Двухколоночный разворот (левая/правая) без скролла
/// - Постраничная навигация стрелками
/// - Ремонт/Продажа в ячейках
/// </summary>
public class GaragePanelController : MonoBehaviour
{
    [SerializeField] private Button repairAllButton;

    [Header("Связи")]
    [SerializeField] private Transform leftColumnParent;
    [SerializeField] private Transform rightColumnParent;
    [SerializeField] private GameObject slotPrefab;

    [Header("Навигация")]
    [SerializeField] private Button prevPageButton;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private TMP_Text pageLabel;

    [Header("Фильтры (вкладки)")]
    [SerializeField] private Button allButton;          // «ВСЕ» слева
    [SerializeField] private Button workingTabButton;   // «РАБОЧАЯ»
    [SerializeField] private Button transportTabButton; // «ТРАНСПОРТНЫЕ»

    [Header("Крестик (закрыть)")]
    public UnityEvent onCloseRequested; // Привяжи возвращение к диалогу Эдди
    [SerializeField] private Button closeButton;

    [Header("Настройка разворота")]
    [SerializeField] private int itemsPerLeftColumn = 5;
    [SerializeField] private int itemsPerRightColumn = 5;


    // Источник данных
    [SerializeField] private VehicleDatabase vehicleDatabase;

    private List<VehicleData> _source = new List<VehicleData>(); // отфильтрованный список
    private int _pageIndex = 0;
    private VehicleGroup? _currentGroup = null; // null = «ВСЕ»

    private GameManager gameManager;
    public static GaragePanelController Instance;

    private GameData Data
    {
        get
        {
            if (gameManager == null)
            {
#if UNITY_2023_1_OR_NEWER
                gameManager = Object.FindFirstObjectByType<GameManager>();
#else
                gameManager = Object.FindObjectOfType<GameManager>();
#endif
            }
            return gameManager != null ? gameManager.CurrentGame : null;
        }
    }

    private int PageCapacity => itemsPerLeftColumn + itemsPerRightColumn;

    private void Awake()
    {
        // Навешиваем кнопки
        if (allButton) allButton.onClick.AddListener(() => { _currentGroup = null; Rebuild(); });
        if (workingTabButton) workingTabButton.onClick.AddListener(() => { _currentGroup = VehicleGroup.Working; Rebuild(); });
        if (transportTabButton) transportTabButton.onClick.AddListener(() => { _currentGroup = VehicleGroup.Transport; Rebuild(); });

        if (prevPageButton) prevPageButton.onClick.AddListener(OnPrevPage);
        if (nextPageButton) nextPageButton.onClick.AddListener(OnNextPage);

        if (closeButton) closeButton.onClick.AddListener(() => onCloseRequested?.Invoke());
    }

    private void OnEnable()
    {
        Instance = this;

        Data?.ClampAllVehicleHP();

        _pageIndex = 0;
        _currentGroup = null;
        Rebuild();
    }

    private void OnDisable()
    {
        if (Instance == this)
            Instance = null;
    }

    // ==== Публичные действия из ячеек ====

    /// <summary> Запуск ремонта по дням (без мгновенного восстановления). </summary>
    public void StartRepair(VehicleData v)
    {
        if (Data == null || v == null) return;

        // Уже в ремонте — выходим
        if (v.isUnderRepair)
            return;

        // Нельзя ремонтировать полностью исправную технику
        if (v.condition >= 100f)
            return;

        // === 1. Расчёт стоимости ремонта ===
        float damage = 100f - v.condition;      // напр. 25% урона
        float damageUnits = damage / 10f;       // каждые 10% = 1 единица
        float repairCostFloat = v.price * 0.07f * damageUnits; // 7% от цены * units
        int repairCost = Mathf.RoundToInt(repairCostFloat);

        // === 2. Проверка денег ===
        if (!Data.SpendMoney(repairCost))
        {
            HUDController.Instance?.ShowToast("Недостаточно средств для ремонта!");
            return;
        }

        // === 3. Запуск ремонта ===
        int days = Mathf.Max(1, Mathf.CeilToInt(damage * 0.5f));   // 10% = 5 дней
        v.isUnderRepair = true;
        v.repairDaysLeft = days;
        v.inGarage = false;

        // === 4. Обновляем деньги на HUD ===
        HUDController.Instance?.UpdateMoney(Data.money);
        FindFirstObjectByType<MoneyDisplay>()?.UpdateMoney();

        SaveManager.SaveGame(GameManager.Instance.CurrentGame, GameManager.Instance.CurrentSlot);

        // === 5. Сообщение игроку ===
        HUDController.Instance?.ShowToast(
            $"🔧 '{v.name}': ремонт {days} дн. Стоимость: {repairCost:N0}$"
        );

        // === 6. Обновление UI конкретного слота ===
        RefreshSingleSlot(v);
    }




    /// <summary> Подтверждения тут нет — панель подтверждения можешь добавить отдельно. </summary>
    public void TrySell(VehicleData v)
    {
        if (Data == null || v == null) return;

        float pricePercent = 75f - (100f - v.condition) * 0.5f;
        pricePercent = Mathf.Clamp(pricePercent, 25f, 75f);

        int salePrice = Mathf.RoundToInt(v.price * (pricePercent / 100f));

        Data.AddMoney(salePrice);

        Data.ownedVehicles.RemoveAll(x => x.uniqueId == v.uniqueId);

        // ⭐ Обновляем HUD (если он включён)
        HUDController.Instance?.UpdateMoney(Data.money);

        // ⭐ Обновляем MoneyDisplay в гараже (всегда)
        FindFirstObjectByType<MoneyDisplay>()?.UpdateMoney();

        SaveManager.SaveGame(GameManager.Instance.CurrentGame, GameManager.Instance.CurrentSlot);

        RefreshSingleSlot(v);
    }

    public int CalculateTotalRepairCost()
    {
        if (Data == null || Data.ownedVehicles == null)
            return 0;

        int total = 0;

        foreach (var v in Data.ownedVehicles)
        {
            if (v == null) continue;
            if (v.condition >= 100f) continue;
            if (v.isUnderRepair) continue;

            float damage = 100f - v.condition;
            float units = damage / 10f;
            float cost = v.price * 0.07f * units;

            total += Mathf.RoundToInt(cost);
        }

        return total;
    }

    public void OnRepairAllClicked()
    {
        int totalCost = CalculateTotalRepairCost();

        if (totalCost <= 0)
        {
            HUDController.Instance?.ShowToast("Нет техники, требующей ремонта.");
            return;
        }

        if (ConfirmRepairAllUI.Instance == null)
        {
            Debug.LogError("❌ ConfirmRepairAllUI.Instance == null — добавь ConfirmRepairAllPanel на сцену и привяжи скрипт.");
            return;
        }

        ConfirmRepairAllUI.Instance.Show(this, totalCost);
    }

    public void RepairAllConfirmed(int totalCost)
    {
        if (Data == null || Data.ownedVehicles == null)
            return;

        if (!Data.SpendMoney(totalCost))
        {
            HUDController.Instance?.ShowToast("Недостаточно средств для ремонта всей техники!");
            return;
        }

        int count = 0;

        foreach (var v in Data.ownedVehicles)
        {
            if (v == null) continue;
            if (v.condition >= 100f) continue;
            if (v.isUnderRepair) continue;

            float damage = 100f - v.condition;
            int days = Mathf.Max(1, Mathf.CeilToInt(damage * 0.5f));

            v.isUnderRepair = true;
            v.repairDaysLeft = days;
            v.inGarage = false;

            count++;
        }

        HUDController.Instance?.UpdateMoney(Data.money);
        FindFirstObjectByType<MoneyDisplay>()?.UpdateMoney();

        SaveManager.SaveGame(GameManager.Instance.CurrentGame, GameManager.Instance.CurrentSlot);

        HUDController.Instance?.ShowToast($"🔧 В ремонт отправлено: {count} ед. техники");

        Rebuild();
    }

    // ==== Построение страниц ====
    public void Rebuild()
    {
        _source.Clear();

        var data = Data;
        if (data == null || data.ownedVehicles == null)
        {
            ClearColumns();
            UpdatePagerUI();
            UpdateRepairAllButtonUI();   // <-- добавлено
            return;
        }

        // Фильтр по группе
        IEnumerable<VehicleData> query = data.ownedVehicles;

        if (_currentGroup.HasValue)
            query = query.Where(v => v.group == _currentGroup.Value);

        _source = query.ToList();

        // Корректировка номера страницы
        _pageIndex = Mathf.Clamp(
            _pageIndex,
            0,
            Mathf.Max(0, (_source.Count - 1) / PageCapacity)
        );

        RebuildCurrentPageOnly();

        // 🔥 Обновляем кнопку "Отремонтировать всё"
        UpdateRepairAllButtonUI();
    }


    private void UpdateRepairAllButtonUI()
    {
        if (repairAllButton == null)
            return;

        // Считаем стоимость ремонта всей техники
        int totalCost = CalculateTotalRepairCost();

        // Если нечего ремонтировать → кнопка неактивна
        repairAllButton.interactable = totalCost > 0;

        // Обновляем текст кнопки
        var txt = repairAllButton.GetComponentInChildren<TMP_Text>();
        if (txt != null)
        {
            if (totalCost > 0)
                txt.text = "Отремонтировать всё";
            else
                txt.text = "Все машины исправны";
        }
    }


    private void RebuildCurrentPageOnly()
    {
        ClearColumns();

        if (_source == null || _source.Count == 0)
        {
            UpdatePagerUI();
            return;
        }

        int startIndex = _pageIndex * PageCapacity;
        int endIndex = Mathf.Min(startIndex + PageCapacity, _source.Count);

        int leftCount = Mathf.Min(itemsPerLeftColumn, endIndex - startIndex);
        int rightCount = Mathf.Max(0, endIndex - startIndex - leftCount);

        for (int i = 0; i < leftCount; i++)
        {
            var v = _source[startIndex + i];
            CreateSlot(leftColumnParent, v);
        }

        for (int i = 0; i < rightCount; i++)
        {
            var v = _source[startIndex + leftCount + i];
            CreateSlot(rightColumnParent, v);
        }

        UpdatePagerUI();
    }

    private void ClearColumns()
    {
        if (leftColumnParent != null)
        {
            for (int i = leftColumnParent.childCount - 1; i >= 0; i--)
                Destroy(leftColumnParent.GetChild(i).gameObject);
        }

        if (rightColumnParent != null)
        {
            for (int i = rightColumnParent.childCount - 1; i >= 0; i--)
                Destroy(rightColumnParent.GetChild(i).gameObject);
        }
    }

    private void CreateSlot(Transform parent, VehicleData v)
    {
        if (slotPrefab == null || parent == null || v == null) return;

        var go = Instantiate(slotPrefab, parent);
        var slot = go.GetComponent<VehicleSlotUI>();
        if (slot != null)
            slot.Bind(v, this);
        else
            Debug.LogWarning("На префабе slotPrefab нет компонента VehicleSlotUI!");
    }

    private void UpdatePagerUI()
    {
        int totalPages = Mathf.Max(1, (_source.Count + PageCapacity - 1) / PageCapacity);

        if (pageLabel)
            pageLabel.text = $"{_pageIndex + 1}/{totalPages}";

        if (prevPageButton) prevPageButton.interactable = _pageIndex > 0;
        if (nextPageButton) nextPageButton.interactable = _pageIndex < totalPages - 1;
    }

    private void OnPrevPage()
    {
        _pageIndex = Mathf.Max(0, _pageIndex - 1);
        RebuildCurrentPageOnly();
    }

    private void OnNextPage()
    {
        int totalPages = Mathf.Max(1, (_source.Count + PageCapacity - 1) / PageCapacity);
        _pageIndex = Mathf.Min(totalPages - 1, _pageIndex + 1);
        RebuildCurrentPageOnly();
    }

    public void RefreshSingleSlot(VehicleData v)
    {
        var slots = GetComponentsInChildren<VehicleSlotUI>(true);

        foreach (var slot in slots)
        {
            // сравниваем по uniqueId, а не по ссылке на объект
            if (slot.Data != null && slot.Data.uniqueId == v.uniqueId)
            {
                // МАШИНЫ УЖЕ НЕТ В СПИСКЕ → УДАЛЯЕМ СЛОТ
                if (!Data.ownedVehicles.Any(x => x.uniqueId == v.uniqueId))
                {
                    Destroy(slot.gameObject);
                    Rebuild(); // важный момент!
                    return;
                }

                // Если она есть, просто обновляем UI
                slot.Bind(v, this);
                return;
            }
        }

        // Если слот не найден — перестроим страницу
        Rebuild();
    }

    public int CalculateSalePrice(VehicleData v)
    {
        float pricePercent = 75f - (100f - v.condition) * 0.5f;
        pricePercent = Mathf.Clamp(pricePercent, 25f, 75f);
        return Mathf.RoundToInt(v.price * (pricePercent / 100f));
    }


}
