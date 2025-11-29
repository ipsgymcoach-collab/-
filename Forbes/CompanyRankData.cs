using System;
using UnityEngine;

[Serializable]
public class CompanyRankData
{
    public string name;
    public string ceoName;
    public int rank;
    public float netWorth;        // Текущее состояние ($ млн)
    public float dailyChange;     // Процент изменения за день (−3%…+3%)

    public CompanyRankData(string name, string ceoName, float netWorth)
    {
        this.name = name;
        this.ceoName = ceoName;
        this.netWorth = netWorth;
        this.dailyChange = 0;
        this.rank = 0;
    }
}
