using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class LaborMarketUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject workerItemPrefab;
    [SerializeField] private TMP_Dropdown professionFilter;
    [SerializeField] private Button sortCategoryBtn;
    [SerializeField] private Button sortLevelBtn;
    [SerializeField] private Button sortSalaryBtn;
    [SerializeField] private Button sortHireBtn;
    [SerializeField] private Button resetFilterButton;

    [Header("Top Info UI")]
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text buildersCountText;
    [SerializeField] private TMP_Text officeCountText;

    public System.Action RefreshMoneyUI;

    /// <summary>
    /// Полный список рабочих рынка (без нанятых)
    /// </summary>
    private List<WorkerData> baseWorkers = new List<WorkerData>();

    /// <summary>
    /// Текущий отображаемый список (фильтр + сортировка)
    /// </summary>
    private List<WorkerData> currentWorkers = new List<WorkerData>();

    private bool ascCategory = true, ascLevel = true, ascSalary = true, ascHire = true;
    private float nextUpdateMonth;

    // Максимальные квоты по уровню игрока (строители / офис)
    private readonly Dictionary<int, (int builders, int office)> maxByLevel = new()
    {
        {1, (8, 3)},
        {2, (14, 4)},
        {3, (18, 6)},
        {4, (24, 7)},
        {5, (30, 9)},
        {6, (35, 10)},
        {7, (42, 11)},
        {8, (50, 12)},
        {9, (55, 15)},
        {10, (80, 20)}
    };

    private void Start()
    {
        RefreshMoneyUI = UpdateMoneyUI;

        // Сначала генерируем базовый список
        GenerateNewList();
        // Потом заполняем дропдаун профессий
        FillProfessionFilter();

        sortCategoryBtn.onClick.AddListener(() => SortBy("category"));
        sortLevelBtn.onClick.AddListener(() => SortBy("level"));
        sortSalaryBtn.onClick.AddListener(() => SortBy("salary"));
        sortHireBtn.onClick.AddListener(() => SortBy("hireCost"));

        professionFilter.onValueChanged.AddListener(delegate { ApplyFilter(); });
        resetFilterButton.onClick.AddListener(ResetFilter);

        UpdateMoneyUI();
        ScheduleNextUpdate();
    }

    private void Update()
    {
        int curMonth = (TimeController.Instance != null)
            ? TimeController.Instance.month
            : GameManager.Instance.CurrentGame.month;

        if (curMonth >= nextUpdateMonth)
        {
            // Обновляем рынок, но сохраняем выбранный фильтр
            RebuildMarketKeepFilter();
            ScheduleNextUpdate();
        }
    }

    private void ScheduleNextUpdate()
    {
        float add = Random.Range(1f, 4f); // обновление раз в 1–3 месяца
        int curMonth = (TimeController.Instance != null)
            ? TimeController.Instance.month
            : GameManager.Instance.CurrentGame.month;

        nextUpdateMonth = curMonth + add;
    }

    /// <summary>
    /// Пересобираем базовый список работников (без нанятых)
    /// </summary>
    private void GenerateNewList()
    {
        int playerLevel = GameManager.Instance.CurrentGame.level;

        // Полный список с учётом уровня, квот и т.п.
        baseWorkers = WorkersDatabase.Instance
            .GetWorkersForLevel(playerLevel)
            .Where(w => !w.isHired)
            .ToList();

        // По умолчанию показываем всех (фильтр = "Все профессии")
        currentWorkers = new List<WorkerData>(baseWorkers);

        RedrawList();
    }

    private void SortBy(string field)
    {
        if (currentWorkers == null || currentWorkers.Count == 0) return;

        switch (field)
        {
            case "category":
                currentWorkers = ascCategory
                    ? currentWorkers.OrderBy(w => w.category).ToList()
                    : currentWorkers.OrderByDescending(w => w.category).ToList();
                ascCategory = !ascCategory;
                break;

            case "level":
                currentWorkers = ascLevel
                    ? currentWorkers.OrderBy(w => w.appearanceLevel).ToList()
                    : currentWorkers.OrderByDescending(w => w.appearanceLevel).ToList();
                ascLevel = !ascLevel;
                break;

            case "salary":
                currentWorkers = ascSalary
                    ? currentWorkers.OrderBy(w => w.salary).ToList()
                    : currentWorkers.OrderByDescending(w => w.salary).ToList();
                ascSalary = !ascSalary;
                break;

            case "hireCost":
                currentWorkers = ascHire
                    ? currentWorkers.OrderBy(w => w.hireCost).ToList()
                    : currentWorkers.OrderByDescending(w => w.hireCost).ToList();
                ascHire = !ascHire;
                break;
        }

        RedrawList();
    }

    private void RedrawList()
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        foreach (var w in currentWorkers)
        {
            var go = Instantiate(workerItemPrefab, contentParent);
            go.GetComponent<WorkerItemUI>().Setup(w, this);
        }

        RefreshAllItemsAvailability();
    }

    private void FillProfessionFilter()
    {
        var professions = WorkersDatabase.Instance.workers
            .Where(w => w != null && !string.IsNullOrEmpty(w.profession))
            .Select(w => w.profession)
            .Distinct()
            .OrderBy(p => p)
            .ToList();

        professions.Insert(0, "Все профессии");
        professionFilter.ClearOptions();
        professionFilter.AddOptions(professions);

        // Стартовое значение
        professionFilter.value = 0;
        professionFilter.RefreshShownValue();
    }

    /// <summary>
    /// Применить фильтр по профессии к baseWorkers
    /// </summary>
    private void ApplyFilter()
    {
        if (baseWorkers == null) return;

        string selected = professionFilter.options[professionFilter.value].text;

        if (selected == "Все профессии")
        {
            currentWorkers = new List<WorkerData>(baseWorkers);
        }
        else
        {
            currentWorkers = baseWorkers
                .Where(w => w.profession == selected)
                .ToList();
        }

        RedrawList();
    }

    /// <summary>
    /// Кнопка "Сбросить фильтр"
    /// </summary>
    private void ResetFilter()
    {
        if (professionFilter == null) return;

        professionFilter.value = 0;
        professionFilter.RefreshShownValue();

        // просто показываем всех из baseWorkers
        currentWorkers = new List<WorkerData>(baseWorkers);
        RedrawList();

        Debug.Log("🔄 Фильтр профессий сброшен");
    }

    private void UpdateMoneyUI()
    {
        moneyText.text = $"{GameManager.Instance.CurrentGame.money:n0}$";
        UpdateStatsUI();
    }

    private void UpdateStatsUI()
    {
        var data = GameManager.Instance.CurrentGame;
        if (data == null) return;

        if (levelText != null)
            levelText.text = $"Уровень: {data.level}";

        int builders = 0;
        int office = 0;

        if (data.hiredWorkers != null)
        {
            builders = data.hiredWorkers.Count(w => w.category == "Стройка");
            office = data.hiredWorkers.Count(w => w.category == "Офис");
        }

        var caps = GetMaxForLevel(data.level);

        if (buildersCountText != null)
            buildersCountText.text = $"Строители: {builders} / {caps.builders}";

        if (officeCountText != null)
            officeCountText.text = $"Офисники: {office} / {caps.office}";
    }

    // ==== PUBLIC HELPERS ====

    public (int builders, int office) GetMaxForLevel(int level)
    {
        return maxByLevel.ContainsKey(level) ? maxByLevel[level] : (5, 3);
    }

    public bool IsCategoryAtLimit(string category)
    {
        var data = GameManager.Instance.CurrentGame;
        if (data == null) return false;

        int level = data.level;
        var caps = GetMaxForLevel(level);

        int builders = 0, office = 0;
        if (data.hiredWorkers != null)
        {
            builders = data.hiredWorkers.Count(w => w.category == "Стройка");
            office = data.hiredWorkers.Count(w => w.category == "Офис");
        }

        if (category == "Стройка") return builders >= caps.builders;
        if (category == "Офис") return office >= caps.office;
        return false;
    }

    public void RefreshAllItemsAvailability()
    {
        foreach (Transform child in contentParent)
        {
            var ui = child.GetComponent<WorkerItemUI>();
            if (ui != null) ui.RefreshAvailability();
        }

        UpdateStatsUI();
    }

    /// <summary>
    /// Пересобрать рынок после найма/увольнения, сохранив выбранный фильтр
    /// </summary>
    public void RebuildMarketKeepFilter()
    {
        int savedIndex = (professionFilter != null) ? professionFilter.value : 0;

        GenerateNewList(); // обновляем baseWorkers и currentWorkers

        if (professionFilter != null &&
            professionFilter.options != null &&
            professionFilter.options.Count > 0)
        {
            if (savedIndex < 0 || savedIndex >= professionFilter.options.Count)
                savedIndex = 0;

            professionFilter.value = savedIndex;
            professionFilter.RefreshShownValue();
            ApplyFilter();
        }
        else
        {
            RefreshAllItemsAvailability();
        }
    }
}
