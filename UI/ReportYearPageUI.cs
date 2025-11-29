using UnityEngine;
using TMPro;

public class ReportYearPageUI : MonoBehaviour
{
    [Header("Title")]
    [SerializeField] private TMP_Text titleLabel;

    [Header("Values")]
    [SerializeField] private TMP_Text salaryValue;
    [SerializeField] private TMP_Text billsValue;
    [SerializeField] private TMP_Text repairsValue;
    [SerializeField] private TMP_Text loanPaymentsValue;
    [SerializeField] private TMP_Text debtPaymentsValue;
    [SerializeField] private TMP_Text profitSmallValue;
    [SerializeField] private TMP_Text profitMediumValue;
    [SerializeField] private TMP_Text profitLargeValue;
    [SerializeField] private TMP_Text profitSpecialValue;
    [SerializeField] private TMP_Text totalValue;

    private GameData data;

    private void Awake()
    {
        data = GameManager.Instance.CurrentGame;
    }

    public void ShowYear(int year)
    {
        var report = data.GetReportForYear(year);
        if (report == null)
        {
            Debug.LogError($"Нет данных отчёта за {year}");
            return;
        }

        titleLabel.text = $"Отчёт за {year} год";

        salaryValue.text = $"{report.salaryExpenses}$";
        billsValue.text = $"{report.bills}$";
        repairsValue.text = $"{report.repairs}$";
        loanPaymentsValue.text = $"{report.loanInterest}$";
        debtPaymentsValue.text = $"{report.debtPayments}$";
        profitSmallValue.text = $"{report.profitSmall}$";
        profitMediumValue.text = $"{report.profitMedium}$";
        profitLargeValue.text = $"{report.profitLarge}$";
        profitSpecialValue.text = $"{report.profitSpecial}$";

        int total =
            -report.salaryExpenses
            - report.bills
            - report.repairs
            - report.loanInterest
            - report.debtPayments
            + report.profitSmall
            + report.profitMedium
            + report.profitLarge
            + report.profitSpecial;

        totalValue.text = $"{total}$";
    }

    public void BackToList(GameObject listPage)
    {
        listPage.SetActive(true);
        gameObject.SetActive(false);
    }
}
