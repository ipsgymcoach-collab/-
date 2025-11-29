using UnityEngine;
using System;

public static class EconomyManager
{
    /// <summary>Оплата аренды офиса и гаража.</summary>
    public static void PayOfficeAndGarageRent(GameData data)
    {
        if (data == null) return;

        int totalRent = data.officeRent + data.garageRent;

        if (totalRent > 0)
        {
            data.money -= totalRent;
            HUDController.Instance?.UpdateMoney(data.money);

            string msg = $"🏢 Оплачена аренда офиса и гаража: {totalRent}$\nОстаток: {data.money}$";
            MonthlyPopupUI.Instance?.ShowMessage(
                msg,
                new Color(0.9f, 0.7f, 0.2f),   // жёлто-оранжевый
                45f
            );

            Debug.Log($"[EconomyManager] Выплачена аренда: {totalRent}$");
        }
    }

    /// <summary>Оплата долгов и кредитов.</summary>
    public static void PayLoanPayments(GameData data)
    {
        if (data == null) return;

        int totalLoanPayments = 0;

        // Основной долг
        if (data.currentDebt > 0)
        {
            int payment = Mathf.Min(data.monthlyDebtPayment, data.currentDebt);
            data.currentDebt -= payment;
            totalLoanPayments += payment;
        }

        // Активные кредиты
        if (data.activeLoans != null)
        {
            foreach (var loan in data.activeLoans)
            {
                if (loan.isActive && loan.remainingAmount > 0)
                {
                    int pay = Mathf.Min(loan.monthlyPayment, loan.remainingAmount);
                    loan.remainingAmount -= pay;
                    totalLoanPayments += pay;

                    if (loan.remainingAmount <= 0)
                        loan.isActive = false;
                }
            }
        }

        if (totalLoanPayments > 0)
        {
            data.money -= totalLoanPayments;
            HUDController.Instance?.UpdateMoney(data.money);

            string msg = $"💳 Выплаты по кредитам и долгам: {totalLoanPayments}$\nОстаток: {data.money}$";
            MonthlyPopupUI.Instance?.ShowMessage(
                msg,
                new Color(0.25f, 0.45f, 0.9f),  // синий
                45f
            );

            Debug.Log($"[EconomyManager] Кредиты/долги: {totalLoanPayments}$");
        }
    }
}
