using UnityEngine;
using System.Linq;

public class LaborMarketManager : MonoBehaviour
{
    public static LaborMarketManager Instance;

    private GameManager gameManager;
    private GameData data;

    [Header("Назначение строителя")]
    [SerializeField] private AssignToBrigadePanel assignToBrigadePanel;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        gameManager = GameManager.Instance;
        data = gameManager.CurrentGame;
    }

    /// <summary>
    /// Добавить работника в список нанятых.
    /// Для строителей — сначала выбор бригады (если есть хотя бы одна).
    /// </summary>
    public void HireWorker(WorkerData worker)
    {
        if (data == null || worker == null) return;

        // Уже нанят?
        if (data.hiredWorkers != null && data.hiredWorkers.Any(w => w.id == worker.id))
        {
            Debug.Log($"[LaborMarketManager] {worker.firstName} уже нанят.");
            return;
        }

        string cat = (worker.category ?? "").Trim().ToLower();

        // Строитель → выбор бригады
        if (cat == "стройка" && data.allBrigades != null && data.allBrigades.Count > 0)
        {
            Debug.Log($"[LaborMarketManager] Открываю панель выбора бригады.");

            assignToBrigadePanel.gameObject.SetActive(true);

            assignToBrigadePanel.Open(
                worker,
                brigade =>
                {
                    if (brigade.workers == null)
                        brigade.workers = new System.Collections.Generic.List<WorkerData>();

                    brigade.workers.Add(worker);
                    worker.isBusy = true; // 🆕 фикс: работник теперь занят (в бригаде)

                    Debug.Log($"[LaborMarketManager] ✅ {worker.firstName} назначен в {brigade.name}");
                    FinalizeHire(worker);
                },
                () =>
                {
                    Debug.Log($"[LaborMarketManager] ⏳ {worker.firstName} нанят без бригады.");
                    FinalizeHire(worker);
                }
            );

            return;
        }

        // Обычный найм
        FinalizeHire(worker);
    }

    /// <summary>
    /// Фактический найм работника (со списанием денег).
    /// </summary>
    private void FinalizeHire(WorkerData worker)
    {
        // 💰 списываем деньги
        if (!GameManager.Instance.SpendMoney(worker.hireCost))
        {
            HUDController.Instance?.ShowToast("Недостаточно денег!");
            Debug.Log("❌ Недостаточно денег для найма!");
            return;
        }

        if (data.hiredWorkers == null)
            data.hiredWorkers = new System.Collections.Generic.List<WorkerData>();

        if (!data.hiredWorkers.Any(w => w.id == worker.id))
            data.hiredWorkers.Add(worker);

        worker.isHired = true;

        // 🧾 обновляем HUD и сохраняем игру
        HUDController.Instance?.UpdateHUD(data);
        SaveManager.SaveGame(GameManager.Instance.CurrentGame, GameManager.Instance.CurrentSlot);

        Debug.Log($"✅ Нанят {worker.profession}: {worker.firstName} {worker.lastName}. Деньги: {data.money}$");

        // 🔁 обновляем биржу труда
        var marketUI = Object.FindFirstObjectByType<LaborMarketUI>();
        if (marketUI != null)
        {
            marketUI.RebuildMarketKeepFilter();
            marketUI.RefreshMoneyUI?.Invoke();
        }

        // 🔁 Обновляем панель всех работников (если открыта)
        var workersPanel = Object.FindFirstObjectByType<WorkersPanelUI>();
        if (workersPanel != null)
            workersPanel.OpenPanel();
    }

    public void FireWorker(WorkerData worker)
    {
        if (data == null || worker == null) return;

        if (data.hiredWorkers != null && data.hiredWorkers.Remove(worker))
        {
            worker.isHired = false;
            worker.recentlyFired = false;
            worker.restDaysLeft = 0;
            worker.isBusy = false;

            // Удаляем из всех бригад
            if (data.allBrigades != null)
                foreach (var b in data.allBrigades)
                    b.workers?.Remove(worker);

            // Обновляем HUD и сохраняем игру
            HUDController.Instance?.UpdateHUD(data);
            SaveManager.SaveGame(GameManager.Instance.CurrentGame, GameManager.Instance.CurrentSlot);

            Debug.Log($"🗑️ Уволен {worker.firstName} {worker.lastName} ({worker.profession}).");

            // Возвращаем на биржу труда
            var marketUI = Object.FindFirstObjectByType<LaborMarketUI>();
            if (marketUI != null)
            {
                marketUI.RebuildMarketKeepFilter();
                marketUI.RefreshMoneyUI?.Invoke();
            }
        }
    }
}
