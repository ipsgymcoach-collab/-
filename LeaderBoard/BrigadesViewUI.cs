using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class BrigadePanelUI : MonoBehaviour
{
    public static BrigadePanelUI Instance;

    [Header("Информация о бригадире")]
    [SerializeField] private Image foremanPortrait;
    [SerializeField] private TMP_Dropdown brigadeDropdown;
    [SerializeField] private TMP_Text workerCountText;
    [SerializeField] private TMP_Text completedOrdersText;

    [Header("Списки работников")]
    [SerializeField] private Transform freeWorkersContainer;
    [SerializeField] private Transform brigadeWorkersContainer;
    [SerializeField] private GameObject workerRowPrefab;

    [Header("Кнопки перевода работников")]
    [SerializeField] private Button addToBrigadeButton;
    [SerializeField] private Button removeFromBrigadeButton;

    private ForemanData currentForeman;
    private BrigadeData currentBrigade;

    private List<WorkerRowUI> selectedFreeWorkers = new List<WorkerRowUI>();
    private List<WorkerRowUI> selectedBrigadeWorkers = new List<WorkerRowUI>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (addToBrigadeButton != null)
            addToBrigadeButton.onClick.AddListener(AddSelectedWorkerToBrigade);

        if (removeFromBrigadeButton != null)
            removeFromBrigadeButton.onClick.AddListener(RemoveSelectedWorkerFromBrigade);
    }

    /// <summary>
    /// Вызывается, когда вкладка "Бригады" становится активной
    /// (BrigadesView.SetActive(true) → OnEnable)
    /// </summary>
    private void OnEnable()
    {
        // Если данных игры ещё нет – ничего не делаем
        if (GameManager.Instance == null || GameManager.Instance.Data == null)
            return;

        InitializeBrigades();
    }

    /// <summary>
    /// Публичный вход – если когда-то нужно открыть на конкретного бригадира.
    /// Теперь НЕ включает/выключает панель, только грузит данные.
    /// </summary>
    public void Open(ForemanData foreman)
    {
        currentForeman = foreman ?? GameManager.Instance?.Data?.foremen?.FirstOrDefault(f => f.isHired);
        if (currentForeman == null) return;

        InitializeBrigades();

        if (currentForeman.brigades != null && currentForeman.brigades.Count > 0)
            LoadBrigade(currentForeman.brigades[0]);
    }

    public void RefreshBrigadeList()
    {
        InitializeBrigades();
    }

    // ------------------------------------------------------------------
    // Вся твоя старая логика – ниже без изменений, кроме удаления панели
    // ------------------------------------------------------------------

    private void InitializeBrigades()
    {
        var data = GameManager.Instance.Data;
        if (data == null) return;

        // 🔄 Синхронизация статусов бригад по активным заказам
        foreach (var o in data.activeOrders)
        {
            if (!string.IsNullOrEmpty(o.brigadeName))
            {
                var activeBrigade = data.allBrigades.FirstOrDefault(b => b.name == o.brigadeName);
                if (activeBrigade != null)
                    activeBrigade.isWorking = o.isStarted && !o.isCompleted;
            }
        }

        // 🧹 Убираем дубликаты бригад
        data.allBrigades = data.allBrigades
            .Where(b => b != null)
            .GroupBy(b => b.id)
            .Select(g => g.First())
            .ToList();

        // 👷 Обработка бригадиров
        foreach (var f in data.foremen.Where(f => f.isHired))
        {
            if (f.brigades == null)
                f.brigades = new List<BrigadeData>();

            f.brigades = f.brigades
                .Where(b => b != null)
                .GroupBy(b => b.id)
                .Select(g => g.First())
                .ToList();

            int expected = Mathf.Max(1, f.extraBrigades + 1);

            // ❌ убираем лишние бригады
            if (f.brigades.Count > expected)
            {
                var extras = f.brigades
                    .Where(b => !b.isWorking)
                    .Skip(expected)
                    .ToList();

                foreach (var ex in extras)
                {
                    f.brigades.Remove(ex);
                    data.allBrigades.RemoveAll(b => b.id == ex.id);
                }
            }

            // ✅ создаём недостающие
            int existing = f.brigades.Count;
            for (int i = existing + 1; i <= expected; i++)
            {
                string newId = $"{f.id}_brigade_{i}";
                if (data.allBrigades.Any(b => b.id == newId)) continue;

                var newBr = new BrigadeData
                {
                    id = newId,
                    foremanId = f.id,
                    name = $"Бригада {f.name} №{i}",
                    workers = new List<WorkerData>(),
                    completedOrders = 0,
                    isWorking = false,
                    maxWorkers = 30 // 👈 ЛИМИТ количества людей
                };

                f.brigades.Add(newBr);
                data.allBrigades.Add(newBr);
            }

            foreach (var b in f.brigades)
            {
                if (!data.allBrigades.Any(x => x.id == b.id))
                    data.allBrigades.Add(b);
            }
        }

        // 🧩 Свободные бригады (не на заказе)
        var free = data.allBrigades.Where(b => b != null && !b.isWorking).ToList();

        brigadeDropdown.onValueChanged.RemoveAllListeners();
        brigadeDropdown.ClearOptions();

        if (free.Count == 0)
        {
            brigadeDropdown.AddOptions(new List<string> { "❌ Нет свободных бригад" });
            brigadeDropdown.interactable = false;
            return;
        }

        brigadeDropdown.AddOptions(
            free.Select(b =>
            {
                var f = data.foremen.FirstOrDefault(x => x.id == b.foremanId);
                return $"{b.name} ({(f != null ? f.name : "Без бригадира")})";
            }).ToList()
        );

        brigadeDropdown.interactable = true;

        brigadeDropdown.onValueChanged.AddListener(index =>
        {
            currentBrigade = free[index];
            currentForeman = data.foremen.FirstOrDefault(f => f.id == currentBrigade.foremanId);
            LoadBrigade(currentBrigade);
        });

        currentBrigade = free[0];
        currentForeman = data.foremen.FirstOrDefault(f => f.id == currentBrigade.foremanId);

        LoadBrigade(currentBrigade);
    }

    private void LoadBrigade(BrigadeData brigade)
    {
        currentBrigade = brigade;
        if (currentBrigade == null) return;

        if (foremanPortrait != null && currentForeman != null)
        {
            var sprite = Resources.Load<Sprite>($"Icon/{currentForeman.iconId}");
            if (sprite != null) foremanPortrait.sprite = sprite;
        }

        UpdateWorkerCount();
        completedOrdersText.text = $"Выполнено заказов: {currentBrigade.completedOrders}";
        RefreshFreeWorkers();
        RefreshBrigadeWorkers();
    }

    private void RefreshFreeWorkers()
    {
        foreach (Transform child in freeWorkersContainer)
            Destroy(child.gameObject);

        var data = GameManager.Instance.Data;
        if (data == null) return;

        // Все нанятые строители
        var allWorkers = data.hiredWorkers
            .Where(w => w != null && w.category.ToLower().Contains("строй") && !w.isBusy)
            .ToList();

        // Уже в бригадах
        var assigned = data.allBrigades
            .Where(b => b != null)
            .SelectMany(b => b.workers)
            .ToList();

        // Свободные = не в assigned
        var freeWorkers = allWorkers.Where(w => !assigned.Contains(w)).ToList();

        bool full = currentBrigade.workers.Count >= currentBrigade.maxWorkers;

        foreach (var w in freeWorkers)
        {
            var row = Instantiate(workerRowPrefab, freeWorkersContainer);
            var ui = row.GetComponent<WorkerRowUI>();
            ui.Setup(w, OnSelectFreeWorker);

            bool locked = currentBrigade.isWorking;

            ui.SetInteractable(!locked && !full);
            ui.SetGray(locked || full);
        }
    }

    private void RefreshBrigadeWorkers()
    {
        foreach (Transform child in brigadeWorkersContainer)
            Destroy(child.gameObject);

        if (currentBrigade == null) return;

        foreach (var w in currentBrigade.workers)
        {
            var row = Instantiate(workerRowPrefab, brigadeWorkersContainer);
            var ui = row.GetComponent<WorkerRowUI>();
            ui.Setup(w, OnSelectBrigadeWorker);

            bool locked = currentBrigade.isWorking;

            ui.SetInteractable(!locked);
            ui.SetGray(locked);
        }

        UpdateWorkerCount();
    }

    private void OnSelectFreeWorker(WorkerRowUI ui)
    {
        ui.SetSelected(!selectedFreeWorkers.Contains(ui));
        if (ui.IsSelected) selectedFreeWorkers.Add(ui);
        else selectedFreeWorkers.Remove(ui);
    }

    private void OnSelectBrigadeWorker(WorkerRowUI ui)
    {
        ui.SetSelected(!selectedBrigadeWorkers.Contains(ui));
        if (ui.IsSelected) selectedBrigadeWorkers.Add(ui);
        else selectedBrigadeWorkers.Remove(ui);
    }

    private void AddSelectedWorkerToBrigade()
    {
        if (currentBrigade == null) return;

        if (currentBrigade.isWorking)
        {
            HUDController.Instance?.ShowToast("❌ Нельзя добавлять работников — бригада работает!");
            return;
        }

        if (currentBrigade.workers.Count >= currentBrigade.maxWorkers)
        {
            HUDController.Instance?.ShowToast("❌ Достигнут лимит 30 человек!");
            return;
        }

        foreach (var ui in selectedFreeWorkers)
        {
            if (currentBrigade.workers.Count >= currentBrigade.maxWorkers)
            {
                HUDController.Instance?.ShowToast("❌ Лимит 30 человек!");
                break;
            }

            var worker = ui.Data;
            if (worker == null) continue;

            worker.isBusy = true;

            if (!currentBrigade.workers.Contains(worker))
                currentBrigade.workers.Add(worker);
        }

        selectedFreeWorkers.Clear();
        RefreshFreeWorkers();
        RefreshBrigadeWorkers();
        UpdateWorkerCount();
    }

    private void RemoveSelectedWorkerFromBrigade()
    {
        if (currentBrigade == null) return;

        if (currentBrigade.isWorking)
        {
            HUDController.Instance?.ShowToast("❌ Нельзя изменять состав — бригада работает!");
            return;
        }

        foreach (var ui in selectedBrigadeWorkers)
        {
            var worker = ui.Data;
            if (worker == null) continue;

            worker.isBusy = false;
            currentBrigade.workers.Remove(worker);
        }

        selectedBrigadeWorkers.Clear();
        RefreshFreeWorkers();
        RefreshBrigadeWorkers();
        UpdateWorkerCount();
    }

    private void UpdateWorkerCount()
    {
        if (currentBrigade == null) return;

        workerCountText.text =
            $"Рабочих: {currentBrigade.workers.Count} / {currentBrigade.maxWorkers}";
    }
}
