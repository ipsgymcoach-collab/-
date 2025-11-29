using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;

/// <summary>
/// Группа техники для фильтрации во вкладках «Рабочая / Транспортные»
/// (отдельно от детального VehicleType)
/// </summary>
[Serializable]
public enum VehicleGroup
{
    Working,     // Рабочая (строительная)
    Transport    // Транспортные (включая прицепы/грузовики)
}

[Serializable]
public enum VehicleType
{
    Working,   // Рабочая техника (экскаватор, кран, каток и т.д.)
    Trailer,   // Прицеп
    Transport  // Транспорт (грузовики/фургоны/самосвалы)
}

[Serializable]
public class BrigadeData
{
    public string id;                     // Уникальный ID бригады
    public string foremanId;              // ID бригадира
    public string name;                   // Название ("Бригада Егорова №1")
    public List<WorkerData> workers = new List<WorkerData>(); // Список рабочих в бригаде
    public int completedOrders = 0;       // Кол-во завершённых заказов
    public int maxWorkers = 30;
    public bool isSelected = false;

    [Range(0, 100)]
    public int mood = 70;                 // 🔹 Настроение бригады (по умолчанию 70%)

    public bool isWorking = false;        // true = бригада сейчас на заказе
    public string currentOrderId = "";    // id текущего заказа (если назначена)
}

[Serializable]
public class VehicleData
{
    public string id;
    public string uniqueId;
    public string name;
    public VehicleType type;
    public VehicleGroup group;
    public float condition = 100;
    public int baseMaxHP = 100;
    public int maintenanceCost;
    public int price;
    public bool inGarage = true;
    public string iconId;
    public int repairCost = 0;
    public int maxOwnedAllowed = 1;
    public int unlockLevel = 1;
    public string shopIconId;
    public string description; // 🟡 Описание техники для окна с вопросом

    // 🚧 Ремонт по дням
    public bool isUnderRepair = false;
    public int repairDaysLeft = 0;
}

[System.Serializable]
public class OrderData
{
    // 🆔 Основные параметры
    public string orderId;            // ID заказа из базы OrdersDatabase
    public string orderName;          // Название / адрес
    public string address;            // Адрес из OrderInfo
    public int payment;               // Базовая оплата

    // 📅 Временные параметры
    public int totalDays;             // Всего дней на выполнение
    public int remainingDays;         // Осталось дней до завершения
    public int daysPassed = 0;        // Сколько дней прошло с начала

    // 👷 Рабочие и прогресс
    public int currentWorkers;
    public int maxWorkers;
    public int progress;              // Процент прогресса (если нужно для UI)

    // 💰 Финансовые показатели
    public int netProfit;             // Чистая прибыль (после расчётов)

    // 🧱 Бригада и настроение
    public string brigadeId;          // ID бригады, выполняющей заказ
    public string brigadeName;        // Имя бригады
    public int workersMood = 100;     // Настроение рабочих
    public int moodDeltaPlanned;      // Какое изменение настроения применить в конце

    // ⚙️ Статус заказа
    public bool isStarted = false;    // Начат ли заказ (в работе)
    public bool isActive = true;      // Активен ли заказ (показывать в списке)
    public bool isCompleted = false;  // Завершён ли проект
}


// === 🧱 Новая структура для склада ресурсов ===
[Serializable]
public class WarehouseResource
{
    public string id;       // ID ресурса (cement, sand, и т.д.)
    public string name;     // Название ресурса
    public int quantity;    // Количество на складе
}

[Serializable]
public class ForemanData
{
    public string id;
    public string name;
    public string buff;
    public string debuff;
    public int hireCost;
    public int salary;
    public string iconId;

    public bool isHired = false;
    public bool isFired = false;
    public int rehireAvailableDay = 0;
    public int requiredLevel = 1;

    // === Новые поля для системы бригад ===
    public int extraBrigades = 0;          // сколько дополнительных бригад он даёт
    public float speedBonus = 0f;          // бонус к скорости строительства в %
    public bool isSpecialLeader = false;   // флаг — особый лидер
    public List<BrigadeData> brigades = new List<BrigadeData>();

