using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReportIconUI : MonoBehaviour
{
    [SerializeField] private TMP_Text yearLabel;
    private int year;
    private ReportsMainPageUI parent;

    public void Setup(int year, ReportsMainPageUI parent)
    {
        this.year = year;
        this.parent = parent;
        yearLabel.text = year.ToString();
    }

    public void OnClick()
    {
        parent.OpenYearReport(year);
    }
}
