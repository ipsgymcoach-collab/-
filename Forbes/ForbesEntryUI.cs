using UnityEngine;
using TMPro;

public class ForbesEntryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text companyNameText;
    [SerializeField] private TMP_Text ceoNameText;
    [SerializeField] private TMP_Text netWorthText;
    [SerializeField] private TMP_Text changeText;

    public void Setup(CompanyRankData data)
    {
        rankText.text = data.rank.ToString();
        companyNameText.text = data.name;
        ceoNameText.text = data.ceoName;
        netWorthText.text = $"{data.netWorth:F1} млн $";

        // Цвет изменения
        if (data.dailyChange >= 0)
        {
            changeText.text = $"▲ {data.dailyChange:F1}%";
            changeText.color = new Color(0.2f, 0.8f, 0.2f); // зелёный
        }
        else
        {
            changeText.text = $"▼ {Mathf.Abs(data.dailyChange):F1}%";
            changeText.color = new Color(0.9f, 0.2f, 0.2f); // красный
        }
    }
}
