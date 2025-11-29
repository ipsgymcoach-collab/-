using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class WorkersDatabase
{
    public List<WorkerData> workers = new List<WorkerData>();

    private static WorkersDatabase _instance;
    public static WorkersDatabase Instance
    {
        get
        {
            if (_instance == null) _instance = LoadDatabase();
            return _instance;
        }
    }

    // 🎯 Сколько работников генерируем
    private const int ConstructionWorkersTarget = 500;
    private const int OfficeWorkersTarget = 250;

    // === Таблица имён и фамилий (сейчас русские, потом можно добавить ENG) ===
    private static readonly string[] FirstNames = new string[]
    {
    "Алексей","Андрей","Антон","Артём","Богдан","Борис","Вадим","Валентин","Виктор","Владислав",
    "Вячеслав","Георгий","Денис","Дмитрий","Евгений","Егор","Иван","Илья","Кирилл","Константин",
    "Леонид","Максим","Михаил","Николай","Олег","Павел","Роман","Сергей","Степан","Юрий",
    "Артемий","Александр","Пётр","Григорий","Матвей","Савелий","Ярослав","Марк","Тимофей","Фёдор",
    "Прохор","Лука","Семён","Тихон","Емельян","Платон","Назар","Всеволод","Радимир","Елисей",
    "Арсен","Арсений","Дамир","Руслан","Виталий","Глеб","Артёмий","Ростислав","Мирон","Марат",
    "Альберт","Стас","Игнат","Добрыня","Давид","Леон","Серафим","Эмиль","Артур","Тамир",
    "Феликс","Клим","Еремей","Валерий","Святослав","Аристарх","Ефим","Оскар","Эрик","Ян"
    };


    private static readonly string[] LastNames = new string[]
    {
    "Иванов","Петров","Сидоров","Смирнов","Кузнецов","Попов","Васильев","Соколов","Михайлов","Новиков",
    "Фёдоров","Морозов","Волков","Алексеев","Лебедев","Семёнов","Егоров","Павлов","Ковалёв","Голубев",
    "Виноградов","Богданов","Жуков","Орлов","Тарасов","Беляев","Захаров","Киселёв","Николаев","Шестаков",
    "Куликов","Макаров","Филиппов","Комиссаров","Гордеев","Грачёв","Елизаров","Дроздов","Сысоев","Карпов",
    "Шубин","Гаврилов","Медведев","Королёв","Родионов","Сурков","Щукин","Кондратьев","Пахомов","Бирюков",
    "Горбунов","Герасимов","Архипов","Титов","Фомин","Сафронов","Козлов","Костин","Борисов","Березин",
    "Ларионов","Соловьёв","Краснов","Анисимов","Лукин","Мельников","Калинин","Ефимов","Наумов","Фролов",
    "Гордеев","Завьялов","Трофимов","Сурнин","Семенчиков","Баженов","Плотников","Хромов","Сергеев","Пронин"
    };


    // === Конфиги профессий (по данным из старой базы workers.json) ===
    private struct ProfessionConfig
    {
        public string id;
        public string name;
        public string category;
        public int minSalary;
        public int maxSalary;
        public int appearanceLevel;

        public ProfessionConfig(
            string id,
            string name,
            string category,
            int minSalary,
            int maxSalary,
            int appearanceLevel)
        {
            this.id = id;
            this.name = name;
            this.category = category;
            this.minSalary = minSalary;
            this.maxSalary = maxSalary;
            this.appearanceLevel = appearanceLevel;
        }
    }


    // ⚙️ Здесь мы повторяем те же профессии, что были в JSON
    private static readonly ProfessionConfig[] ProfessionConfigs = new ProfessionConfig[]
    {
    // ==== Стройка ====
    new ProfessionConfig("p06_laborer",     "Разнорабочий",        "Стройка", 881,  1192, 1),  // lvl 1
    new ProfessionConfig("p03_electrician", "Электрик",            "Стройка", 1113, 1489, 3),  // lvl 3
    new ProfessionConfig("p09_surveyor",    "Геодезист",           "Стройка", 1105, 1500, 2),  // lvl 2
    new ProfessionConfig("p01_carpenter",   "Плотник",             "Стройка", 1435, 1892, 2),  // lvl 2
    new ProfessionConfig("p02_painter",     "Маляр",               "Стройка", 1524, 1771, 1),  // lvl 1
    new ProfessionConfig("p20_mounter",     "Монтажник",           "Стройка", 1523, 1983, 1),  // lvl 1
    new ProfessionConfig("p05_welder",      "Сварщик",             "Стройка", 1503, 1996, 4),  // lvl 4
    new ProfessionConfig("p21_mason",       "Каменщик",            "Стройка", 1489, 1865, 3),  // lvl 3
    new ProfessionConfig("p08_concreter",   "Бетонщик",            "Стройка", 1857, 2394, 3),  // lvl 3
    new ProfessionConfig("p22_rollerop",    "Машинист катка",      "Стройка", 2023, 2562, 5),  // lvl 5
    new ProfessionConfig("p23_asphalt",     "Асфальтобетонщик",    "Стройка", 1821, 2381, 4),  // lvl 4
    new ProfessionConfig("p24_plasterer",   "Штукатур",            "Стройка", 2203, 2866, 5),  // lvl 5
    new ProfessionConfig("p25_foreman",     "Прораб",              "Стройка", 2500, 3190, 6),  // lvl 6
    new ProfessionConfig("p26_craneman",    "Крановщик",           "Стройка", 4046, 5960, 7),  // lvl 7

    // ==== Офис ====
    new ProfessionConfig("p10_secretary1",  "Секретарь 1",         "Офис",    1553, 1553, 1),  // lvl 1
    new ProfessionConfig("p11_secretary2",  "Секретарь 2",         "Офис",    1616, 1880, 1),  // lvl 1
    new ProfessionConfig("p12_secretary3",  "Секретарь 3",         "Офис",    1514, 1769, 1),  // lvl 1
    new ProfessionConfig("p13_secretary4",  "Секретарь 4",         "Офис",    1607, 1848, 1),  // lvl 1
    new ProfessionConfig("p14_secretary5",  "Секретарь 5",         "Офис",    1583, 1833, 1),  // lvl 1

    new ProfessionConfig("p19_recruiter",   "Рекрутер",            "Офис",    2726, 3410, 2),  // lvl 2
    new ProfessionConfig("p15_accountant",  "Бухгалтер",           "Офис",    2157, 2769, 3),  // lvl 3
    new ProfessionConfig("p18_economist",   "Экономист",           "Офис",    2515, 3166, 4),  // lvl 4
    new ProfessionConfig("p17_analyst",     "Аналитик",            "Офис",    2607, 3481, 5),  // lvl 5
    new ProfessionConfig("p31_planner",     "Планировщик",         "Офис",    3013, 3737, 5),  // lvl 5
    new ProfessionConfig("p30_hr",          "HR",                  "Офис",    2864, 3578, 6),  // lvl 6
    new ProfessionConfig("p32_marketer",    "Маркетолог",          "Офис",    3504, 4430, 7),  // lvl 7
    new ProfessionConfig("p16_manager",     "Менеджер проектов",   "Офис",    2201, 2963, 7)   // lvl 7
    };


    // 🔁 Вместо чтения JSON — создаём базу работников в рантайме
    private static WorkersDatabase LoadDatabase()
    {
        var db = new WorkersDatabase();
        db.GenerateWorkers();
        Debug.Log($"[WorkersDatabase] Сгенерировано {db.workers.Count} работников (строители + офис)");
        return db;
    }

    /// <summary>
    /// Генерирует полную базу работников (строители + офис)
    /// </summary>
    private void GenerateWorkers()
    {
        workers = new List<WorkerData>();

        // Фиксированный сид — чтобы при каждом запуске одной и той же игры
        // список был одинаковый (важно для сохранений)
        System.Random rng = new System.Random(12345);

        GenerateWorkersForCategory("Стройка", ConstructionWorkersTarget, rng);
        GenerateWorkersForCategory("Офис", OfficeWorkersTarget, rng);
    }

    private void GenerateWorkersForCategory(string category, int targetCount, System.Random rng)
    {
        var configs = ProfessionConfigs.Where(c => c.category == category).ToList();
        if (configs.Count == 0)
        {
            Debug.LogError($"[WorkersDatabase] Нет ProfessionConfig для категории {category}");
            return;
        }

        for (int i = 0; i < targetCount; i++)
        {
            var cfg = configs[rng.Next(configs.Count)];

            var worker = new WorkerData();

            // ID в стиле b001 / o001 (совместимо со старой базой)
            string prefix = category == "Стройка" ? "b" : "o";
            worker.id = prefix + (i + 1).ToString("D3");

            // Имя и фамилия
            worker.firstName = FirstNames[rng.Next(FirstNames.Length)];
            worker.lastName = LastNames[rng.Next(LastNames.Length)];

            worker.category = category;
            worker.profession = CleanProfession(cfg.name);
            worker.professionId = cfg.id;

            // Уровень появления 1–10, с перекосом в сторону низких уровней
            int roll = rng.Next(0, 100);
            int appearanceLvl;
            if (roll < 40) appearanceLvl = rng.Next(1, 4);   // 40% — уровни 1–3
            else if (roll < 75) appearanceLvl = rng.Next(3, 7);   // 35% — 3–6
            else if (roll < 95) appearanceLvl = rng.Next(6, 9);   // 20% — 6–8
            else appearanceLvl = rng.Next(9, 11);  // 5%  — 9–10

            worker.appearanceLevel = Mathf.Clamp(appearanceLvl, 1, 10);

            // Навык работника — пока у всех 1
            worker.skillLevel = 1;

            // Зарплата — интерполяция между min и max с учётом уровня ПОЯВЛЕНИЯ + небольшой рандом
            float t = (worker.appearanceLevel - 1) / 9f; // 0..1
            float baseSalary = Mathf.Lerp(cfg.minSalary, cfg.maxSalary, t);

            float randFactor = 0.9f + (float)rng.NextDouble() * 0.2f; // ±10%
            int salary = Mathf.RoundToInt(baseSalary * randFactor);

            worker.salary = salary;
            worker.hireCost = salary + rng.Next(400, 1200);
            worker.upgradeCost = Mathf.RoundToInt(salary * 0.7f) + rng.Next(200, 800);

            worker.isHired = false;

            workers.Add(worker);
        }
    }

    // ====== Таблица базовых квот по уровню игрока (как в старой версии) ======
    private struct MarketQuota
    {
        public int total, build, office;
        public MarketQuota(int total, int build, int office)
        {
            this.total = total;
            this.build = build;
            this.office = office;
        }
    }

    private static readonly Dictionary<int, MarketQuota> quotas = new()
    {
        {1,  new MarketQuota(10, 6, 4)},
        {2,  new MarketQuota(13, 7, 5)},
        {3,  new MarketQuota(13, 7, 5)},
        {4,  new MarketQuota(15, 9, 6)},
        {5,  new MarketQuota(15, 9, 6)},
        {6,  new MarketQuota(15, 9, 6)},
        {7,  new MarketQuota(17,10, 7)},
        {8,  new MarketQuota(20,13, 7)},
        {9,  new MarketQuota(25,13,12)},
        {10, new MarketQuota(35,23,12)}
    };

    // ====== Лимиты по профессиям (оставляем как было) ======
    private readonly Dictionary<string, int> professionLimits = new()
    {
        {"Бухгалтер", 2},
        {"Секретарь", 2},
        {"Экономист", 2},
        {"Рекрутер", 1},
        {"Аналитик", 1}
    };

    public bool IsProfessionLimited(string profession)
    {
        if (!professionLimits.ContainsKey(profession)) return false;

        int currentCount = 0;
        if (GameManager.Instance != null && GameManager.Instance.CurrentGame.hiredWorkers != null)
        {
            currentCount = GameManager.Instance.CurrentGame.hiredWorkers
                .Count(w => w.profession == profession);
        }

        return currentCount >= professionLimits[profession];
    }

    // 👷 Получить название профессии по id работника (для заказов / SuburbOrdersPanel)
    public string GetWorkerNameById(string id)
    {
        var worker = workers.FirstOrDefault(w => w.id == id);
        if (worker == null) return "???";

        if (!string.IsNullOrEmpty(worker.professionName))
            return worker.professionName;

        return worker.profession;
    }

    /// <summary>
    /// Возвращает список работников для биржи труда
    /// с квотами по уровню игрока и небольшим количеством работников с прошлых уровней.
    /// </summary>
    public List<WorkerData> GetWorkersForLevel(int playerLevel)
    {
        if (!quotas.TryGetValue(playerLevel, out MarketQuota baseQuota))
            baseQuota = quotas[1];

        // ==== 1️⃣ Основной пул — текущий уровень появления ====
        var mainPool = workers
            .Where(w => !w.isHired && !w.recentlyFired && w.appearanceLevel == playerLevel)
            .ToList();

        // ==== 2️⃣ Дополнительные ячейки со старых уровней ====
        int extraSlots = Mathf.Max(0, (playerLevel - 1) * 2);
        var lowLevelPool = workers
            .Where(w => !w.isHired && !w.recentlyFired && w.appearanceLevel < playerLevel)
            .OrderByDescending(w => w.appearanceLevel)
            .ToList();

        // ==== 3️⃣ Убираем профессии, где достигнут лимит ====
        mainPool = mainPool.Where(w => !IsProfessionLimited(w.profession)).ToList();
        lowLevelPool = lowLevelPool.Where(w => !IsProfessionLimited(w.profession)).ToList();

        // ==== 4️⃣ Разделяем по категориям ====
        var mainBuild = mainPool.Where(w => w.category == "Стройка").ToList();
        var mainOffice = mainPool.Where(w => w.category == "Офис").ToList();

        var lowBuild = lowLevelPool.Where(w => w.category == "Стройка").ToList();
        var lowOffice = lowLevelPool.Where(w => w.category == "Офис").ToList();

        // ==== 5️⃣ Финальный результат ====
        List<WorkerData> result = new();
        var usedIds = new HashSet<string>();
        var professionCount = new Dictionary<string, int>();

        void TakeRandom(List<WorkerData> pool, int count)
        {
            int attempts = 0;
            while (count > 0 && pool.Count > 0 && attempts < 500)
            {
                attempts++;
                int idx = Random.Range(0, pool.Count);
                var w = pool[idx];
                pool.RemoveAt(idx);

                if (w == null || usedIds.Contains(w.id)) continue;
                if (professionCount.ContainsKey(w.profession) && professionCount[w.profession] >= 2)
                    continue;

                usedIds.Add(w.id);

                if (!professionCount.ContainsKey(w.profession))
                    professionCount[w.profession] = 0;
                professionCount[w.profession]++;

                result.Add(w);
                count--;
            }
        }

        TakeRandom(mainBuild, baseQuota.build);
        TakeRandom(mainOffice, baseQuota.office);

        if (extraSlots > 0)
        {
            int buildExtras = Mathf.CeilToInt(extraSlots * 0.6f);
            int officeExtras = extraSlots - buildExtras;

            TakeRandom(lowBuild, buildExtras);
            TakeRandom(lowOffice, officeExtras);
        }

        int missing = (baseQuota.total + extraSlots) - result.Count;
        if (missing > 0)
        {
            var leftovers = workers
                .Where(w => !w.isHired && !w.recentlyFired && !usedIds.Contains(w.id))
                .ToList();
            TakeRandom(leftovers, missing);
        }

        Debug.Log($"[WorkersDatabase] Биржа уровня {playerLevel}: подобрано {result.Count} работников (вкл. {extraSlots} с прошлых уровней)");
        return result;
    }

    // === Обновление отдыха уволенных ===
    public void UpdateRestDays()
    {
        foreach (var worker in workers)
        {
            if (worker.recentlyFired && worker.restDaysLeft > 0)
            {
                worker.restDaysLeft--;

                if (worker.restDaysLeft <= 0)
                {
                    worker.recentlyFired = false;
                    Debug.Log($"✅ {worker.firstName} {worker.lastName} снова доступен для найма.");
                }
            }
        }
    }

    private string CleanProfession(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return raw;

        var parts = raw.Split(' ');

        if (parts.Length >= 2 && int.TryParse(parts[1], out _))
            return parts[0];

        return raw;
    }

}