    // === Новое поле ===
    public bool isBusy = false;            // 🚧 Занят ли сейчас бригадир на объекте
}


[Serializable]
public class GameData
{
    // ==== Общие игровые данные ====
    public string companyName = "Knight Construction";
    public int selectedLogoId = 1;
    public int selectedHeroId = 0;
    public int currentDebt = 15000000;
    public List<LoanData> activeLoans = new List<LoanData>();

    public int startingDebt = 15000000;
    public int monthlyDebtPayment = 21500;
    public int totalDebtPaid = 0;
    public string nextDebtPaymentDate = "2025-01-20";

    public int officeRent = 8000;
    public int garageRent = 12000;

    public int level = 1;
    public int xp = 0;
    public int money = 135000;
    public int playerXP = 0; // 🔹 общий опыт игрок

    public int day = 15;
    public int month = 1;
    public int year = 2017;
    public int hour = 9;
    public int minute = 30;

    public bool use12HourFormat = false;
    public bool isDateFormatDDMM = true;

    public double totalGameSeconds = 0;

    public float cameraPosX = 0f;
    public float cameraPosY = 5f;
    public float cameraPosZ = -10f;

    public float cameraRotX = 10f;
    public float cameraRotY = 0f;
    public float cameraRotZ = 0f;

    public bool hasSavedCamera = false;

    public int yearlySalaryExpenses = 0;
    public int yearlyBills = 0;
    public int yearlyRepairs = 0;
    public int yearlyLoanPayments = 0;
    public int yearlyDebtPayments = 0;
    public int yearlyPurchases = 0;

    public int yearlyProfitSmall = 0;
    public int yearlyProfitMedium = 0;
    public int yearlyProfitLarge = 0;
    public int yearlyProfitSpecial = 0;
    public int yearlyTotalProfit = 0;
    public int yearlyVehicleSales = 0;

    public string lastSaveTime = "2025-01-01 09:30";
    public bool cleanupPaid = false;
    public bool cleanupAsked = false;
    public bool notificationsEnabled = true;
    public List<PendingPrepaymentData> pendingPrepayments = new List<PendingPrepaymentData>();
    public OrdersCategoryState suburbOrdersState = new OrdersCategoryState();


    public OrdersCategoryState cityOrdersState = new OrdersCategoryState();
    public OrdersCategoryState centerOrdersState = new OrdersCategoryState();
    public OrdersCategoryState specialOrdersState = new OrdersCategoryState();


    public global::SerializableDictionary<string, int> eventFlags = new global::SerializableDictionary<string, int>();
    public string LastSceneEntryPoint { get; set; } = "";
    public List<ForemanData> foremen = new List<ForemanData>();

    public List<TeamData> teams = new List<TeamData>();
    public List<OrderData> activeOrders = new List<OrderData>();
    public List<string> completedOrders = new List<string>();

    // 🔹 Все нанятые рабочие игрока
    public List<WorkerData> workers = new List<WorkerData>();

    // 🔹 Все существующие бригады (вспомогательный общий список)
    public List<BrigadeData> allBrigades = new List<BrigadeData>();

    public int suburbXP = 0;

    public List<OrderData> pendingOrders = new List<OrderData>(); //

    public bool hasOwnHouse = false;

    public int basePrice;   // цена из магазина
    public TeamData GetTeamById(string id)
    {
        return teams.Find(t => t.id == id);
    }

    // ==== Владения игрока ====
    public List<VehicleData> ownedVehicles = new List<VehicleData>();
    // Временно
    public float timeScaleBeforePause = 1f;

