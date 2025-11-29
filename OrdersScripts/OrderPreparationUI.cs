using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

/// <summary>
/// Панель подготовки заказа перед стартом строительства.
/// ✅ 7 ThreeStepSelector
/// ✅ 3 Toggle страховки
/// ✅ Dropdown бригад + динамический вывод состава бригады в "окне рабочих"
/// ✅ Предупреждение, если не укладываемся в срок
/// ✅ Совместимость с OrdersPanelUI: Open(...), OpenFromActive(...), SetupOrder(...)
/// ❗ Без рефлексии — прямой доступ к GameManager.Instance.Data.allBrigades и WorkerData.professionId
/// ✅ Новый слайдер "Заказ материалов" (materialDeliverySelector) — влияет на время/прибыль/материалы
/// ✅ SliderEquipment (техника): аккурат/норма/аренда
/// </summary>
public class OrderPreparationUI : MonoBehaviour
{
    private bool isInitialized = false; // флаг, чтобы не применять уменьшение при первом открытии

    // ===== Входные данные заказа =====
    [Header("Данные заказа (вход)")]
    [HideInInspector] private string orderId;
    [HideInInspector] private string address;
    [HideInInspector] private Sprite photo;
    [HideInInspector, TextArea(2, 8)] private string description;
    [HideInInspector, TextArea(2, 8)] private string baseRequirementsText;

    [Tooltip("Оплата по контракту (брутто), без модификаторов этого окна.")]
    [HideInInspector] private int basePayment = 10000;

    [Tooltip("Срок по контракту (дни), от заказчика.")]
    [HideInInspector] private int limitDays = 20;

    [Tooltip("Базовая продолжительность строительства без модификаторов.")]
    [HideInInspector] private int baseDurationDays = 20;

    [Tooltip("Текущее настроение выбранной бригады (0..100).")]
    [HideInInspector, Range(0, 100)] private int brigadeMood = 70;

    // Храним структуру требований (редактируемые копии)
    private List<RequiredWorker> currentRequiredWorkers = new List<RequiredWorker>();
    private List<RequiredVehicle> currentRequiredVehicles = new List<RequiredVehicle>();
    private List<RequiredMaterial> currentRequiredMaterials = new List<RequiredMaterial>();

    // ===== UI: левая колонка =====
    [Header("UI: левая колонка (карточка)")]
    [SerializeField] private TMP_Text addressText;
    [SerializeField] private Image photoImage;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private GameObject requirementsTextContainer;
    private TMP_Text requirementsText;

    [Header("UI: нижние блоки слева")]
    [SerializeField] private Toggle insuranceTechToggle;
    [SerializeField] private Toggle insuranceWorkersToggle;
    [SerializeField] private Toggle insuranceIncidentsToggle;
    [SerializeField] private TMP_Dropdown brigadeDropdown;

    // 🔹 Текущий заказ (для читабельности)
    private SuburbOrderData currentOrderInfo;

    // ===== Селекторы =====
    [Header("Selectors")]
    [SerializeField] private ThreeStepSelector materialDeliverySelector; // доставка материалов
    [SerializeField] private ThreeStepSelector workHoursSelector;
    [SerializeField] private ThreeStepSelector workerCountSelector;
    [SerializeField] private ThreeStepSelector equipmentSelector;    // техника
    [SerializeField] private ThreeStepSelector materialsSelector;
    [SerializeField] private ThreeStepSelector controlSelector;
    [SerializeField] private ThreeStepSelector workerPaySelector;

    // ===== UI: правая колонка (итог) =====
    [Header("UI: правая колонка (итог)")]
    [SerializeField] private TMP_Text qualityValueText;
    [SerializeField] private TMP_Text qualityFactorsText;
    [SerializeField] private TMP_Text moodDeltaText;
    [SerializeField] private TMP_Text moodFactorsText;
    [SerializeField] private TMP_Text timeLimitText;
    [SerializeField] private TMP_Text timePlannedText;
    [SerializeField] private TMP_Text profitNetText;
    [SerializeField] private TMP_Text requirementsNotesText;

    [Header("UI: старт и подтверждение")]
    [SerializeField] private Button startButton;

    [Header("Навигация")]
    [SerializeField] private Button backButton;

    [Header("Проверка ресурсов перед стартом")]
    [SerializeField] private Transform resourceCheckContainer;
    [SerializeField] private GameObject resourceRowPrefab;

    [Header("Анимация печати")]
    [SerializeField] private Image stampImage;
    [SerializeField] private float stampFadeDuration = 0.6f;

    // состояния
    private int selectedWorkHours, selectedWorkerCount, selectedEquipment, selectedMaterials, selectedControl, selectedWorkerPay;
    private int selectedMaterialDelivery; // 0/1/2
    private float durationMul = 1f; private int qualityDelta = 0; private int moodDelta = 0; private float profitMul = 1f;
    private bool note_MaterialsUp20, note_WorkersHalf, note_LockAllWorkers, note_EquipmentMinus50, note_EquipmentPlus2, note_TransportRequired;

    // ✅ новые примечания по технике (не трогаем старые флаги, чтобы ничего не сломать)
    private bool note_EquipmentCareful, note_EquipmentRental;

    public event Action<OrderPreparationResult> OnConfirm;

    // 💾 ОРИГИНАЛЬНАЯ БАЗА из OrdersDatabase (фиксируется ОДИН РАЗ при открытии)
    private Dictionary<string, int> originalWorkerNeeds = new Dictionary<string, int>();

    // ⚙️ Предыдущее положение слайдера (лево/центр/право) по профессии
    private Dictionary<string, int> previousWorkerState = new Dictionary<string, int>();

    // 💾 Оригинальные материалы для расчётов (фиксируются при открытии)
    private Dictionary<string, int> originalRequiredMaterials = new Dictionary<string, int>();

    // Бригады
    private int selectedBrigadeIndex = 0;

    private System.Random random = new System.Random();

    // ====== СЛУЖЕБНОЕ: единая точка управления состоянием кнопки Start ======
    private void SetStartButtonState(bool enabled)
    {
        if (!startButton) return;

        startButton.interactable = enabled;

        var cg = startButton.GetComponent<CanvasGroup>();
        if (!cg) cg = startButton.gameObject.AddComponent<CanvasGroup>();

        cg.interactable = enabled;
        cg.blocksRaycasts = enabled;
        cg.alpha = enabled ? 1f : 0.5f;
    }

