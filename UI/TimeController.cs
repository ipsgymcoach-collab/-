using UnityEngine;
using System;
using System.Linq;

public class TimeController : MonoBehaviour
{
    public static TimeController Instance;

    [Header("Настройки времени")]
    [Tooltip("Сколько секунд реального времени занимает 1 игровой год")]
    public float realSecondsPerGameYear = 3600f;

    public float GameSpeed { get; private set; } = 0f;   // 🚀 игра стартует с паузы
    private float lastSpeed = 1f;

    public static GaragePanelController GarageUI;
    private double totalGameSeconds;
    public double TotalGameSeconds => totalGameSeconds;

    private float gameSecondsPerRealSecond;

    [Header("Игровая дата/время")]
    public int day;
    public int month;
    public int year;
    public int hour;
    public int minute;

    private int lastDayChecked = -1;

    [Header("Настройки выплат (меняются через инспектор)")]
    [Tooltip("День месяца для выплаты зарплаты работникам и бригадирам")]
    [SerializeField] private int salaryPaymentDay = 8;

    [Tooltip("День месяца для списания аренды офиса и гаража")]
    [SerializeField] private int rentPaymentDay = 3;

    [Tooltip("День месяца для ежемесячного платежа по долгам и кредитам")]
    [SerializeField] private int loanPaymentDay = 20;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        float secondsInGameYear = 365f * 24f * 3600f;
        gameSecondsPerRealSecond = secondsInGameYear / Mathf.Max(1f, realSecondsPerGameYear);

        var gm = GameManager.Instance;
        if (gm != null && gm.CurrentGame != null && gm.CurrentGame.year > 0)
        {
            if (gm.CurrentGame.totalGameSeconds > 0)
            {
                totalGameSeconds = gm.CurrentGame.totalGameSeconds;
            }
            else
            {
                totalGameSeconds = ConvertToGameSeconds(
                    gm.CurrentGame.year,
                    gm.CurrentGame.month,
                    gm.CurrentGame.day,
                    gm.CurrentGame.hour,
                    gm.CurrentGame.minute
                );
            }
        }
        else
        {
            year = 2017;
            month = 1;
            day = 15;
            hour = 9;
            minute = 0;

            totalGameSeconds = ConvertToGameSeconds(year, month, day, hour, minute);
        }

        ConvertFromGameSeconds(totalGameSeconds, out year, out month, out day, out hour, out minute);

        HUDController.Instance?.RefreshDateTimeUI();