    // ==== Каталог моделей (не сериализуется, чтобы не затирал ownedVehicles при загрузке) ====
    [NonSerialized]
    public List<VehicleData> vehicles = new List<VehicleData>
    {
        // 1 lvl //
        new VehicleData { id = "MiniExcavator", name = "Мини - Экскаватор E-120", type = VehicleType.Working, group = VehicleGroup.Working, condition = 100, maintenanceCost = 450, price = 10000, iconId = "Miniexcavator", shopIconId = "Miniexcavatorshop", repairCost = 500, unlockLevel = 1, maxOwnedAllowed = 3 },
        new VehicleData { id = "MiniForklift", name = "Вилочный Погрузчик TC-3000", type = VehicleType.Working, group = VehicleGroup.Working, condition = 100, maintenanceCost = 150, price = 5000, iconId = "Miniforklift", shopIconId = "Miniforkliftshop",  repairCost = 350, unlockLevel = 1, maxOwnedAllowed = 5 },
        new VehicleData { id = "MiniLoader", name = "Мини - Погрузчик CM-90", type = VehicleType.Working, group = VehicleGroup.Working, condition = 100, maintenanceCost = 300, price = 9000, iconId = "Miniloader", shopIconId = "Miniloadershop", repairCost = 600, unlockLevel = 1, maxOwnedAllowed = 3 },
        new VehicleData { id = "FirsTrak", name = "Трак L-240", type = VehicleType.Transport, group = VehicleGroup.Transport, condition = 90, maintenanceCost = 370, price = 7000, iconId = "Firstrak", shopIconId = "Firsttrakshop", repairCost = 300, unlockLevel = 1, maxOwnedAllowed = 2 },
        // 2 lvl //
        new VehicleData { id = "SmallElevator", name = "Маленький подъемник", type = VehicleType.Working, group = VehicleGroup.Working, condition = 100, maintenanceCost = 100, price = 3500, iconId = "Smallelevator", shopIconId = "Smallelevatorshop", repairCost = 250, unlockLevel = 2, maxOwnedAllowed = 3 },
        new VehicleData { id = "SecondTrak", name = "Трак L-370", type = VehicleType.Transport, group = VehicleGroup.Transport, condition = 100, maintenanceCost = 310, price = 9200, iconId = "Secondtrak", shopIconId = "Secondtrakshop", repairCost = 350, unlockLevel = 2, maxOwnedAllowed = 3 },
        new VehicleData { id = "FirstAutotrawler", name = "Автотрал 4 х 2 ", type = VehicleType.Transport, group = VehicleGroup.Transport, condition = 100, maintenanceCost = 200, price = 4000, iconId = "Firstautotrawler", shopIconId = "Firstautotrawlershop", repairCost = 200, unlockLevel = 2, maxOwnedAllowed = 2 },
        // 3 lvl //
        new VehicleData { id = "SecondMiniloader", name = "Мини - Погрузчик CM-210", type = VehicleType.Working, group = VehicleGroup.Working, condition = 100, maintenanceCost = 450, price = 12000, iconId = "Secondminiloader", shopIconId = "Secondminiloadershop", repairCost = 850, unlockLevel = 3, maxOwnedAllowed = 2 },
        new VehicleData { id = "TowTruck", name = "Автовоз", type = VehicleType.Transport, group = VehicleGroup.Transport, condition = 100, maintenanceCost = 310, price = 11000, iconId = "Towtruck", shopIconId = "Towtruckshop", repairCost = 650, unlockLevel = 3, maxOwnedAllowed = 2 },
        new VehicleData { id = "CarVan", name = "Вэн Дадж", type = VehicleType.Transport, group = VehicleGroup.Transport, condition = 100, maintenanceCost = 230, price = 5500, iconId = "Carvan", shopIconId = "Carvanshop", repairCost = 450, unlockLevel = 3, maxOwnedAllowed = 2 },
        // 4 lvl //
        new VehicleData { id = "DumpTruck", name = "Самосвал Z-200", type = VehicleType.Working, group = VehicleGroup.Working, condition = 100, maintenanceCost = 450, price = 15000, iconId = "Dumptruck", shopIconId = "Dumptruckshop", repairCost = 1200, unlockLevel = 4, maxOwnedAllowed = 3 },
        // 5 lvl // 
        new VehicleData { id = "RoadRoller", name = "Дорожный каток", type = VehicleType.Working, group = VehicleGroup.Working, condition = 100, maintenanceCost = 850, price = 18000, iconId = "Roadroller", shopIconId = "Roadrollershop", repairCost = 1500, unlockLevel = 5, maxOwnedAllowed = 2 },
        new VehicleData { id = "BigVan", name = "Вэн Купер", type = VehicleType.Transport, group = VehicleGroup.Transport, condition = 100, maintenanceCost = 200, price = 10500, iconId = "Bigvan", shopIconId = "Bigvanshop", repairCost = 500, unlockLevel = 5, maxOwnedAllowed = 2 },
        // 6 lvl //
        new VehicleData { id = "ConcreteMixer", name = "Бетономешалка Альбрус", type = VehicleType.Working, group = VehicleGroup.Working, condition = 100, maintenanceCost = 770, price = 14000, iconId = "Concretemixer", shopIconId = "Concretemixershop", repairCost = 1200, unlockLevel = 6, maxOwnedAllowed = 2 },
        new VehicleData { id = "BigExcavator", name = "Экскаватор ЯК-1", type = VehicleType.Working, group = VehicleGroup.Working, condition = 100, maintenanceCost = 320, price = 35000, iconId = "Bigexcavator", shopIconId = "Bigexcavatorshop", repairCost = 3900, unlockLevel = 6, maxOwnedAllowed = 3 },
        new VehicleData { id = "WorkHouse", name = "Жилой Контейнер", type = VehicleType.Working, group = VehicleGroup.Working, condition = 100, maintenanceCost = 1630, price = 9000, iconId = "Workhouse", shopIconId = "Workhouseshop", repairCost = 400, unlockLevel = 6, maxOwnedAllowed = 2 },
        new VehicleData { id = "TelescopicHandlers", name = "Телескопический погрузчик", type = VehicleType.Working, group = VehicleGroup.Working, condition = 100, maintenanceCost = 1610, price = 30000, iconId = "Telescopichandlers", shopIconId = "Telescopichandlersshop", repairCost = 2000, unlockLevel = 6, maxOwnedAllowed = 1 },
        // 7 lvl //
        new VehicleData { id = "BullDozer", name = "Бульдозер", type = VehicleType.Working, group = VehicleGroup.Working, condition = 100, maintenanceCost = 3400, price = 72000, iconId = "Bulldozer", shopIconId = "Bulldozershop", repairCost = 5200, unlockLevel = 7, maxOwnedAllowed = 3 },
        new VehicleData { id = "BigautoTrawler", name = "Автотрал 6 х 2", type = VehicleType.Transport, group = VehicleGroup.Transport, condition = 100, maintenanceCost = 450, price = 12000, iconId = "Bigautotrawler", shopIconId = "Bigautotrawlershop", repairCost = 900, unlockLevel = 7, maxOwnedAllowed = 1 },
        // 8 lvl //
        new VehicleData { id = "DraglineExcavator", name = "Экскаватор-драглайн (шар баба)", type = VehicleType.Working, group = VehicleGroup.Working, condition = 100, maintenanceCost = 5080, price = 125000, iconId = "Draglineexcavator", shopIconId = "Draglineexcavatorshop", repairCost = 7500, unlockLevel = 8, maxOwnedAllowed = 2 },
        new VehicleData { id = "MultiLift", name = "Мультилифт", type = VehicleType.Transport, group = VehicleGroup.Transport, condition = 100, maintenanceCost = 500, price = 27000, iconId = "Multilift", shopIconId = "Multiliftshop", repairCost = 2100, unlockLevel = 8, maxOwnedAllowed = 2 },
        // 9 lvl //
        new VehicleData { id = "DumpTruck2", name = "Самосвал TR-100", type = VehicleType.Working, group = VehicleGroup.Working, condition = 100, maintenanceCost = 1980, price = 28500, iconId = "Dumptruck2", shopIconId = "Dumptruck2shop", repairCost = 1500, unlockLevel = 9, maxOwnedAllowed = 2 },
        new VehicleData { id = "LoaderGrapple", name = "Захватной погрузчик", type = VehicleType.Working, group = VehicleGroup.Working, condition = 100, maintenanceCost = 1000, price = 19000, iconId = "Loadergrapple", shopIconId = "Loadergrappleshop", repairCost = 1100, unlockLevel = 9, maxOwnedAllowed = 2 },
        // 10 lvl //
        new VehicleData { id = "CranE1", name = "Кран", type = VehicleType.Working, group = VehicleGroup.Working, condition = 100, maintenanceCost = 7070, price = 221000, iconId = "Crane1", shopIconId = "Crane1shop", repairCost = 20100, unlockLevel = 10, maxOwnedAllowed = 3 }
    };

