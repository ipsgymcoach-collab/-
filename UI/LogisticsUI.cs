using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class LogisticsUI : MonoBehaviour
{
    [Header("Slices (Pie Chart)")]
    [SerializeField] private Image constructionSlice;  // жёлтый сектор
    [SerializeField] private Image officeSlice;        // синий сектор

    [Header("Counters")]
    [SerializeField] private TMP_Text constructionCountText;
    [SerializeField] private TMP_Text officeCountText;

    [Header("Salary UI")]
    [SerializeField] private TMP_Text constructionSalaryText;
    [SerializeField] private TMP_Text officeSalaryText;

    [Header("Workers List")]
    [SerializeField] private Transform workersContainer;
    [SerializeField] private GameObject workerPrefab;

    private List<WorkerData> hiredWorkers = new List<WorkerData>();

    private void OnEnable()
    {
        RefreshUI();
    }

    // -------------------------------------------------------------------
    // MAIN REFRESH
    // -------------------------------------------------------------------
    private void RefreshUI()
    {
        LoadHiredWorkers();
        UpdatePieChart();
        UpdateCounters();
        UpdateSalaries();
        UpdateWorkersList();
    }

    // -------------------------------------------------------------------
    // LOAD HIRED WORKERS
    // -------------------------------------------------------------------
    private void LoadHiredWorkers()
    {
        hiredWorkers.Clear();

        GameData data = GameManager.Instance.CurrentGame;
        if (data == null || data.hiredWorkers == null)
            return;

        // Гарантированная сортировка по алфавиту
        hiredWorkers = data.hiredWorkers
            .OrderBy(w => w.lastName)
            .ThenBy(w => w.firstName)
            .ToList();

        // Автоприсвоение категории, если пусто
        foreach (var w in hiredWorkers)
        {
            if (string.IsNullOrEmpty(w.category))
            {
                string p = w.profession.ToLower();

                if (p.Contains("разн") ||
                    p.Contains("стро") ||
                    p.Contains("кран") ||
                    p.Contains("монтаж") ||
                    p.Contains("земле") ||
                    p.Contains("дорож"))
                {
                    w.category = "Стройка";
                }
                else
                {
                    w.category = "Офис";
                }
            }
        }
    }


    // -------------------------------------------------------------------
    // PIE CHART
    // -------------------------------------------------------------------
    private void UpdatePieChart()
    {
        int c = hiredWorkers.Count(w => w.category == "Стройка");
        int o = hiredWorkers.Count(w => w.category == "Офис");

        int total = c + o;

        if (total == 0)
        {
            constructionSlice.fillAmount = 0.5f;
            officeSlice.fillAmount = 1f;
            return;
        }

        float percent = (float)c / total;

        officeSlice.fillAmount = 1f;
        constructionSlice.fillAmount = percent;
    }

    // -------------------------------------------------------------------
    // TEXT COUNTERS
    // -------------------------------------------------------------------
    private void UpdateCounters()
    {
        int c = hiredWorkers.Count(w => w.category == "Стройка");
        int o = hiredWorkers.Count(w => w.category == "Офис");

        constructionCountText.text = c.ToString();
        officeCountText.text = o.ToString();
    }

    // -------------------------------------------------------------------
    // SALARY
    // -------------------------------------------------------------------
    private void UpdateSalaries()
    {
        int sc = hiredWorkers.Where(w => w.category == "Стройка").Sum(w => w.salary);
        int so = hiredWorkers.Where(w => w.category == "Офис").Sum(w => w.salary);

        constructionSalaryText.text = $"{sc:n0}$ / мес";
        officeSalaryText.text = $"{so:n0}$ / мес";
    }

    // -------------------------------------------------------------------
    // WORKERS LIST
    // -------------------------------------------------------------------
    private void UpdateWorkersList()
    {
        foreach (Transform t in workersContainer)
            Destroy(t.gameObject);

        // Сортировка по имени → затем по фамилии
        var sorted = hiredWorkers
            .OrderBy(w => w.firstName)     // имя
            .ThenBy(w => w.lastName)       // фамилия
            .ToList();

        foreach (var w in sorted)
        {
            var go = Instantiate(workerPrefab, workersContainer);
            go.GetComponent<WorkerItemUI_Logic>().Setup(w);
        }
    }

}
