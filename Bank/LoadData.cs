using System;

[Serializable]
public class LoanData
{
    public int loanId;               // ID кредита (1,2,3)
    public int principalAmount;      // сумма кредита без процентов
    public int totalAmount;          // сумма кредита с процентами
    public int remainingAmount;      // сколько осталось выплатить
    public int monthlyPayment;       // ежемесячный платёж
    public string takenDate;         // дата взятия кредита (строкой)
    public bool isActive;            // активен ли кредит

    // Для отслеживания долей
    public int alreadyPrincipalPaid; // сколько тела уже погашено

    public LoanData(int id, int totalAmount, int monthlyPayment, string date, int principalAmount)
    {
        this.loanId = id;
        this.totalAmount = totalAmount;
        this.remainingAmount = totalAmount;
        this.monthlyPayment = monthlyPayment;
        this.takenDate = date;
        this.isActive = true;

        this.principalAmount = principalAmount;
        this.alreadyPrincipalPaid = 0;
    }
}