    // ==== Работники ====
    public List<WorkerData> hiredWorkers = new List<WorkerData>();

    // ==== 🧱 Склад ресурсов ====
    public List<WarehouseResource> warehouseResources = new List<WarehouseResource>();

    public int GetWorkerCountById(string workerId)
    {
        if (hiredWorkers == null)
            return 0;

        int count = 0;
        foreach (var w in hiredWorkers)
        {
            if (w.id == workerId && w.isHired)
                count++;
        }
        return count;
    }

    public void AddXp(int amount)
    {
        if (level >= 10) return;
        xp += amount;
        while (xp >= 100 && level < 10)
        {
            xp -= 100;
            level++;
        }
        if (level >= 10)
            xp = 0;
    }

    public void AddMoney(int amount) => money += amount;

    public bool SpendMoney(int amount)
    {
        if (money >= amount)
        {
            money -= amount;
            return true;
        }
        return false;
    }

    public void ResetYearlyReport()
    {
        yearlySalaryExpenses = 0;
        yearlyBills = 0;
        yearlyRepairs = 0;
        yearlyLoanPayments = 0;
        yearlyDebtPayments = 0;
        yearlyPurchases = 0;
        yearlyProfitSmall = 0;
        yearlyProfitMedium = 0;
        yearlyProfitLarge = 0;
        yearlyProfitSpecial = 0;
        yearlyTotalProfit = 0;
    }

