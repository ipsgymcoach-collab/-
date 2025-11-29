using UnityEngine;

public class ReportsMainPageUI : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject reportIconPrefab;

    [Header("Pages")]
    [SerializeField] private GameObject yearReportPage;
    [SerializeField] private ReportYearPageUI yearReportUI;

    private GameData data;

    private void Awake()
    {
        data = GameManager.Instance.CurrentGame;
    }

    private void OnEnable()
    {
        RefreshList();
    }

    public void RefreshList()
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        var years = data.GetAvailableReportYears();

        foreach (int year in years)
        {
            GameObject obj = Instantiate(reportIconPrefab, contentParent);
            obj.GetComponent<ReportIconUI>().Setup(year, this);
        }
    }

    public void OpenYearReport(int year)
    {
        gameObject.SetActive(false);
        yearReportPage.SetActive(true);
        yearReportUI.ShowYear(year);
    }
}
