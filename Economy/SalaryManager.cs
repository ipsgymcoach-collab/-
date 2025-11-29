using UnityEngine;

public static class SalaryManager
{
    /// <summary>
    /// Выплата ежемесячных зарплат всем нанятым сотрудникам и бригадирам.
    /// Вызывать 1-го числа каждого месяца.
    /// </summary>
    public static void PayMonthlySalaries(GameData data)
    {
        if (data == null) return;

        int total = 0;

        // Рабочие
        if (data.hiredWorkers != null)
        {
            foreach (var w in data.hiredWorkers)
            {
                if (w != null && w.isHired)
                    total += w.salary;
            }
        }

        // Бригадиры
        if (data.foremen != null)
        {
            foreach (var f in data.foremen)
            {
                if (f != null && f.isHired)
                    total += f.salary;
            }
        }

        if (total > 0)
        {
            data.money -= total;
            HUDController.Instance?.UpdateMoney(data.money);

            string msg = $"💵 Выплачены зарплаты: {total}$\nОстаток: {data.money}$";
            MonthlyPopupUI.Instance?.ShowMessage(
                msg,
                new Color(0.15f, 0.7f, 0.2f), // зелёный фон
                45f                            // показывать 45 секунд (~3 игровых часа)
            );
        }
    }
}