    // ==== Выдача стартовых машин игроку ====
    public void GiveStartingVehicles()
    {
        ownedVehicles.Clear();
        AddVehicleById("MiniExcavator");
        AddVehicleById("MiniForklift");
        AddVehicleById("MiniLoader");
        AddVehicleById("FirsTrak");
    }

    public void AddVehicleById(string id)
    {
        VehicleData baseVehicle = vehicles.Find(v => v.id == id);
        if (baseVehicle == null)
        {
            Debug.LogWarning($"[GameData] Машина с id {id} не найдена в каталоге.");
            return;
        }

        // Вычисляем максимально допустимый HP по улучшениям гаража
        int allowedMaxHP = GetGarageMaxVehicleCondition();

        // Стартовое HP техники — минимум из заводского и разрешённого по улучшению
        float finalStartHP = Mathf.Min(baseVehicle.condition, allowedMaxHP);

        VehicleData copy = new VehicleData
        {
            id = baseVehicle.id,
            uniqueId = Guid.NewGuid().ToString(),
            name = baseVehicle.name,
            type = baseVehicle.type,
            group = baseVehicle.group,

            condition = finalStartHP,

            baseMaxHP = 100,

            maintenanceCost = baseVehicle.maintenanceCost,
            price = baseVehicle.price,
            inGarage = true,
            iconId = baseVehicle.iconId,
            repairCost = baseVehicle.repairCost,
            maxOwnedAllowed = baseVehicle.maxOwnedAllowed
        };

        ownedVehicles.Add(copy);
    }