    // ====== LIFECYCLE ======
    private void Awake()
    {
        if (timeLimitText) timeLimitText.text = $"Времени на проект осталось: {limitDays} дн.";

        if (requirementsText == null)
        {
            var scrollRoot = GameObject.Find("RequirementsText");
            if (scrollRoot != null)
                requirementsText = scrollRoot.GetComponentInChildren<TMP_Text>();
            else
                Debug.LogWarning("⚠ Не найден объект 'RequirementsText' для requirementsText в OrderPreparationUI");
        }

        if (!startButton)
        {
            // На случай, если поле не проставлено в инспекторе
            startButton = GetComponentsInChildren<Button>(true)
                .FirstOrDefault(b => b.name.ToLower().Contains("start"));
        }

        if (startButton)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartButtonPressed);
            // Полное выключение до загрузки заказа
            SetStartButtonState(false);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
            var cg = backButton.GetComponent<CanvasGroup>() ?? backButton.gameObject.AddComponent<CanvasGroup>();
            cg.interactable = true; cg.blocksRaycasts = true; cg.ignoreParentGroups = true;
        }

        // Подписки на селекторы
        HookSelector(workHoursSelector, v => { selectedWorkHours = v; Recalculate(); ValidateCanStart(); });
        HookSelector(workerCountSelector, v => { selectedWorkerCount = v; Recalculate(); RefreshResourceCheckPanel(); ValidateCanStart(); });
        HookSelector(equipmentSelector, v => { selectedEquipment = v; Recalculate(); RefreshResourceCheckPanel(); ValidateCanStart(); }); // техника
        HookSelector(materialsSelector, v => { selectedMaterials = v; Recalculate(); ValidateCanStart(); });
        HookSelector(controlSelector, v => { selectedControl = v; Recalculate(); ValidateCanStart(); });
        HookSelector(workerPaySelector, v => { selectedWorkerPay = v; Recalculate(); ValidateCanStart(); });

        // Доставка материалов
        HookSelector(materialDeliverySelector, v => { selectedMaterialDelivery = v; Recalculate(); ValidateCanStart(); });

        if (insuranceTechToggle) insuranceTechToggle.onValueChanged.AddListener(_ => { Recalculate(); ValidateCanStart(); });
        if (insuranceWorkersToggle) insuranceWorkersToggle.onValueChanged.AddListener(_ => { Recalculate(); ValidateCanStart(); });
        if (insuranceIncidentsToggle) insuranceIncidentsToggle.onValueChanged.AddListener(_ => { Recalculate(); ValidateCanStart(); });

        // По умолчанию центр
        ForceCenterIfNull(workHoursSelector, ref selectedWorkHours);
        ForceCenterIfNull(workerCountSelector, ref selectedWorkerCount);
        ForceCenterIfNull(equipmentSelector, ref selectedEquipment);
        ForceCenterIfNull(materialsSelector, ref selectedMaterials);
        ForceCenterIfNull(controlSelector, ref selectedControl);
        ForceCenterIfNull(workerPaySelector, ref selectedWorkerPay);
        ForceCenterIfNull(materialDeliverySelector, ref selectedMaterialDelivery);

        // В Awake данные могут ещё не подгрузиться — но пробуем
        PopulateBrigadeDropdown();
        if (brigadeDropdown)
        {
            brigadeDropdown.onValueChanged.RemoveAllListeners();
            brigadeDropdown.onValueChanged.AddListener(OnBrigadeSelected);
        }

        // Не включаем кнопку автоматически до проверки всех условий
        ValidateCanStart(false);           // только расчёт, без изменения кнопки
        SetStartButtonState(false);        // визуально и логически — выключено
    }

    private void OnEnable()
    {
        // 🧩 Сбрасываем все страховки при каждом открытии панели
        if (insuranceTechToggle != null)
        {
            insuranceTechToggle.isOn = false;
            insuranceTechToggle.onValueChanged?.Invoke(false);
        }

        if (insuranceWorkersToggle != null)
        {
            insuranceWorkersToggle.isOn = false;
            insuranceWorkersToggle.onValueChanged?.Invoke(false);
        }

        if (insuranceIncidentsToggle != null)
        {
            insuranceIncidentsToggle.isOn = false;
            insuranceIncidentsToggle.onValueChanged?.Invoke(false);
        }

        // После сброса пересчитываем всё заново
        Recalculate();
        ValidateCanStart(false);

        // Выключаем кнопку "Старт" до проверки условий
        SetStartButtonState(false);
    }


    private void HookSelector(ThreeStepSelector sel, Action<int> onChange)
    {
        if (!sel) return;
        sel.OnValueChanged -= onChange;
        sel.OnValueChanged += onChange;
    }

    private void ForceCenterIfNull(ThreeStepSelector sel, ref int backingField)
    {
        if (sel == null)
        {
            Debug.LogWarning($"⚠ Не назначен селектор в {name}");
            return;
        }

        backingField = 1;
        if (!TrySetSelectorInstant(sel, 1))
        {
            if (isActiveAndEnabled && gameObject.activeInHierarchy)
                StartCoroutine(InitSelectorNextFrame(sel, 1));
        }
    }

    private bool TrySetSelectorInstant(ThreeStepSelector sel, int value)
    {
        try { sel.SetIndexInstant(value); return true; }
        catch { return false; }
    }

    private IEnumerator InitSelectorNextFrame(ThreeStepSelector sel, int value)
    {
        yield return null;
        if (sel != null)
        {
            try { sel.SetIndexInstant(value); }
            catch (Exception ex) { Debug.LogWarning($"⚠ Не удалось проинициализировать селектор {sel.name}: {ex.Message}"); }
        }
    }

    private void OnBackClicked()
    {
        OrdersPanelUI.Instance?.ReturnToOrdersMenu();
        gameObject.SetActive(false);
    }

    // ====== ОТКРЫТИЕ ПАНЕЛИ ======
    public void Open(SuburbOrderData info)
    {
        if (info == null) { Debug.LogWarning("⚠ OrderPreparationUI.Open: info == null"); return; }

        currentOrderInfo = info;


        // 🔥 фиксируем оригинальные требования из OrdersDatabase
        previousWorkerState.Clear();
        originalWorkerNeeds.Clear();
        if (info.requiredWorkers != null)
            foreach (var r in info.requiredWorkers)
                originalWorkerNeeds[r.workerId] = r.count;

        // 🔥 глубокие копии (редактируем в UI, базу не трогаем)
        currentRequiredWorkers = new List<RequiredWorker>();
        if (info.requiredWorkers != null)
        {
            foreach (var rw in info.requiredWorkers)
                currentRequiredWorkers.Add(new RequiredWorker { workerId = rw.workerId, count = rw.count });
        }

        currentRequiredVehicles = new List<RequiredVehicle>();
        if (info.requiredVehicles != null)
        {
            foreach (var v in info.requiredVehicles)
                currentRequiredVehicles.Add(new RequiredVehicle { vehicleId = v.vehicleId, count = v.count });
        }

        // ✅ ГЛУБОКАЯ КОПИЯ материалов
        currentRequiredMaterials = new List<RequiredMaterial>();
        if (info.requiredMaterials != null)
        {
            foreach (var m in info.requiredMaterials)
            {
                currentRequiredMaterials.Add(new RequiredMaterial { materialId = m.materialId, count = m.count });
            }
        }

        // 💾 Сохраняем оригинальные количества материалов для расчётов
        originalRequiredMaterials.Clear();
        foreach (var m in currentRequiredMaterials)
            originalRequiredMaterials[m.materialId] = m.count;

        // Карточка + расчёт
        SetupOrder(info.id, info.address, null, info.description, "", info.payment, info.duration, info.duration, 70);

        // Обновить бригады (если Awake был раньше загрузки данных)
        PopulateBrigadeDropdown();
        SyncBrigadeMoodFromSelected();

        UpdateBrigadeWorkersUI();

        gameObject.SetActive(true);
        isInitialized = true;
        Debug.Log($"📋 Открыт заказ {info.id}: работников = {currentRequiredWorkers.Count}, техники = {currentRequiredVehicles.Count}, материалов = {currentRequiredMaterials.Count}");

        // 🚫 Проверяем ресурсы сразу при открытии панели
        RefreshResourceCheckPanel();
        bool canStart = ValidateCanStart(true);
        SetStartButtonState(canStart);

        if (resourceCheckContainer is RectTransform rt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

    }

    public void OpenFromActive(OrderData activeOrder, SuburbOrderData info)
    {
        if (info == null) { Debug.LogWarning("⚠ OrderPreparationUI.OpenFromActive: info == null"); return; }

        currentOrderInfo = info;

        // 🔥 фиксируем оригинальные требования для РАБОЧИХ
        previousWorkerState.Clear();
        originalWorkerNeeds.Clear();
        if (info.requiredWorkers != null)
            foreach (var r in info.requiredWorkers)
                originalWorkerNeeds[r.workerId] = r.count;

        // 🔥 глубокие копии (редактируем в UI, базу не трогаем)
        currentRequiredWorkers = new List<RequiredWorker>();
        if (info.requiredWorkers != null)
            foreach (var rw in info.requiredWorkers)
                currentRequiredWorkers.Add(new RequiredWorker { workerId = rw.workerId, count = rw.count });

        currentRequiredVehicles = new List<RequiredVehicle>();
        if (info.requiredVehicles != null)
            foreach (var v in info.requiredVehicles)
                currentRequiredVehicles.Add(new RequiredVehicle { vehicleId = v.vehicleId, count = v.count });

        // ✅ ГЛУБОКАЯ КОПИЯ МАТЕРИАЛОВ + фиксация «оригинала»
        currentRequiredMaterials = new List<RequiredMaterial>();
        if (info.requiredMaterials != null)
            foreach (var m in info.requiredMaterials)
                currentRequiredMaterials.Add(new RequiredMaterial { materialId = m.materialId, count = m.count });

        originalRequiredMaterials.Clear();
        foreach (var m in currentRequiredMaterials)
            originalRequiredMaterials[m.materialId] = m.count;


        // карточка слева + первичный пересчёт/отрисовка
        SetupOrder(info.id, info.address, null, info.description, "", info.payment, info.duration, info.duration, 70);

        // Обновить бригады
        PopulateBrigadeDropdown();
        SyncBrigadeMoodFromSelected();

        gameObject.SetActive(true);
        if (workerCountSelector != null) StartCoroutine(SetWorkerSelectorCenter());
        isInitialized = true;

        RefreshResourceCheckPanel();
        bool canStart = ValidateCanStart(true);
        SetStartButtonState(canStart);
        Debug.Log("✅ OrderPreparationUI.OpenFromActive — панель успешно открыта");
    }

    private IEnumerator RefreshScrollLayoutNextFrame(RectTransform rt)
    {
        // ждём конец кадра, чтобы Unity успел пересчитать контент
        yield return new WaitForEndOfFrame();

        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

        var scroll = rt.GetComponentInParent<ScrollRect>();
        if (scroll != null)
        {
            // фиксируем позицию прокрутки (чтобы не дёргался)
            scroll.normalizedPosition = new Vector2(0, 1);
        }
    }


    private string BuildRequirementsText(List<RequiredWorker> workers, List<RequiredVehicle> vehicles, List<RequiredMaterial> materials)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<b>ТРЕБОВАНИЯ:</b>");

        if (workers != null && workers.Count > 0)
        {
            sb.AppendLine("<b>Рабочие:</b>");
            foreach (var rw in workers)
                sb.AppendLine($"• {ProfessionIdToName(rw.workerId)} × {Mathf.Max(1, rw.count)}");
        }

        if (vehicles != null && vehicles.Count > 0)
        {
            sb.AppendLine("<b>Техника:</b>");
            foreach (var rv in vehicles)
            {
                string name = VehicleDatabase.Instance?.GetVehicleNameById(rv.vehicleId) ?? rv.vehicleId;
                sb.AppendLine($"• {name} × {Mathf.Max(1, rv.count)}");
            }
        }

        if (materials != null && materials.Count > 0)
        {
            sb.AppendLine("<b>Материалы:</b>");
            foreach (var rm in materials)
            {
                string name = GameManager.Instance.GetMaterialNameById(rm.materialId);
                sb.AppendLine($"• {name} × {Mathf.Max(1, rm.count)}");
            }
        }

        return sb.ToString();
    }

    /// <summary> Установка карточки заказа (левая колонка) </summary>
    private void SetupOrder(string id, string addr, Sprite photoSprite, string descr, string reqText, int payment, int duration, int baseDur, int mood)
    {
        orderId = id;
        address = addr;
        photo = photoSprite;
        description = descr ?? "";
        baseRequirementsText = reqText ?? "";

        basePayment = payment;
        limitDays = duration;
        baseDurationDays = baseDur;
        brigadeMood = mood;

        if (addressText) addressText.text = address;
        if (photoImage) photoImage.sprite = photo;
        if (descriptionText) descriptionText.text = description;
        if (requirementsText) requirementsText.text = baseRequirementsText;

        timeLimitText?.SetText($"Времени на проект осталось: {limitDays} дн.");

        // дефолт — все слайдеры в центре
        selectedWorkHours = 1; selectedWorkerCount = 1;
        selectedEquipment = 1; selectedMaterials = 1; selectedControl = 1; selectedWorkerPay = 1;
        selectedMaterialDelivery = 1;

        Recalculate();
        RefreshResourceCheckPanel();

        // Первичная проверка после заполнения карточки
        bool canStart = ValidateCanStart(true);
        SetStartButtonState(canStart);
    }

    // ====== БРИГАДЫ ======
    private void PopulateBrigadeDropdown()
    {
        if (!brigadeDropdown) return;
        brigadeDropdown.ClearOptions();

        var data = GameManager.Instance?.Data;
        var brigades = data?.allBrigades;

        if (brigades == null || brigades.Count == 0)
        {
            brigadeDropdown.AddOptions(new List<string> { "Нет доступных бригад" });
            selectedBrigadeIndex = 0;
            return;
        }

        // 🔹 Фильтруем только свободные (не isWorking)
        var freeBrigades = brigades.Where(b => b != null && !b.isWorking).ToList();

        if (freeBrigades.Count == 0)
        {
            brigadeDropdown.AddOptions(new List<string> { "❌ Все бригады заняты" });
            brigadeDropdown.interactable = false;
            selectedBrigadeIndex = 0;
            Debug.Log("⚠ Нет свободных бригад для назначения на заказ");
            return;
        }

        // 🔹 Формируем отображаемый список
        var options = freeBrigades.Select((b, i) =>
        {
            int mood = Mathf.Clamp(b?.mood ?? 70, 0, 100);
            string name = string.IsNullOrEmpty(b?.name) ? $"Бригада №{i + 1}" : b.name;
            return $"{name} (настроение {mood}%)";
        }).ToList();

        brigadeDropdown.AddOptions(options);
        brigadeDropdown.interactable = true;

        // ⚙️ Слушатель выбора
        brigadeDropdown.onValueChanged.RemoveAllListeners();
        brigadeDropdown.onValueChanged.AddListener(index =>
        {
            selectedBrigadeIndex = Mathf.Clamp(index, 0, freeBrigades.Count - 1);
            var selected = freeBrigades[selectedBrigadeIndex];

            // Находим индекс в общем списке
            int globalIndex = data.allBrigades.FindIndex(b => b.id == selected.id);
            if (globalIndex >= 0)
                selectedBrigadeIndex = globalIndex;

            foreach (var b in data.allBrigades)
                if (b != null) b.isSelected = false;

            selected.isSelected = true;

            SyncBrigadeMoodFromSelected();
            UpdateBrigadeWorkersUI();
            Recalculate();
            ValidateCanStart();
        });

        // 🔹 Устанавливаем первую свободную
        selectedBrigadeIndex = 0;
        var first = freeBrigades.First();
        int firstGlobal = data.allBrigades.FindIndex(b => b.id == first.id);
        if (firstGlobal >= 0) selectedBrigadeIndex = firstGlobal;

        SyncBrigadeMoodFromSelected();
        Debug.Log($"✅ Список бригад обновлён. Свободно: {freeBrigades.Count}, занято: {brigades.Count - freeBrigades.Count}");
    }


    private void OnBrigadeSelected(int index)
    {
        selectedBrigadeIndex = index;

        var brigades = GameManager.Instance?.Data?.allBrigades;
        if (brigades != null && brigades.Count > 0)
        {
            foreach (var b in brigades) if (b != null) b.isSelected = false;
            if (index >= 0 && index < brigades.Count && brigades[index] != null) brigades[index].isSelected = true;
        }

        SyncBrigadeMoodFromSelected();
        UpdateBrigadeWorkersUI();
        Recalculate();
        ValidateCanStart();
    }

    private BrigadeData GetSelectedBrigade()
    {
        var brigades = GameManager.Instance?.Data?.allBrigades;
        if (brigades == null || brigades.Count == 0) return null;
        int idx = Mathf.Clamp(selectedBrigadeIndex, 0, brigades.Count - 1);
        return brigades[idx];
    }

    private void SyncBrigadeMoodFromSelected()
    {
        var brigade = GetSelectedBrigade();
        if (brigade != null) brigadeMood = Mathf.Clamp(brigade.mood, 0, 100);
    }

    private void UpdateBrigadeWorkersUI() { /* вывод состава бригады, при необходимости */ }

    // ====== ПОДСЧЁТ РЕСУРСОВ ======
    private int GetAvailableWorkerCount(string professionId)
    {
        int total = 0;
        var brigades = GameManager.Instance?.Data?.allBrigades;
        if (brigades == null) return 0;

        foreach (var brigade in brigades)
        {
            if (brigade?.workers == null) continue;
            if (brigade.isWorking) continue; // 🟡 Пропускаем занятые бригады

            total += brigade.workers.Count(w => w != null && w.professionId == professionId);
        }
        return total;
    }


    private IEnumerator SetWorkerSelectorCenter()
    {
        yield return null;
        if (workerCountSelector != null)
        {
            workerCountSelector.SetIndexInstant(1);
            Debug.Log("✅ WorkerCountSelector установлен в центр");
        }
    }

    private int GetAvailableVehicleCount(string vehicleId)
    {
        return GameManager.Instance.Data.ownedVehicles
            .Count(v => v != null
                     && v.id == vehicleId
                     && v.inGarage
                     && !v.isUnderRepair);   // ← главное!
    }



    private string ProfessionIdToName(string id)
    {
        return id switch
        {
            "p01_carpenter" => "Плотник",
            "p02_painter" => "Маляр",
            "p03_electrician" => "Электрик",
            "p04_engineer" => "Инженер",
            "p05_welder" => "Сварщик",
            "p06_laborer" => "Разнорабочий",
            "p07_plumber" => "Сантехник",
            "p08_concreter" => "Бетонщик",
            "p09_surveyor" => "Геодезист",
            "p10_roofer" => "Кровельщик",
            "p07_craneoperator" => "Крановщик",
            _ => id
        };
    }

    // ====== ПАНЕЛЬ ПРОВЕРОК «ТРЕБУЕТСЯ / ЕСТЬ» ======
    private bool isRefreshing = false;

    private void RefreshResourceCheckPanel()
    {
        if (isRefreshing) return;
        isRefreshing = true;

        // 🔧 Вместо удаления — просто скрываем все старые строки
        foreach (Transform child in resourceCheckContainer)
            child.gameObject.SetActive(false);

        // 🔹 Локальная функция для получения или создания строки
        GameObject GetRowObject()
        {
            foreach (Transform child in resourceCheckContainer)
            {
                if (!child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(true);

                    // 🧹 Очистим текст внутри перед повторным использованием
                    var texts = child.GetComponentsInChildren<TMP_Text>(true);
                    foreach (var t in texts)
                        t.text = "";

                    return child.gameObject;
                }
            }

            // если нет свободных — создаём новую
            var newRow = Instantiate(resourceRowPrefab, resourceCheckContainer);
            newRow.SetActive(true);
            return newRow;
        }

        void CreateCategory(string title)
        {
            var rowObj = GetRowObject();
            var texts = rowObj.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 4)
            {
                string color = title switch
                {
                    "ТЕХНИКА" => "#87CEFA",
                    "МАТЕРИАЛЫ" => "#90EE90",
                    _ => "#FFD700"
                };
                texts[0].text = $"<b><color={color}>{title}</color></b>";
                texts[1].text = texts[2].text = texts[3].text = "";
            }
        }

        void CreateRow(string type, string name, int have, int need)
        {
            var rowObj = GetRowObject();
            var texts = rowObj.GetComponentsInChildren<TMP_Text>();
            bool ok = have >= need;
            if (texts.Length >= 4)
            {
                texts[0].text = type;
                texts[1].text = name;
                texts[2].text = $"{have}/{need}";
                texts[3].text = ok ? "✅" : "❌";
                texts[3].color = ok ? Color.green : Color.red;
            }
        }

        // ----- РАБОЧИЕ -----
        if (currentRequiredWorkers != null && currentRequiredWorkers.Count > 0)
        {
            CreateCategory("РАБОЧИЕ");

            foreach (var req in currentRequiredWorkers)
            {
                // база строго из OrdersDatabase, зафиксированная при открытии
                int baseNeed = originalWorkerNeeds.ContainsKey(req.workerId)
                    ? originalWorkerNeeds[req.workerId]
                    : req.count;

                int have = GetAvailableWorkerCount(req.workerId);

                if (!previousWorkerState.ContainsKey(req.workerId))
                    previousWorkerState[req.workerId] = 1; // центр

                int previous = previousWorkerState[req.workerId];
                int current = selectedWorkerCount;

                // Лево: применять -20% только при переходе 1→0 (или 2→0), 1 раз
                if (isInitialized && current == 0 && previous != 0)
                {
                    int newNeed = Mathf.Max(1, Mathf.FloorToInt(baseNeed * 0.8f));
                    req.count = newNeed;
                    previousWorkerState[req.workerId] = 0;
                    Debug.Log($"🟡 Сокращение штата для {req.workerId}: {baseNeed} → {newNeed}");
                }
                // Центр: вернуть ровно базу
                else if (isInitialized && current == 1 && previous != 1)
                {
                    req.count = baseNeed;
                    previousWorkerState[req.workerId] = 1;
                    Debug.Log($"🟢 Возврат базового числа рабочих {req.workerId}: {baseNeed}");
                }
                // Право: добор временных (только отображение have)
                else if (isInitialized && current == 2 && previous != 2)
                {
                    if (have < baseNeed) have = baseNeed;
                    previousWorkerState[req.workerId] = 2;
                }

                CreateRow("Рабочие", ProfessionIdToName(req.workerId), have, req.count);
            }
        }

        // ----- ТЕХНИКА -----
        if (currentRequiredVehicles != null && currentRequiredVehicles.Count > 0)
        {
            CreateCategory("ТЕХНИКА");

            // Если аренда активна, добавляем 1 к доступным
            bool rentalMode = selectedEquipment == 2;

            foreach (var req in currentRequiredVehicles)
            {
                int need = Mathf.Max(1, req.count);
                int have = GetAvailableVehicleCount(req.vehicleId);

                if (rentalMode && have < need)
                {
                    have += 1; // временная арендованная техника
                }

                string vName = VehicleDatabase.Instance?.GetVehicleNameById(req.vehicleId) ?? req.vehicleId;
                CreateRow("Техника", vName, have, need);
            }
        }

        // ---------- МАТЕРИАЛЫ ----------
        if (currentRequiredMaterials != null && currentRequiredMaterials.Count > 0)
        {
            CreateCategory("МАТЕРИАЛЫ");

            foreach (var req in currentRequiredMaterials)
            {
                int need = Mathf.Max(1, req.count);
                int have = GameManager.Instance.Data.GetResourceQuantity(req.materialId);
                string mName = GameManager.Instance.GetMaterialNameById(req.materialId);
                CreateRow("Материалы", mName, have, need);
            }
        }

        bool canStart = ValidateCanStart(true);
        SetStartButtonState(canStart);

        // 🔒 Переключаем скролл после перестройки списка
        UpdateScrollLock();

        isRefreshing = false;

    }

    private bool ValidateCanStart(bool updateButton = true)
    {
        bool ok = true;

        // Рабочие
        foreach (var req in currentRequiredWorkers)
        {
            int need = Mathf.Max(1, req.count);
            if (GetAvailableWorkerCount(req.workerId) < need) { ok = false; break; }
        }

        // Техника — учитываем аренду 1 ед. при selectedEquipment == 2
        if (ok && currentRequiredVehicles != null && currentRequiredVehicles.Count > 0)
        {
            bool rentalMode = selectedEquipment == 2;

            foreach (var req in currentRequiredVehicles)
            {
                int need = Mathf.Max(1, req.count);
                int have = GetAvailableVehicleCount(req.vehicleId);

                // аренда добавляет 1 виртуальную технику
                if (rentalMode && have < need)
                    have += 1;

                if (have < need)
                {
                    ok = false;
                    break;
                }
            }
        }

        // Материалы
        if (ok)
            foreach (var req in currentRequiredMaterials)
            {
                int need = Mathf.Max(1, req.count);
                if (GameManager.Instance.Data.GetResourceQuantity(req.materialId) < need) { ok = false; break; }
            }

        if (updateButton) SetStartButtonState(ok);
        return ok;
    }



    // ====== ПЕРЕСЧЁТ КАЧЕСТВА / НАСТРОЕНИЯ / РЕСУРСОВ / ВРЕМЕНИ / ПРИБЫЛИ ======
    private void Recalculate()
    {
        // === ⏸ Временно выключаем Layout-группы, чтобы панель не мигала ===
        VerticalLayoutGroup[] vGroups = GetComponentsInChildren<VerticalLayoutGroup>(true);
        ContentSizeFitter[] fitters = GetComponentsInChildren<ContentSizeFitter>(true);

        foreach (var g in vGroups) g.enabled = false;
        foreach (var f in fitters) f.enabled = false;


        // Сброс итогов и флагов
        durationMul = 1f; qualityDelta = 0; moodDelta = 0; profitMul = 1f;
        note_MaterialsUp20 = note_WorkersHalf = note_LockAllWorkers = note_EquipmentMinus50 = note_EquipmentPlus2 = note_TransportRequired = false;
        note_EquipmentCareful = note_EquipmentRental = false;

        var qSb = new System.Text.StringBuilder("<b>КАЧЕСТВО</b>\n");
        var mSb = new System.Text.StringBuilder("<b>НАСТРОЕНИЕ</b>\n");
        var rSb = new System.Text.StringBuilder("<b>РЕСУРСЫ</b>\n");
        var reqNotes = new System.Text.StringBuilder();

        string Plus(float v, string t) => $"<color=#4CAF50>+{v}%</color> {t}\n";
        string Minus(float v, string t) => $"<color=#E53935>-{v}%</color> {t}\n";
        string PlusRaw(int v, string t) => $"<color=#4CAF50>+{v}</color> {t}\n";
        string MinusRaw(int v, string t) => $"<color=#E53935>-{v}</color> {t}\n";

        // === ⏰ Рабочие часы ===
        switch (selectedWorkHours)
        {
            case 0:
                durationMul *= 1.30f; moodDelta += 5; profitMul *= 0.95f;
                mSb.Append(PlusRaw(5, "Спокойный график"));
                rSb.Append(Minus(5, "Прибыль"));
                break;
            case 1:
                mSb.Append("Стандартный график\n");
                break;
            case 2:
                durationMul *= 0.85f; moodDelta -= 5; profitMul *= 0.90f;
                mSb.Append(MinusRaw(5, "Переработки"));
                rSb.Append(Minus(10, "Прибыль"));
                break;
        }

        // === 👷 Количество рабочих ===
        int brigadeCount = GetCurrentBrigadeWorkersCount();
        int requiredCount = (currentRequiredWorkers != null && currentRequiredWorkers.Count > 0)
            ? currentRequiredWorkers.Sum(w => Mathf.Max(1, w.count))
            : 0;

        if (selectedWorkerCount == 0)
        {
            durationMul *= 1.30f;   // медленнее
            qualityDelta -= 5;
            profitMul *= 1.15f;     // экономия
        }
        else if (selectedWorkerCount == 2)
        {
            durationMul *= 0.90f;   // быстрее
            if (brigadeCount >= requiredCount)
            {
                int extra = brigadeCount - requiredCount;
                qualityDelta += Mathf.Clamp(extra * 2, 2, 10);
            }
            else
            {
                // временные рабочие (догоняем отсутствующих)
                note_WorkersHalf = true;
                profitMul *= 0.5f;
            }
        }

        // Блокировка правого шага <50%
        if (workerCountSelector != null)
        {
            float fill = requiredCount > 0 ? (float)brigadeCount / requiredCount : 0f;
            if (fill < 0.5f) workerCountSelector.SetRightLocked(true);
            else if (fill < 1f) workerCountSelector.SetRightLocked(false);
            else workerCountSelector.SetRightLocked(true);
            workerCountSelector.SetLeftLocked(false);
        }

        // === 🚜 Техника (SliderEquipment) ===
        switch (selectedEquipment)
        {
            case 0: // аккуратно
                durationMul *= 1.15f; // медленнее
                note_EquipmentCareful = true;
                qSb.Append(Plus(5, "Аккуратное использование техники"));
                break;

            case 1: // стандарт
                qSb.Append("Стандартное использование техники\n");
                break;

            case 2: // аренда: −35% прибыли, закрывает 1 ед. техники
                profitMul *= 0.65f;
                note_EquipmentRental = true;
                rSb.Append(Minus(35, "Аренда техники"));
                reqNotes.Append("• Одна единица требуемой техники будет заменена арендой при старте\n");
                break;
        }

        // === 🚜 Блокировка правого шага техники (новое) ===
        if (equipmentSelector != null && currentRequiredVehicles != null && currentRequiredVehicles.Count > 0)
        {
            int totalNeed = currentRequiredVehicles.Sum(v => Mathf.Max(1, v.count));
            int totalHave = 0;
            foreach (var req in currentRequiredVehicles)
                totalHave += GetAvailableVehicleCount(req.vehicleId);

            float fill = totalNeed > 0 ? (float)totalHave / totalNeed : 0f;
            Debug.Log($"[ТЕХНИКА] have={totalHave}, need={totalNeed}, fill={fill}");

            // если техники достаточно — блокируем аренду (правая сторона)
            if (fill >= 1f)
            {
                equipmentSelector.SetRightLocked(true);
                Debug.Log("🔒 Equipment slider locked (вся техника есть — аренда недоступна)");
            }
            else
            {
                equipmentSelector.SetRightLocked(false);
                Debug.Log("🔓 Equipment slider unlocked (техники не хватает — аренда возможна)");
            }

            // левый (аккуратный режим) не блокируем никогда
            equipmentSelector.SetLeftLocked(false);
        }




        // === 🧱 Материалы: Политика + Доставка ===
        // Запоминаем «оригинал» материалов при первом вызове
        if (originalRequiredMaterials.Count == 0 && currentRequiredMaterials != null)
        {
            originalRequiredMaterials.Clear();
            foreach (var m in currentRequiredMaterials)
                originalRequiredMaterials[m.materialId] = Mathf.Max(1, m.count);
        }

        float materialsFactor = 1f;

        // Политика материалов
        switch (selectedMaterials)
        {
            case 0: // экономия
                materialsFactor *= 0.80f; // −20% расхода
                qualityDelta -= 20;
                profitMul *= 1.10f;
                qSb.Append(Minus(20, "Экономия материалов"));
                rSb.Append(Plus(10, "Экономия на материалах"));
                break;
            case 1: // норма
                qSb.Append("Стандартные материалы\n");
                break;
            case 2: // излишек
                materialsFactor *= 1.25f; // +25% расхода
                qualityDelta += 15;
                profitMul *= 0.85f;
                qSb.Append(Plus(15, "Излишек материалов"));
                rSb.Append(Minus(15, "Излишек материалов"));
                break;
        }

        // Доставка материалов
        switch (selectedMaterialDelivery)
        {
            case 0: // медленная
                durationMul *= 0.60f;   // −40% времени
                profitMul *= 0.85f;     // −15% прибыли
                materialsFactor *= 0.70f; // −30% расхода
                rSb.Append(Minus(15, "Медленная доставка"));
                break;
            case 1: // стандарт
                rSb.Append("Стандартная доставка\n");
                break;
            case 2: // быстрая
                durationMul *= 0.80f;   // −20% времени
                profitMul *= 0.60f;     // −40% прибыли
                materialsFactor *= 0.45f; // −55% расхода
                rSb.Append(Minus(40, "Ускоренная доставка"));
                break;
        }

        // Применяем materialsFactor к текущему списку из «оригинала»
        if (currentRequiredMaterials != null && originalRequiredMaterials.Count > 0)
        {
            foreach (var req in currentRequiredMaterials)
            {
                if (originalRequiredMaterials.TryGetValue(req.materialId, out int baseCount))
                    req.count = Mathf.Max(1, Mathf.RoundToInt(baseCount * materialsFactor));
            }
            RefreshResourceCheckPanel();
            if (resourceCheckContainer is RectTransform rt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            UpdateScrollLock();
        }

        // === 🔎 Контроль качества ===
        switch (selectedControl)
        {
            case 0:
                durationMul *= 1.15f; // +15% срок
                moodDelta += 5;
                qSb.Append(Minus(10, "Слабый контроль качества"));
                mSb.Append(PlusRaw(5, "Слабый контроль"));
                break;
            case 1:
                qSb.Append("Стандартный контроль качества\n");
                break;
            case 2:
                durationMul *= 0.90f; // −10% срок
                moodDelta -= 5;
                qSb.Append(Plus(10, "Усиленный контроль качества"));
                mSb.Append(MinusRaw(5, "Строгий контроль"));
                break;
        }

        // === 💵 Оплата рабочим ===
        switch (selectedWorkerPay)
        {
            case 0:
                moodDelta -= 25; profitMul *= 1.15f;
                mSb.Append(MinusRaw(25, "Урезанная оплата"));
                rSb.Append(Plus(15, "Экономия на зарплате"));
                break;
            case 1:
                mSb.Append("Стандартная оплата\n");
                break;
            case 2:
                moodDelta += 10; profitMul *= 0.85f;
                mSb.Append(PlusRaw(10, "Повышенная оплата"));
                rSb.Append(Minus(15, "Повышенная оплата"));
                break;
        }

        // === 🛡️ Страховки ===
        if (insuranceTechToggle && insuranceTechToggle.isOn)
        {
            qualityDelta += 10; profitMul *= 0.85f;
            qSb.Append(Plus(10, "Страховка техники"));
            rSb.Append(Minus(15, "Страховка техники"));
        }
        if (insuranceWorkersToggle && insuranceWorkersToggle.isOn)
        {
            moodDelta += 5; profitMul *= 0.90f;
            mSb.Append(PlusRaw(5, "Страховка рабочих"));
            rSb.Append(Minus(10, "Страховка рабочих"));
        }
        if (insuranceIncidentsToggle && insuranceIncidentsToggle.isOn)
        {
            qSb.Append("Меньше риска происшествий\n");
        }

        // Низкое настроение бригады снижает качество
        if (brigadeMood < 40)
        {
            qualityDelta -= 35;
            qSb.Append(Minus(35, "Низкое настроение бригады"));
        }

        // === Итоговые значения и вывод ===
        int plannedDays = Mathf.Max(1, Mathf.RoundToInt(baseDurationDays * durationMul));
        int finalQuality = Mathf.Clamp(100 + qualityDelta, 0, 100);
        int finalMood = moodDelta;
        int netProfit = Mathf.RoundToInt(basePayment * profitMul);

        if (qualityValueText)
            qualityValueText.text = $"<b>Качество:</b> {finalQuality}%";

        // === Новое оформление настроения ===
        if (moodDeltaText)
        {
            int currentMood = brigadeMood;
            int plannedMood = Mathf.Clamp(currentMood + moodDelta, 0, 100);
            string sign = moodDelta > 0 ? "+" : (moodDelta < 0 ? "" : "");
            string deltaText = moodDelta != 0 ? $" ({sign}{moodDelta})" : "";
            moodDeltaText.text = $"<b>Настроение:</b> {currentMood}%{deltaText}";
        }

        if (timeLimitText) timeLimitText.text = $"<b>Время от заказчика:</b> {limitDays} дн.";
        if (timePlannedText)
        {
            int orig = baseDurationDays;
            string diff = plannedDays < orig ? $" (быстрее на {orig - plannedDays} дн.)"
                         : plannedDays > orig ? $" (дольше на {plannedDays - orig} дн.)" : "";
            timePlannedText.text = $"<b>Время для рабочих:</b> {plannedDays} дн.{diff}";
        }

        if (profitNetText) profitNetText.text = $"<b>Чистая прибыль:</b> ${netProfit:N0}";

        // Старые заметки (если где-то используются)
        var legacy = new System.Text.StringBuilder();
        if (note_MaterialsUp20) legacy.AppendLine("• Экономия на материалах до 20% применена");
        if (note_WorkersHalf) legacy.AppendLine("• Наняты временные рабочие (договор на объект)");
        if (note_LockAllWorkers) legacy.AppendLine("• Все рабочие будут заняты");
        if (note_EquipmentMinus50) legacy.AppendLine("• Оборудование снижено на 50%");
        if (note_EquipmentPlus2) legacy.AppendLine("• Добавлено +2 единицы оборудования");
        if (note_EquipmentCareful) legacy.AppendLine("• Техника: аккуратное использование (медленнее, износ −5% при старте)");
        if (note_EquipmentRental) legacy.AppendLine("• Техника: аренда (−35% прибыли, закроет 1 ед. техники при старте)");
        if (note_TransportRequired) legacy.AppendLine("• Требуется транспорт для доставки");

        if (qualityFactorsText) qualityFactorsText.text = qSb.ToString();
        if (moodFactorsText) moodFactorsText.text = mSb.ToString();
        if (requirementsNotesText) requirementsNotesText.text = rSb.ToString() + reqNotes.ToString() + legacy.ToString();

        // === ▶ Включаем Layout-группы обратно после пересчёта ===
        foreach (var g in vGroups)
            if (g != null) g.enabled = true;

        foreach (var f in fitters)
            if (f != null) f.enabled = true;

        if (TryGetComponent<RectTransform>(out var selfRect))
            LayoutRebuilder.ForceRebuildLayoutImmediate(selfRect);


    }

    private int GetCurrentBrigadeWorkersCount()
    {
        var allBrigades = GameManager.Instance?.Data?.allBrigades;
        if (allBrigades == null || allBrigades.Count == 0) return 0;

        if (selectedBrigadeIndex >= 0 && selectedBrigadeIndex < allBrigades.Count)
        {
            var brigade = allBrigades[selectedBrigadeIndex];
            if (brigade != null && brigade.workers != null)
                return brigade.workers.Count;
        }
        return 0;

    }

    private IEnumerator SmoothScrollFixNextFrame(RectTransform content)
    {
        // Ждём конец кадра, чтобы Unity завершил пересчёт layout
        yield return new WaitForEndOfFrame();

        var scroll = content.GetComponentInParent<ScrollRect>();
        Vector2 pos = scroll != null ? scroll.normalizedPosition : Vector2.up;

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        // Возвращаем скролл в исходное положение (чтобы не дёргался)
        if (scroll != null)
            scroll.normalizedPosition = pos;
    }

    private string DeltaTextFromSelector(int idx, int low, int mid, int high)
    {
        return idx switch
        {
            0 => low >= 0 ? $"+{low}" : $"{low}",
            1 => mid >= 0 ? $"+{mid}" : $"{mid}",
            2 => high >= 0 ? $"+{high}" : $"{high}",
            _ => "0"
        };
    }

    private void ApplyEquipmentEffects()
    {
        var data = GameManager.Instance.Data;
        if (data == null || data.ownedVehicles == null) return;

        // === 🚛 АРЕНДА ===
        if (selectedEquipment == 2)
        {
            bool addedVirtual = false;

            foreach (var req in currentRequiredVehicles)
            {
                int have = GetAvailableVehicleCount(req.vehicleId);
                int need = Mathf.Max(1, req.count);

                if (have < need)
                {
                    have += 1;
                    addedVirtual = true;
                    Debug.Log($"🚛 Аренда техники: добавлена виртуальная единица {req.vehicleId}.");
                }
            }

            if (addedVirtual)
                Debug.Log("✅ Аренда активна — недостающая техника покрыта временной арендой. Прибыль −35%, износ не применяется.");
        }
        // === 🚜 АККУРАТНОЕ / НОРМАЛЬНОЕ ИСПОЛЬЗОВАНИЕ ===
        else if (selectedEquipment == 0 || selectedEquipment == 1)
        {
            float loss = (selectedEquipment == 0) ? 5f : 10f;

            foreach (var req in currentRequiredVehicles)
            {
                int remaining = Mathf.Max(1, req.count);
                foreach (var v in data.ownedVehicles)
                {
                    if (v == null || v.id != req.vehicleId) continue;

                    v.condition = Mathf.Clamp(v.condition - loss, 0f, 100f);
                    remaining--;
                    if (remaining <= 0) break;
                }
            }

            Debug.Log($"🚜 Износ техники применён ({loss}% HP) для всех участвующих машин.");
        }

        // ====== УБРАТЬ ТЕХНИКУ ИЗ ГАРАЖА (ОТМЕТИТЬ КАК РАБОТАЮЩУЮ) ======
        foreach (var req in currentRequiredVehicles)
        {
            int need = Mathf.Max(1, req.count);

            foreach (var v in data.ownedVehicles)
            {
                if (v == null) continue;
                if (v.id != req.vehicleId) continue;
                if (v.isUnderRepair) continue;
                if (!v.inGarage) continue;

                v.inGarage = false;   // ← МАШИНА УЕХАЛА НА ЗАКАЗ

                need--;
                if (need <= 0) break;
            }
        }

        // 💾 Сохраняем изменения
        int slot = GameManager.Instance != null ? GameManager.Instance.CurrentSlot : 0;
        SaveManager.SaveGame(data, slot);
    }


    // ====== СТАРТ ЗАКАЗА ======
    private void OnStartButtonPressed()
    {
        // Проверяем, можно ли начать заказ
        if (!ValidateCanStart())
        {
            Debug.LogWarning("❌ Невозможно начать заказ: не хватает ресурсов, техники или работников.");
            SetStartButtonState(false);
            return;
        }

        // Если всё готово — рассчитываем план
        int planned = Mathf.CeilToInt(baseDurationDays * durationMul);

        // Запускаем заказ напрямую
        ConfirmAndSend(planned);
    }

    private void ConfirmAndSend(int planned)
    {
        int quality = Mathf.Clamp(70 + qualityDelta, 0, 100);
        int net = Mathf.RoundToInt(basePayment * profitMul);

        ApplyEquipmentEffects();

        var result = new OrderPreparationResult
        {
            orderId = orderId,
            address = address,

            selectedWorkHours = selectedWorkHours,
            selectedWorkerCount = selectedWorkerCount,
            selectedEquipment = selectedEquipment,
            selectedMaterials = selectedMaterials,
            selectedControl = selectedControl,
            selectedWorkerPay = selectedWorkerPay,

            insuranceTech = insuranceTechToggle && insuranceTechToggle.isOn,
            insuranceWorkers = insuranceWorkersToggle && insuranceWorkersToggle.isOn,
            insuranceIncidents = insuranceIncidentsToggle && insuranceIncidentsToggle.isOn,

            brigadeName = GetSelectedBrigade()?.name ?? "Без бригады",
            brigadeMood = brigadeMood,

            finalQualityPercent = quality,
            moodDelta = moodDelta,
            plannedDurationDays = planned,
            limitDays = limitDays,
            netProfit = net,

            note_MaterialsUp20 = note_MaterialsUp20,
            note_WorkersHalf = note_WorkersHalf,
            note_LockAllWorkers = note_LockAllWorkers,
            note_EquipmentMinus50 = note_EquipmentMinus50,
            note_EquipmentPlus2 = note_EquipmentPlus2,
            note_TransportRequired = note_TransportRequired
        };

        // === 🔹 АКТИВАЦИЯ БРИГАДЫ ===
        var selectedBrigade = GetSelectedBrigade();
        if (selectedBrigade != null)
        {
            var data = GameManager.Instance.Data;

            // ссылка из общего списка, чтобы не терялась
            var brigadeRef = data.allBrigades.FirstOrDefault(b => b.id == selectedBrigade.id) ?? selectedBrigade;
            var foreman = data.foremen.FirstOrDefault(f => f.id == brigadeRef.foremanId);

            // Проверяем доступность
            int activeCount = data.allBrigades.Count(b => b.foremanId == foreman?.id && b.isWorking);
            int capacity = Mathf.Max(1, (foreman?.extraBrigades ?? 0) + 1);

            if (activeCount >= capacity)
            {
                HUDController.Instance?.ShowToast($"❌ {foreman?.name ?? "Бригадир"} уже занят на максимум!");
                return;
            }

            // Помечаем как работающую
            brigadeRef.isWorking = true;
            brigadeRef.currentOrderId = orderId;

            // Если лимит исчерпан — бригадир занят
            if (foreman != null)
            {
                int activeNow = data.allBrigades.Count(b => b.foremanId == foreman.id && b.isWorking);
                if (activeNow >= capacity)
                    foreman.isBusy = true;
            }

            SaveManager.SaveGame(data, GameManager.Instance.CurrentSlot);
            Debug.Log($"🏗️ {brigadeRef.name} активна для заказа {orderId}");
        }

        // === Штамп и возврат ===
        if (stampImage != null && stampFadeDuration > 0f)
            StartCoroutine(Co_StampThenReturn(result));
        else
        {
            OnConfirm?.Invoke(result);
            OrdersPanelUI.Instance?.ReturnToOrdersMenu();
            gameObject.SetActive(false);
        }
    }
private IEnumerator Co_StampThenReturn(OrderPreparationResult result)
    {
        // показываем штамп
        if (stampImage != null)
        {
            stampImage.gameObject.SetActive(true);
            var canvasGroup = stampImage.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = stampImage.gameObject.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 0f;
            }

            float duration = 1.5f; // 🕓 длительность эффекта
            float t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / 0.5f); // плавное появление
                yield return null;
            }

            yield return new WaitForSeconds(1.2f); // штамп держится перед переходом
            canvasGroup.alpha = 0f;
            stampImage.gameObject.SetActive(false);
        }

        // вызываем подтверждение
        OnConfirm?.Invoke(result);
        OrdersPanelUI.Instance?.ReturnToOrdersMenu();
        gameObject.SetActive(false);
    }


    private IEnumerator ReenableLayoutsNextFrame(
    VerticalLayoutGroup[] groups,
    ContentSizeFitter[] fitters)
    {
        yield return new WaitForEndOfFrame();

        foreach (var g in groups)
            if (g != null) g.enabled = true;

        foreach (var f in fitters)
            if (f != null) f.enabled = true;

        // после включения можно принудительно перестроить корневой Layout
        if (TryGetComponent<RectTransform>(out var rt))
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    // ✅ Эффект печати — просто вспышка визуала
    // ✅ Эффект печати — плавное появление и исчезновение с лёгким "ударом"
    private IEnumerator Co_Stamp()
    {
        if (stampImage == null) yield break;

        // Делаем печать некликабельной
        stampImage.raycastTarget = false;

        // Если была активна с прошлого раза — сбрасываем
        stampImage.gameObject.SetActive(false);
        yield return null;

        // Активируем снова для нового эффекта
        stampImage.gameObject.SetActive(true);

        // Добавляем CanvasGroup для анимации прозрачности
        CanvasGroup cg = stampImage.GetComponent<CanvasGroup>();
        if (cg == null) cg = stampImage.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        RectTransform rt = stampImage.rectTransform;
        Vector3 originalScale = rt.localScale;

        // 🔹 Эффект "удара" штампом — увеличивается и сжимается с fade-in
        float t = 0f;
        float duration = 0.25f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float eased = Mathf.Sin((t / duration) * Mathf.PI * 0.5f);
            cg.alpha = Mathf.Lerp(0f, 1f, eased);
            rt.localScale = Vector3.Lerp(originalScale * 1.6f, originalScale, eased);
            yield return null;
        }

        cg.alpha = 1f;
        rt.localScale = originalScale;

        // 🕒 держим видимой 1.2 секунды
        yield return new WaitForSeconds(1.2f);

        // 🔸 Плавное исчезновение
        float fadeTime = 0.6f;
        t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, t / fadeTime);
            yield return null;
        }

        stampImage.gameObject.SetActive(false);
    }



    // ====== РЕЖИМ ПРОСМОТРА ======
    public void SetViewMode(bool viewOnly)
    {
        foreach (var selectable in GetComponentsInChildren<Selectable>(true))
        {
            if (selectable != null && selectable.gameObject.name.ToLower().Contains("back"))

            {
                selectable.interactable = true;
                continue;
            }
            if (selectable != null) selectable.interactable = !viewOnly;
        }

        foreach (var slider in GetComponentsInChildren<Slider>(true)) slider.interactable = !viewOnly;
        foreach (var dropdown in GetComponentsInChildren<TMP_Dropdown>(true)) dropdown.interactable = !viewOnly;
        foreach (var toggle in GetComponentsInChildren<Toggle>(true)) toggle.interactable = !viewOnly;

        var startBtn = GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name.ToLower().Contains("start"));
        if (startBtn != null) startBtn.gameObject.SetActive(!viewOnly);

        var backBtn = GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name.ToLower().Contains("back"));
        if (backBtn != null)
        {
            backBtn.interactable = true;
            var btnGroup = backBtn.GetComponent<CanvasGroup>() ?? backBtn.gameObject.AddComponent<CanvasGroup>();
            btnGroup.interactable = true; btnGroup.blocksRaycasts = true; btnGroup.ignoreParentGroups = true;
        }

        Debug.Log(viewOnly ? "🔒 Режим просмотра: всё заблокировано, кроме кнопки Назад" : "🟢 Режим редактирования активен");
    }

    private IEnumerator DelayedRecalculate()
    {
        yield return null;
        Recalculate();
        UpdateBrigadeWorkersUI();
        ValidateCanStart();
    }
    // ====== АВТО-БЛОКИРОВКА ПРОКРУТКИ ======
    private void UpdateScrollLock()
    {
        // Сначала пытаемся найти ScrollRect через контейнер, потом через сам текст
        ScrollRect scrollRect = null;

        if (requirementsTextContainer != null)
            scrollRect = requirementsTextContainer.GetComponentInParent<ScrollRect>();

        if (scrollRect == null && requirementsText != null)
            scrollRect = requirementsText.GetComponentInParent<ScrollRect>();

        if (scrollRect == null) return;

        RectTransform content = scrollRect.content;
        RectTransform viewport = scrollRect.viewport;
        if (content == null || viewport == null) return;

        // Если контент не выше вьюпорта — прокрутка не нужна
        bool needScroll = content.rect.height > viewport.rect.height + 1f;

        // Включаем/выключаем прокрутку и саму полосу
        scrollRect.vertical = needScroll;
        scrollRect.horizontal = false;
        if (scrollRect.verticalScrollbar != null)
            scrollRect.verticalScrollbar.gameObject.SetActive(needScroll);
    }


}

// ===== Результат настроек =====
[Serializable]
public class OrderPreparationResult
{
    [HideInInspector] public string orderId;
    [HideInInspector] public string address;

    public int selectedWorkHours;
    public int selectedWorkerCount;
    public int selectedEquipment;
    public int selectedMaterials;
    public int selectedControl;
    public int selectedWorkerPay;

    public bool insuranceTech;
    public bool insuranceWorkers;
    public bool insuranceIncidents;

    public string brigadeName;
    public int brigadeMood;

    public int finalQualityPercent;
    public int moodDelta;
    public int plannedDurationDays;
    public int limitDays;
    public int netProfit;

    public bool note_MaterialsUp20;
    public bool note_WorkersHalf;
    public bool note_LockAllWorkers;
    public bool note_EquipmentMinus50;
    public bool note_EquipmentPlus2;
    public bool note_TransportRequired;
}
