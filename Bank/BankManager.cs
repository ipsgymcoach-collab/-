using System;
using UnityEngine;

public class BankManager : MonoBehaviour
{
    public static BankManager Instance;

    private GameManager gameManager;
    private GameData data;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        gameManager = GameManager.Instance;
        data = gameManager.CurrentGame;
    }

    /// <summary>
    /// Списание всех обязательных платежей (20-го числа месяца).
    /// </summary>
    public void ProcessMonthlyPayments(DateTime currentDate)
    {
        if (currentDate.Day != 20) return;

        // Аренда офиса
        DeductMoney(data.officeRent, "Аренда офиса");
        data.yearlyBills += data.officeRent;

        // Аренда гаража
        DeductMoney(data.garageRent, "Аренда гаража");
        data.yearlyBills += data.garageRent;

        // Основной долг
        if (data.currentDebt > 0)
        {
            int payment = Mathf.Min(data.monthlyDebtPayment, data.currentDebt);
            data.currentDebt -= payment;
            data.totalDebtPaid += payment;
            DeductMoney(payment, "Основной долг");
            data.yearlyDebtPayments += payment;
        }

        // Кредиты
        for (int i = 0; i < data.activeLoans.Count; i++)
        {
            LoanData loan = data.activeLoans[i];
            if (loan.isActive && loan.remainingAmount > 0)
            {
                int payment = Mathf.Min(loan.monthlyPayment, loan.remainingAmount);
                loan.remainingAmount -= payment;
                DeductMoney(payment, $"Кредит #{loan.loanId}");

                // === Разделение на тело и проценты ===
                int alreadyRepaid = loan.totalAmount - loan.remainingAmount;
                float principalProgress = (float)alreadyRepaid / loan.totalAmount;
                int expectedPrincipal = Mathf.RoundToInt(loan.principalAmount * principalProgress);
                int principalPart = expectedPrincipal - loan.alreadyPrincipalPaid;
                loan.alreadyPrincipalPaid += principalPart;
                int interestPart = payment - principalPart;

                // В отчёт идёт только процент
                data.yearlyLoanPayments += interestPart;

                if (loan.remainingAmount <= 0)
                {
                    loan.isActive = false;
                    Debug.Log($"[BankManager] Кредит #{loan.loanId} полностью выплачен!");
                }
            }
        }

        // обновляем дату следующего платежа
        data.nextDebtPaymentDate = GetNextPaymentDate(currentDate);

        // 🔧 Обновляем HUD
        if (HUDController.Instance != null)
            HUDController.Instance.UpdateHUD(data);
    }

    /// <summary>
    /// Внесение дополнительного платежа по основному долгу.
    /// </summary>
    public void ExtraDebtPayment(int amount)
    {
        if (amount <= 0) return;
        if (data.money < amount) return;

        int pay = Mathf.Min(amount, data.currentDebt);
        data.currentDebt -= pay;
        data.totalDebtPaid += pay;
        DeductMoney(pay, "Доп. платеж по долгу");
        data.yearlyDebtPayments += pay;

        // 🔧 Обновляем HUD
        if (HUDController.Instance != null)
            HUDController.Instance.UpdateHUD(data);
    }

    /// <summary>
    /// Взять кредит (с процентами и фиксированной выплатой).
    /// </summary>
    public bool TakeLoan(int loanId, int baseAmount, int monthlyPayment, DateTime currentDate)
    {
        foreach (var loan in data.activeLoans)
        {
            if (loan.loanId == loanId && loan.isActive)
            {
                Debug.Log($"Кредит #{loanId} уже активен!");
                return false;
            }
        }

        int totalToRepay = baseAmount;

        switch (loanId)
        {
            case 1:
                totalToRepay = 26235;  // вернуть с процентами
                monthlyPayment = 1749; // фиксированная выплата
                break;
            case 2:
                totalToRepay = 74940;
                monthlyPayment = 6245;
                break;
            case 3:
                totalToRepay = 190944;
                monthlyPayment = 15912;
                break;
        }

        LoanData newLoan = new LoanData(
            loanId,
            totalToRepay,
            monthlyPayment,
            currentDate.ToString("yyyy-MM-dd"),
            baseAmount
        );

        data.activeLoans.Add(newLoan);

        AddMoney(baseAmount, $"Взяли кредит #{loanId} (вернуть {totalToRepay}$, {monthlyPayment}$/мес)");

        // 🔧 Обновляем HUD
        if (HUDController.Instance != null)
            HUDController.Instance.UpdateHUD(data);

        return true;
    }

    /// <summary>
    /// Досрочная выплата кредита.
    /// </summary>
    public void ExtraLoanPayment(int loanId, int amount)
    {
        var loan = data.activeLoans.Find(l => l.loanId == loanId && l.isActive);
        if (loan == null) return;
        if (amount <= 0) return;
        if (data.money < amount) return;

        int pay = Mathf.Min(amount, loan.remainingAmount);
        loan.remainingAmount -= pay;
        DeductMoney(pay, $"Досрочная выплата кредита #{loan.loanId}");

        // === Разделение на тело и проценты ===
        int alreadyRepaid = loan.totalAmount - loan.remainingAmount;
        float principalProgress = (float)alreadyRepaid / loan.totalAmount;
        int expectedPrincipal = Mathf.RoundToInt(loan.principalAmount * principalProgress);
        int principalPart = expectedPrincipal - loan.alreadyPrincipalPaid;
        loan.alreadyPrincipalPaid += principalPart;
        int interestPart = pay - principalPart;

        data.yearlyLoanPayments += interestPart;

        if (loan.remainingAmount <= 0)
        {
            loan.isActive = false;
            Debug.Log($"[BankManager] Кредит #{loan.loanId} досрочно полностью выплачен!");
        }

        // 🔧 Обновляем HUD
        if (HUDController.Instance != null)
            HUDController.Instance.UpdateHUD(data);
    }

    private void DeductMoney(int amount, string reason)
    {
        data.money -= amount;
        Debug.Log($"[BankManager] Снято {amount}$ ({reason}), баланс: {data.money}$");

        // 🔧 Обновляем HUD
        if (HUDController.Instance != null)
            HUDController.Instance.UpdateHUD(data);
    }

    private void AddMoney(int amount, string reason)
    {
        data.money += amount;
        Debug.Log($"[BankManager] Добавлено {amount}$ ({reason}), баланс: {data.money}$");

        // 🔧 Обновляем HUD
        if (HUDController.Instance != null)
            HUDController.Instance.UpdateHUD(data);
    }

    private string GetNextPaymentDate(DateTime currentDate)
    {
        DateTime next = currentDate.AddMonths(1);
        return new DateTime(next.Year, next.Month, 20).ToString("yyyy-MM-dd");
    }
}