        lastDayChecked = day;
    }

    private void Update()
    {
        if (GameSpeed > 0f)
        {
            double delta = Time.unscaledDeltaTime * gameSecondsPerRealSecond * GameSpeed;
            totalGameSeconds += delta;

            ConvertFromGameSeconds(totalGameSeconds, out year, out month, out day, out hour, out minute);

            if (day != lastDayChecked)
            {
                lastDayChecked = day;
                OnNewDayStarted();

                // === 🔄 Обновляем UI активных заказов, если открыт ===
                if (OrdersPanelUI.Instance != null)
                {
                    OrdersPanelUI.Instance.RefreshActiveOrders();
                    Debug.Log("🔁 Активные заказы обновлены на новый день.");
                }
            }
        }
    }

    // === Новый день в игре ===
    private void OnNewDayStarted()
    {
        Debug.Log($"📅 Новый день: {day:D2}/{month:D2}/{year:D4}");
        var data = GameManager.Instance?.Data;
        if (data == null) return;

        // === 💰 1. Проверка отложенных предоплат ===
        if (data.pendingPrepayments != null && data.pendingPrepayments.Count > 0)
        {
            for (int i = data.pendingPrepayments.Count - 1; i >= 0; i--)
            {
                var pp = data.pendingPrepayments[i];
                pp.daysRemaining--;
                if (pp.daysRemaining <= 0)
                {
                    data.AddMoney(pp.amount);
                    Debug.Log($"💰 Предоплата {pp.amount}$ за заказ {pp.orderAddress} поступила на счёт!");
                    // Раньше здесь был HUDController.ShowToast — теперь просто обновляем деньги
                    HUDController.Instance?.UpdateMoney(data.money);
                    data.pendingPrepayments.RemoveAt(i);
                }
            }
        }

        // === 🕓 2. Таймер обновления заказов (Пригород) ===
        if (data.suburbOrdersState != null)
        {
            if (data.suburbOrdersState.daysUntilNewOrders > 0)
            {
                data.suburbOrdersState.daysUntilNewOrders--;

                if (data.suburbOrdersState.daysUntilNewOrders == 0)
                {
                    data.suburbOrdersState.daysUntilNewOrders = 30;
                    data.suburbOrdersState.currentOrderIds.Clear();
                    data.suburbOrdersState.needsRegenerate = true;

                    Debug.Log("📦 Пригород: таймер дошёл до 0 — список будет обновлён при открытии панели.");
                }
            }
        }

        // === 👷‍♂️ 3. Обновление рабочих (отдых и т.д.) ===
        WorkersDatabase.Instance?.UpdateRestDays();

        // === 💵 4. Зарплаты ===
        if (day == salaryPaymentDay)
            SalaryManager.PayMonthlySalaries(data);

        // === 🏢 5. Аренда ===
        if (day == rentPaymentDay)
            EconomyManager.PayOfficeAndGarageRent(data);

        // === 💳 6. Кредиты ===
        if (day == loanPaymentDay)
            EconomyManager.PayLoanPayments(data);

        // === 🚗 ОБНОВЛЕНИЕ ГАРАЖНОГО UI ПО НОВОМУ ДНЮ ===
        if (GaragePanelController.Instance != null &&
            GaragePanelController.Instance.gameObject.activeInHierarchy)
        {
            GaragePanelController.Instance.Rebuild();
        }

        // === 🚧 РЕМОНТ ТЕХНИКИ ПО ДНЯМ ===
        if (data.ownedVehicles != null && data.ownedVehicles.Count > 0)
        {
            foreach (var v in data.ownedVehicles)
            {
                if (v == null || !v.isUnderRepair)
                    continue;

                v.repairDaysLeft = Mathf.Max(0, v.repairDaysLeft - 1);

                if (v.repairDaysLeft <= 0)
                {
                    v.isUnderRepair = false;
                    v.inGarage = true;

                    int maxHP = GameManager.Instance.CurrentGame.GetGarageMaxVehicleCondition();
                    v.condition = maxHP;

                    // Раньше здесь был HUDController.ShowToast про окончание ремонта
                    Debug.Log($"🔧 '{v.name}' завершила ремонт: {maxHP}% HP!");
                }
            }
        }

        // === 🔁 7. Уведомляем панели (если открыты) ===
        SuburbOrdersPanelUI.Instance?.OnNewGameDay(day);

        // === 🏗️ Обновление активных заказов по дням ИГРОКА ===
        if (data != null && data.activeOrders != null && data.activeOrders.Count > 0)
        {
            foreach (var order in data.activeOrders)
            {
                if (!order.isActive || order.isCompleted) continue;

                // +1 игровой день
                order.daysPassed = Mathf.Clamp(order.daysPassed + 1, 0, Mathf.Max(1, order.remainingDays));

                // Прогресс считаем ТОЛЬКО по плану игрока (remainingDays)
                if (order.remainingDays > 0)
                {
                    float p = Mathf.Clamp01((float)order.daysPassed / order.remainingDays);
                    order.progress = Mathf.RoundToInt(p * 100f);
                }
                else
                {
                    order.progress = 0;
                }

                // Завершение
                if (order.daysPassed >= order.remainingDays)
                {
                    order.daysPassed = order.remainingDays;
                    order.progress = 100;
                    order.isCompleted = true;

                    int finalPayment = Mathf.RoundToInt(order.payment * 0.8f);
                    data.AddMoney(finalPayment);
                    data.completedOrders.Add(order.orderName);

                    // 🔹 Применяем отложенное изменение настроения бригады
                    if (!string.IsNullOrEmpty(order.brigadeId))
                    {
                        var b = data.allBrigades.FirstOrDefault(x => x != null && x.id == order.brigadeId);
                        if (b != null)
                        {
                            b.mood = Mathf.Clamp(b.mood + order.moodDeltaPlanned, 0, 100);
                            Debug.Log($"🙂 Настроение бригады '{b.name}' изменено на {order.moodDeltaPlanned:+#;-#;0}. Текущее: {b.mood}%");
                        }
                    }

                    SaveManager.SaveGame(data, 0);
                    HUDController.Instance?.UpdateMoney(data.money);
                    Debug.Log($"🏁 Заказ '{order.orderName}' завершён! +{finalPayment}$");
                }

                // Мягкий апдейт строки UI, если панель открыта
                if (OrdersPanelUI.Instance != null && OrdersPanelUI.Instance.gameObject.activeSelf)
                {
                    foreach (var item in OrdersPanelUI.Instance.ActiveOrderItems)
                    {
                        if (item.CurrentOrder == order)
                        {
                            item.UpdateProgressByTime(order.daysPassed);
                            break;
                        }
                    }
                }
            }

            // Полный рефреш только если панель видна
            if (OrdersPanelUI.Instance != null && OrdersPanelUI.Instance.gameObject.activeSelf)
                OrdersPanelUI.Instance.RefreshActiveOrders();
        }

        // === 📘 8. Проверяем смену года и создаём годовой отчёт ===
        CheckYearReport(data);
    }

    private void CheckYearReport(GameData data)
    {
        // === 📅 Срабатывает только 1 января ===
        if (day != 1 || month != 1)
            return;

        int reportYear = year - 1;

        // Проверка на дубликаты
        if (data.yearlyReports.Any(r => r.year == reportYear))
            return;

        if (reportYear <= 0)
            return;

        // === 📝 Создаём новый отчёт ===
        var report = new GameData.YearReportData();
        report.year = reportYear;

        // --- перенос расходов ---
        report.salaryExpenses = data.yearlySalaryExpenses;
        report.bills = data.yearlyBills;
        report.repairs = data.yearlyRepairs;
        report.loanInterest = data.yearlyLoanPayments;
        report.debtPayments = data.yearlyDebtPayments;
        report.purchases = data.yearlyPurchases;

        // --- перенос прибыли ---
        report.profitSmall = data.yearlyProfitSmall;
        report.profitMedium = data.yearlyProfitMedium;
        report.profitLarge = data.yearlyProfitLarge;
        report.profitSpecial = data.yearlyProfitSpecial;

        // --- итог ---
        report.totalProfit = data.yearlyTotalProfit;

        // Добавляем в список
        data.yearlyReports.Add(report);

        Debug.Log($"📘 Добавлен годовой отчёт за {reportYear}");

        // Сбрасываем показатели
        data.ResetYearlyReport();

        // Сохраняем
        SaveManager.SaveGame(data, 0);

        // === 🔔 Уведомление (как месячные списания) ===
        MonthlyPopupUI.Instance?.ShowMessage(
            $"📘 Годовой отчёт за {reportYear} готов!",
            new Color(0.85f, 0.1f, 0.1f), // красный фон
            5f // длительность
        );
    }



    // ===================== Управление скоростью =====================
    public void SetSpeed(float speed)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsUIOpen)
            return;

        if (speed > 0f)
        {
            GameSpeed = speed;
            lastSpeed = speed;
        }
    }

    public void Speed1() => SetSpeed(1f);
    public void Speed2() => SetSpeed(2f);
    public void Speed3() => SetSpeed(3f);

    public void TogglePause()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsUIOpen)
            return;

        if (GameSpeed > 0f)
        {
            lastSpeed = GameSpeed;
            GameSpeed = 0f;
        }
        else
        {
            GameSpeed = lastSpeed > 0f ? lastSpeed : 1f;
        }
    }

    public void SetPause(bool pause)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsUIOpen)
            return;

        if (pause)
        {
            if (GameSpeed > 0f)
            {
                lastSpeed = GameSpeed;
                GameSpeed = 0f;
            }
        }
        else
        {
            GameSpeed = lastSpeed > 0f ? lastSpeed : 1f;
        }
    }

    public void SetPause() => TogglePause();

    // ===================== Конвертация и форматирование =====================
    public double ConvertToGameSeconds(int y, int m, int d, int h, int min)
    {
        double seconds = 0;
        seconds += (y - 1) * 365.0 * 24.0 * 3600.0;
        seconds += (m - 1) * 30.0 * 24.0 * 3600.0;
        seconds += (d - 1) * 24.0 * 3600.0;
        seconds += h * 3600.0;
        seconds += min * 60.0;
        return seconds;
    }

    public void ConvertFromGameSeconds(double totalSeconds, out int y, out int m, out int d, out int h, out int min)
    {
        y = (int)(totalSeconds / (365.0 * 24.0 * 3600.0)) + 1;
        totalSeconds %= 365.0 * 24.0 * 3600.0;

        m = (int)(totalSeconds / (30.0 * 24.0 * 3600.0)) + 1;
        totalSeconds %= 30.0 * 24.0 * 3600.0;

        d = (int)(totalSeconds / (24.0 * 3600.0)) + 1;
        totalSeconds %= 24.0 * 3600.0;

        h = (int)(totalSeconds / 3600.0);
        totalSeconds %= 3600.0;

        min = (int)(totalSeconds / 60.0);
    }

    public string GetTimeString()
    {
        bool use12h = GameManager.Instance?.CurrentGame?.use12HourFormat ?? false;
        if (use12h)
        {
            int displayHour = (hour % 12 == 0) ? 12 : hour % 12;
            string ampm = hour < 12 ? "AM" : "PM";
            return $"{displayHour:D2}:{minute:D2} {ampm}";
        }
        return $"{hour:D2}:{minute:D2}";
    }

    public string GetDateString()
    {
        bool ddmm = GameManager.Instance?.CurrentGame?.isDateFormatDDMM ?? true;
        if (ddmm)
            return $"{day:D2}/{month:D2}/{year:D4}";
        else
            return $"{month:D2}/{day:D2}/{year:D4}";
    }

    public void SaveToGameData(GameData data)
    {
        data.totalGameSeconds = totalGameSeconds;
        data.year = year;
        data.month = month;
        data.day = day;
        data.hour = hour;
        data.minute = minute;
    }

    public void LoadFromGameData(GameData data)
    {
        totalGameSeconds = data.totalGameSeconds > 0
            ? data.totalGameSeconds
            : ConvertToGameSeconds(data.year, data.month, data.day, data.hour, data.minute);

        ConvertFromGameSeconds(totalGameSeconds, out year, out month, out day, out hour, out minute);
        HUDController.Instance?.RefreshDateTimeUI();
        data.ClampAllVehicleHP();
    }
}
