using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // === Синглтон ===
    public static GameManager Instance;

    public GameData CurrentGame;
    public int CurrentSlot { get; private set; } = -1;

    // 🚩 Флаг: открыт ли UI (банк, меню и т.д.)
    public bool IsUIOpen { get; set; } = false;

    // 🚩 Откуда мы перешли в текущую сцену
    public string LastSceneEntryPoint { get; set; } = "";

    // === Новое свойство для глобального доступа ===
    public GameData Data => CurrentGame;

    // === 🔹 Новое: глобальная база ресурсов (из магазина Гарри) ===
    public ResourceDatabase ResourcesData { get; private set; }

    private void Awake()
    {
        // ✅ Защита от дубликатов GameManager при переходах между сценами
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    private void Start()
    {
        if (CurrentGame == null)
        {
            CurrentGame = new GameData();
            Debug.Log("[GameManager] Создана новая GameData в Start()");
        }

        // Если уровень не задан — по умолчанию 1 и 0 XP
        if (CurrentGame.level <= 0) CurrentGame.level = 10;
        if (CurrentGame.xp < 0) CurrentGame.xp = 0;

        // если у игрока нет техники — добавляем стартовые машины
        if (CurrentGame.ownedVehicles == null || CurrentGame.ownedVehicles.Count == 0)
        {
            Debug.Log("[GameManager] Добавляем стартовые машины...");
            AddVehicleToPlayer("MiniExcavator");
            AddVehicleToPlayer("MiniForklift");
            AddVehicleToPlayer("MiniLoader");
            AddVehicleToPlayer("FirsTrak");
            Debug.Log($"[GameManager] Техника добавлена: {CurrentGame.ownedVehicles.Count} шт.");
        }

        // 🧱 Запуск безопасного добавления стартовых работников
        StartCoroutine(AddStartingWorkersDelayed());

        // === 🧱 Проверка и добавление стартового бригадира + автогенерация бригад ===
        if (CurrentGame.foremen == null)
            CurrentGame.foremen = new List<ForemanData>();

        // --- 1️⃣ Добавляем Ивана Петрова, если его нет ---
        bool hasPetrov = CurrentGame.foremen.Exists(f => f.name == "Иван Петров");
        if (!hasPetrov)
        {
            CurrentGame.foremen.Add(new ForemanData()
            {
                id = "f1_1",
                name = "Иван Петров",
                isHired = true,
                isFired = false,
                requiredLevel = 1,
                hireCost = 1200,
                salary = 200,
                extraBrigades = 0,
                iconId = "foreman_icon_1",
                brigades = new List<BrigadeData>()
            });
            Debug.Log("✅ Иван Петров добавлен как стартовый бригадир.");
        }

        // --- 2️⃣ Автоматически создаём бригады для всех нанятых ---
        foreach (var foreman in CurrentGame.foremen)
        {
            if (foreman.isHired)
            {
                if (foreman.brigades == null)
                    foreman.brigades = new List<BrigadeData>();

                if (foreman.brigades.Count == 0)
                {
                    int count = Mathf.Max(1, foreman.extraBrigades + 1);
                    for (int i = 1; i <= count; i++)
                    {
                        BrigadeData newBrigade = new BrigadeData
                        {
                            id = $"{foreman.id}_brigade_{i}",
                            foremanId = foreman.id,
                            name = $"Бригада {foreman.name} №{i}",
                            workers = new List<WorkerData>(),
                            completedOrders = 0
                        };
                        foreman.brigades.Add(newBrigade);

                        if (CurrentGame.allBrigades == null)
                            CurrentGame.allBrigades = new List<BrigadeData>();
                        CurrentGame.allBrigades.Add(newBrigade);
                    }

                    Debug.Log($"👷 {foreman.name}: создано {foreman.brigades.Count} бригад(ы).");
                }
            }
        }
    }

    // === 📦 Загрузка базы материалов ===
    private void LoadResourcesDatabase()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Data/ResourcesData");
        if (jsonFile == null)
        {
            Debug.LogError("❌ Не найден файл ResourcesData.json в папке Resources/Data/");
            return;
        }

        ResourcesData = JsonUtility.FromJson<ResourceDatabase>("{\"categories\":" + jsonFile.text + "}");
        Debug.Log($"✅ Загружено {ResourcesData.categories.Count} категорий материалов");
    }

    // === 🔎 Получение названия материала по ID (для заказов) ===
    public string GetMaterialNameById(string id)
    {
        if (ResourcesData == null || ResourcesData.categories == null)
            return id;

        foreach (var cat in ResourcesData.categories)
        {
            var item = cat.items.Find(i => i.id == id);
            if (item != null)
                return item.name;
        }
        return id;
    }

    // === 💰 Финансы ===
    public void AddMoney(int amount)
    {
        if (CurrentGame == null) return;
        CurrentGame.AddMoney(amount);
        Debug.Log($"[GameManager] Добавлено {amount}$ (итого: {CurrentGame.money}$)");
    }

    public bool SpendMoney(int amount)
    {
        if (CurrentGame == null) return false;
        bool result = CurrentGame.SpendMoney(amount);
        if (result)
            Debug.Log($"[GameManager] Потрачено {amount}$ (осталось: {CurrentGame.money}$)");
        else
            Debug.Log($"[GameManager] Недостаточно средств для оплаты {amount}$");
        return result;
    }

    public void AddXP(int amount)
    {
        if (CurrentGame == null) return;
        CurrentGame.AddXp(amount);
        Debug.Log($"[GameManager] Получено {amount} XP (уровень: {CurrentGame.level})");
    }

    // === 🚜 Добавление техники игроку ===
    public void AddVehicleToPlayer(string modelId)
    {
        if (CurrentGame == null)
        {
            Debug.LogWarning("[GameManager] CurrentGame не инициализирован.");
            return;
        }

        VehicleData model = CurrentGame.vehicles.Find(v => v.id == modelId);
        if (model == null)
        {
            Debug.LogWarning($"[GameManager] Модель {modelId} не найдена в каталоге.");
            return;
        }

        string unique = $"{model.id}#{System.Guid.NewGuid().ToString().Substring(0, 4)}";

        int allowedMaxHP = CurrentGame.GetGarageMaxVehicleCondition();
        float startHP = Mathf.Min(model.condition, allowedMaxHP);

        VehicleData owned = new VehicleData()
        {
            id = model.id,
            uniqueId = unique,
            name = model.name,
            type = model.type,
            group = model.group,

            condition = startHP,
            baseMaxHP = 100,

            maintenanceCost = model.maintenanceCost,
            price = model.price,
            iconId = model.iconId,
            repairCost = model.repairCost,
            maxOwnedAllowed = model.maxOwnedAllowed,
            inGarage = true
        };

        CurrentGame.ownedVehicles.Add(owned);

        Debug.Log($"[GameManager] Игрок получил технику: {owned.name} ({unique}), HP: {owned.condition}/{allowedMaxHP}");
    }


    // === ⚙️ Установка активного сохранения ===
    public void SetCurrentGame(GameData data, int slot)
    {
        CurrentGame = data;
        CurrentSlot = slot;

        if (CurrentGame.level <= 0) CurrentGame.level = 1;
        if (CurrentGame.xp < 0) CurrentGame.xp = 0;

        Debug.Log($"[GameManager] Активный слот = {slot}, компания = {data?.companyName}");

        if (CurrentGame.ownedVehicles == null)
            CurrentGame.ownedVehicles = new List<VehicleData>();
        if (CurrentGame.hiredWorkers == null)
            CurrentGame.hiredWorkers = new List<WorkerData>();
        if (CurrentGame.warehouseResources == null)
            CurrentGame.warehouseResources = new List<WarehouseResource>();
        if (CurrentGame.activeOrders == null)
            CurrentGame.activeOrders = new List<OrderData>();
        if (CurrentGame.completedOrders == null)
            CurrentGame.completedOrders = new List<string>();

        CurrentGame.ClampAllVehicleHP();

        if (CurrentGame.ownedVehicles.Count == 0)
        {
            AddVehicleToPlayer("MiniExcavator");
            AddVehicleToPlayer("MiniForklift");
            AddVehicleToPlayer("MiniLoader");
            AddVehicleToPlayer("FirsTrak");
            Debug.Log("[GameManager] Стартовые машины добавлены");
        }

        StartCoroutine(AddStartingWorkersDelayed());

        if (HUDController.Instance != null && data != null)
        {
            HUDController.Instance.UpdateHUD(data);
            if (TimeController.Instance != null && TimeController.Instance.GameSpeed > 0f)
                TimeController.Instance.SetPause();
            HUDController.Instance.UpdateSpeedButtonColors();
        }

        if (TimeController.Instance != null && data != null)
            TimeController.Instance.LoadFromGameData(data);

        GameManager.Instance.IsUIOpen = false;
        Debug.Log("[GameManager] Слот успешно активирован и инициализирован.");
    }

    // === 🧱 Короутина для добавления стартовых работников ===
    private IEnumerator AddStartingWorkersDelayed()
    {
        yield return new WaitForSeconds(1f);

        if (CurrentGame == null)
            yield break;

        if (CurrentGame.hiredWorkers == null)
            CurrentGame.hiredWorkers = new List<WorkerData>();

        if (CurrentGame.hiredWorkers.Count == 0)
        {
            if (WorkersDatabase.Instance == null || WorkersDatabase.Instance.workers == null)
            {
                Debug.LogWarning("⚠ WorkersDatabase не загружена — повтор через 1 сек.");
                yield return new WaitForSeconds(1f);
            }

            CurrentGame.GiveStartingWorkers();
            Debug.Log("✅ Стартовые работники добавлены (новая игра).");
        }
        else
        {
            Debug.Log($"ℹ Обнаружено уже нанятых работников: {CurrentGame.hiredWorkers.Count}. Стартовые не добавляются повторно.");
        }
    }

    // === 👷 Получение названия профессии по ID (для заказов) ===
    public string GetProfessionNameById(string id)
    {
        switch (id)
        {
            case "p01_carpenter": return "Плотник";
            case "p02_painter": return "Маляр";
            case "p03_electrician": return "Электрик";
            case "p04_engineer": return "Инженер";
            case "p05_welder": return "Сварщик";
            case "p06_laborer": return "Разнорабочий";
            case "p07_plumber": return "Сантехник";
            case "p08_concreter": return "Бетонщик";
            case "p09_surveyor": return "Геодезист";
            case "p10_roofer": return "Кровельщик";
            default: return id ?? "Рабочий";
        }
    }
}