    // ==== Добавлено для СКЛАДА РЕСУРСОВ (общий лимит) ====
    public void AddToWarehouse(string resourceId, int amount)
    {
        if (amount <= 0) return;

        // 1) Общая вместимость склада с учётом бафов Анатолия
        int capacity = GetWarehouseCapacity();
        int usedBefore = GetWarehouseCurrentUsed();

        // Склад уже переполнен
        if (usedBefore >= capacity)
        {
            Debug.Log($"⚠ [GameData] Склад заполнен ({usedBefore}/{capacity}), добавить {amount} ед. {resourceId} нельзя.");
            return;
        }

        int freeSpace = capacity - usedBefore;
        int amountToAdd = Mathf.Min(amount, freeSpace);

        if (amountToAdd <= 0)
        {
            Debug.Log($"⚠ [GameData] Нет свободного места на складе для ресурса {resourceId}.");
            return;
        }

        // 2) Ищем существующую позицию по id
        var res = warehouseResources.Find(r => r.id == resourceId);

        if (res != null)
        {
            res.quantity += amountToAdd;
            Debug.Log($"📦 [GameData] Добавлено {amountToAdd} ед. {res.name} (текущее: {res.quantity}, склад: {usedBefore + amountToAdd}/{capacity})");
        }
        else
        {
            // Красивое название из ResourcesData.json
            string resourceName = resourceId;
            var db = Resources.Load<TextAsset>("Data/ResourcesData");
            if (db != null)
            {
                var wrapper = JsonUtility.FromJson<ResourceDatabase>("{\"categories\":" + db.text + "}");
                foreach (var cat in wrapper.categories)
                {
                    var item = cat.items.Find(i => i.id == resourceId);
                    if (item != null)
                    {
                        resourceName = item.name;
                        break;
                    }
                }
            }

            warehouseResources.Add(new WarehouseResource
            {
                id = resourceId,
                name = resourceName,
                quantity = amountToAdd
            });

            Debug.Log($"🆕 [GameData] Создана новая позиция {resourceName} ({amountToAdd} ед., склад: {usedBefore + amountToAdd}/{capacity})");
        }
    }


    [Serializable]
    public class OrdersCategoryState
    {
        public List<string> currentOrderIds = new List<string>(); // какие id сейчас висят в списке
        public int daysUntilNewOrders = 30;                        // таймер обновления
        public bool needsRegenerate = false;                       // флаг “пересобрать при открытии панели”
    }

    public void PayForemanSalaries()
    {
        int total = 0;
        foreach (var foreman in foremen)
        {
            if (foreman.isHired)
                total += foreman.salary;
        }

        if (total > 0)
        {
            money -= total;
            Debug.Log($"💵 Выплачено зарплат бригадирам: {total}$ ({foremen.Count} чел.)");
            HUDController.Instance?.UpdateMoney(money);
        }
    }

    public void CheckForemanRehireAvailability(int currentDay)
    {
        foreach (var f in foremen)
        {
            if (f.isFired && currentDay >= f.rehireAvailableDay)
            {
                f.isFired = false;
                Debug.Log($"✅ {f.name} снова доступен для найма (прошло 7 дней).");
            }
        }
    }

    [System.Serializable]
    public class PendingPrepaymentData
    {
        public string orderAddress;
        public int amount;
        public int daysRemaining;
    }

    public int GetAvailableSpace(string resourceId)
    {
        int capacity = GetWarehouseCapacity();
        int used = GetWarehouseCurrentUsed();

        int remaining = Mathf.Max(0, capacity - used);
        return remaining;
    }



    public int GetResourceQuantity(string resourceId)
    {
        var res = warehouseResources.Find(r => r.id == resourceId);
        return res != null ? res.quantity : 0;
    }

    public bool RemoveFromWarehouse(string resourceId, int amount)
    {
        var res = warehouseResources.Find(r => r.id == resourceId);
        if (res == null || res.quantity < amount)
            return false;

        res.quantity -= amount;
        if (res.quantity <= 0)
            warehouseResources.Remove(res);

        return true;
    }

    // ==== Выдача стартовых работников ====
    public void GiveStartingWorkers()
    {
        if (hiredWorkers == null)
            hiredWorkers = new List<WorkerData>();

        if (hiredWorkers.Count > 0)
        {
            Debug.Log($"📋 Обнаружено уже нанятых работников: {hiredWorkers.Count}. Стартовые не добавляются повторно.");
            return;
        }

        if (WorkersDatabase.Instance == null || WorkersDatabase.Instance.workers == null)
        {
            Debug.LogWarning("⚠ WorkersDatabase не загружена — стартовые работники не добавлены.");
            return;
        }

        var allWorkers = WorkersDatabase.Instance.workers;

        var builders = allWorkers
            .Where(w =>
                w.profession != null &&
                w.category != null &&
                w.profession.Trim().ToLower().StartsWith("разнорабочий") &&
                w.category.Trim().ToLower() == "стройка" &&
                w.appearanceLevel == 1)
            .Take(3)
            .ToList();

        var secretary = allWorkers
            .FirstOrDefault(w =>
                w.profession != null &&
                w.category != null &&
                w.profession.Trim().ToLower().StartsWith("секретарь") &&
                w.category.Trim().ToLower() == "офис" &&
                w.appearanceLevel == 1);

        foreach (var worker in builders)
        {
            if (worker == null) continue;
            worker.isHired = true;
            hiredWorkers.Add(worker);
            Debug.Log($"👷 Добавлен строитель: {worker.firstName} {worker.lastName}");
        }

        if (secretary != null)
        {
            secretary.isHired = true;
            hiredWorkers.Add(secretary);
            Debug.Log($"💼 Добавлен офисный сотрудник: {secretary.firstName} {secretary.lastName}");
        }
        else
        {
            Debug.LogWarning("⚠ Не найден офисный секретарь в базе workers.json!");
        }

        Debug.Log($"✅ Стартовые работники добавлены: {builders.Count} строителей и {(secretary != null ? 1 : 0)} офисный");
    }

    // === ДОБАВЛЕНО ДЛЯ СИСТЕМЫ FORBES ===
    public int homeLevel = 0;
    public int playerLevel => level;

    public int GetWorkerCount()
    {
        return hiredWorkers != null ? hiredWorkers.Count : 0;
    }

    public int GetOwnedVehiclesCount()
    {
        return ownedVehicles != null ? ownedVehicles.Count : 0;
    }

    public BrigadeData GetBrigadeByName(string name)
    {
        if (allBrigades == null || allBrigades.Count == 0)
            return null;

        return allBrigades.FirstOrDefault(b => b.name == name);
    }

    // ============================================================
    // === СОЗДАНИЕ НОВОЙ БРИГАДЫ ===
    // ============================================================
    public BrigadeData AddNewBrigade(string foremanId, string name)
    {
        BrigadeData brigade = new BrigadeData
        {
            id = System.Guid.NewGuid().ToString(),
            foremanId = foremanId,
            name = name,
            mood = 100,                // 💯 стартовое настроение
            completedOrders = 0,
            isSelected = false,
            workers = new List<WorkerData>()
        };

        allBrigades.Add(brigade);
        Debug.Log($"🆕 Создана новая бригада: {brigade.name} (настроение = {brigade.mood})");
        return brigade;
    }

    // Прогресс улучшений охранника (3 вкладки по 5 уровней)
    public int anatoliyGuardTabLevel = 0;      // Territory
    public int anatoliyGarageTabLevel = 0;     // Garage
    public int anatoliyWarehouseTabLevel = 0;  // Warehouse

    // =========================================================
    // === Улучшения TERRITORY (Анатолий, вкладка Terra) =======
    // =========================================================

    /// <summary>Текущий уровень Terra (0–5)</summary>
    public int GetTerraLevel()
    {
        return Mathf.Clamp(anatoliyGuardTabLevel, 0, 5);
    }

    /// <summary>Удобная проверка для ивентов: есть ли минимум такой уровень.</summary>
    public bool HasTerraLevel(int level)
    {
        return anatoliyGuardTabLevel >= level;
    }

    // Примеры тегов для документации/ивентов:
    // terra_lvl1, terra_lvl2, terra_lvl3, terra_lvl4, terra_lvl5


    // =========================================================
    // === Улучшения GARAGE (лимит машин и максимальный HP) ====
    // =========================================================

    /// <summary>
    /// Максимальное количество машин, которое игрок может иметь одновременно.
    /// База: 10
    /// Lvl1: 20
    /// Lvl3: 30
    /// Lvl5: 50
    /// </summary>
    public int GetGarageVehicleLimit()
    {
        int lvl = Mathf.Clamp(anatoliyGarageTabLevel, 0, 5);

        switch (lvl)
        {
            case 0: return 10;
            case 1: return 20;
            case 2: return 30;
            case 3: return 35;
            case 4: return 35;
            case 5: return 65;
        }

        return 10;
    }

    public int GetRepairDaysReduction()
    {
        int lvl = Mathf.Clamp(anatoliyGarageTabLevel, 0, 5);

        if (lvl >= 3) return 2; // уровень 3 и выше → -2 дня

        return 0;
    }


    /// <summary>
    /// Максимальное "здоровье" техники (в процентах).
    /// База: 100%
    /// Lvl2: 110%
    /// Lvl4: 120%
    /// </summary>
    public int GetGarageMaxVehicleCondition()
    {
        int level = Mathf.Clamp(anatoliyGarageTabLevel, 0, 5);

        if (level >= 4) return 120;
        if (level >= 2) return 110;

        return 100;
    }


    // =========================================================
    // === Улучшения WAREHOUSE (вместимость + скидки) ==========
    // =========================================================

    /// <summary>
    /// Общая вместимость склада.
    /// База: 400
    /// Lvl1: +50  (450)
    /// Lvl2: +150 (600)
    /// Lvl4: +250 (850)
    /// </summary>
    public int GetWarehouseCapacity()
    {
        int cap = 400;
        int level = Mathf.Clamp(anatoliyWarehouseTabLevel, 0, 5);

        if (level >= 1) cap += 50;   // 450
        if (level >= 2) cap += 150;  // 600
        if (level >= 4) cap += 250;  // 850

        return cap;
    }

    /// <summary>Сколько сейчас всего единиц ресурсов на складе (сумма по всем позициям).</summary>
    public int GetWarehouseCurrentUsed()
    {
        if (warehouseResources == null || warehouseResources.Count == 0)
            return 0;

        int total = 0;
        foreach (var r in warehouseResources)
        {
            if (r != null)
                total += r.quantity;
        }
        return total;
    }

    /// <summary>
    /// Множитель скидки на ресурсы.
    /// Lvl0-2: 1.0  (0%)
    /// Lvl3-4: 0.95 (-5%)
    /// Lvl5:   0.90 (-10%)
    /// </summary>
    public float GetWarehouseDiscountMultiplier()
    {
        int level = Mathf.Clamp(anatoliyWarehouseTabLevel, 0, 5);

        if (level >= 5) return 0.90f;
        if (level >= 3) return 0.95f;

        return 1f;
    }

    /// <summary>
    /// Применяем улучшение гаража ко всем машинам:
    /// - машины с 100% повышаются до нового максимума (110 или 120)
    /// - побитые машины остаются как есть
    /// - машины не могут превышать максимальный HP
    /// </summary>
    public void ApplyGarageHPBuffToAllVehicles()
    {
        int maxHP = GetGarageMaxVehicleCondition();
        if (ownedVehicles == null || ownedVehicles.Count == 0)
            return;

        foreach (var v in ownedVehicles)
        {
            if (v == null) continue;

            // Если была полностью исправна — обновляем до нового максимума
            if (v.condition >= 100f)
                v.condition = maxHP;

            // Подрезаем, если вдруг HP выше лимита
            if (v.condition > maxHP)
                v.condition = maxHP;
        }
    }

    public void ClampAllVehicleHP()
    {
        int maxHP = GetGarageMaxVehicleCondition();

        if (ownedVehicles == null) return;

        foreach (var v in ownedVehicles)
        {
            if (v == null) continue;

            // Не допускаем превышения максимально разрешённого HP
            if (v.condition > maxHP)
                v.condition = maxHP;
        }
    }

    [System.Serializable]
    public class YearReportData
    {
        public int year;

        // --- расходы ---
        public int salaryExpenses;
        public int bills;
        public int repairs;
        public int loanInterest;
        public int debtPayments;
        public int purchases;

        // --- доходы ---
        public int profitSmall;
        public int profitMedium;
        public int profitLarge;
        public int profitSpecial;

        // --- итог ---
        public int totalProfit;
    }

    // === СПИСОК ОТЧЁТОВ ПО ГОДАМ ===
    public List<YearReportData> yearlyReports = new List<YearReportData>();

    public YearReportData GetReportForYear(int year)
    {
        return yearlyReports.Find(r => r.year == year);
    }

    public List<int> GetAvailableReportYears()
    {
        List<int> result = new List<int>();
        foreach (var r in yearlyReports)
            result.Add(r.year);
        return result;
    }


}
