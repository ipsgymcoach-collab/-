using System.Collections.Generic;
using UnityEngine;

public class ForbesManager : MonoBehaviour
{
    public static ForbesManager Instance;

    [Header("Настройки генерации компаний")]
    [SerializeField] private int totalCompanies = 100;
    [SerializeField] private float minWealth = 50f;   // млн $
    [SerializeField] private float maxWealth = 5000f; // млн $

    [Header("Данные")]
    public List<CompanyRankData> companies = new List<CompanyRankData>();
    public CompanyRankData playerCompany;

    private string[] prefixes = { "Build", "Sky", "Prime", "Urban", "Iron", "Next", "Pro", "Delta", "Solid", "Stone" };
    private string[] suffixes = { "Construct", "Builders", "Group", "Company", "Development", "Corp", "Industries" };
    private string[] ceoNames = { "Алексей Орлов", "Джон Миллер", "Иван Петров", "Сергей Нестеров", "Роман Ковалев", "Майкл Дженкинс", "Питер Картер", "Ричард Смит" };

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        GenerateCompanies();
    }

    private void GenerateCompanies()
    {
        companies.Clear();

        // === 🏗 3 фиксированные компании-лидера ===
        companies.Clear();

        // === ТОП-100 строительных компаний (вымышленные) ===
        companies.Add(new CompanyRankData("TitanBuild Corporation", "Алексей Орлов", 4800f));
        companies.Add(new CompanyRankData("Skyline Development", "Джон Миллер", 3650f));
        companies.Add(new CompanyRankData("UrbanCore Group", "Роман Ковалёв", 2900f));
        companies.Add(new CompanyRankData("IronStone Construction", "Павел Нестеров", 2800f));
        companies.Add(new CompanyRankData("NextEra Builders", "Дмитрий Сафонов", 2700f));
        companies.Add(new CompanyRankData("DeltaFrame Construction", "Роберт Хендерсон", 2600f));
        companies.Add(new CompanyRankData("PrimeHouse Group", "Антон Климов", 2500f));
        companies.Add(new CompanyRankData("SolidBase Engineering", "Стивен Картер", 2400f));
        companies.Add(new CompanyRankData("SkyTower Projects", "Андрей Морозов", 2300f));
        companies.Add(new CompanyRankData("ProCity Development", "Илья Васильев", 2250f));
        companies.Add(new CompanyRankData("StoneEdge Builders", "Николай Кузнецов", 2200f));
        companies.Add(new CompanyRankData("UrbanForge Construction", "Артем Костин", 2150f));
        companies.Add(new CompanyRankData("BuildCore Systems", "Петр Новиков", 2100f));
        companies.Add(new CompanyRankData("MegaConstruct Solutions", "Майкл Дженкинс", 2050f));
        companies.Add(new CompanyRankData("SteelBeam Group", "Сергей Ефремов", 2000f));
        companies.Add(new CompanyRankData("GrandLine Engineering", "Александр Павлов", 1950f));
        companies.Add(new CompanyRankData("NewAge Constructors", "Георгий Романов", 1900f));
        companies.Add(new CompanyRankData("SkyBridge Projects", "Ричард Смит", 1850f));
        companies.Add(new CompanyRankData("BuildRight Corporation", "Виктор Лебедев", 1800f));
        companies.Add(new CompanyRankData("UrbanLift Company", "Алексей Серов", 1750f));
        companies.Add(new CompanyRankData("PrimeStone Holdings", "Геннадий Карпов", 1700f));
        companies.Add(new CompanyRankData("GoldenHammer Builders", "Евгений Мартынов", 1650f));
        companies.Add(new CompanyRankData("MetroBuild Systems", "Игорь Рябов", 1600f));
        companies.Add(new CompanyRankData("NextStep Engineering", "Даниил Жуков", 1550f));
        companies.Add(new CompanyRankData("ApexHouse Group", "Дэвид Миллер", 1500f));
        companies.Add(new CompanyRankData("StoneRiver Construction", "Константин Орлов", 1450f));
        companies.Add(new CompanyRankData("MegaPlan Development", "Максим Громов", 1400f));
        companies.Add(new CompanyRankData("NorthPeak Builders", "Олег Тимофеев", 1350f));
        companies.Add(new CompanyRankData("UrbanRise Construction", "Владимир Козлов", 1300f));
        companies.Add(new CompanyRankData("BuildPro Alliance", "Григорий Савельев", 1250f));
        companies.Add(new CompanyRankData("Skyline Systems", "Артём Зайцев", 1200f));
        companies.Add(new CompanyRankData("DeltaHouse Group", "Роберт Грей", 1150f));
        companies.Add(new CompanyRankData("EcoStone Constructors", "Егор Чернов", 1100f));
        companies.Add(new CompanyRankData("UrbanMotion Engineering", "Илья Фёдоров", 1050f));
        companies.Add(new CompanyRankData("BuildCraft Limited", "Фёдор Корнеев", 1000f));
        companies.Add(new CompanyRankData("NextGen Development", "Джонатан Браун", 950f));
        companies.Add(new CompanyRankData("StrongBase Contractors", "Павел Дроздов", 900f));
        companies.Add(new CompanyRankData("UrbanSky Group", "Роберт Холмс", 870f));
        companies.Add(new CompanyRankData("PrimeEdge Builders", "Денис Яковлев", 850f));
        companies.Add(new CompanyRankData("CoreHouse Engineering", "Никита Волков", 830f));
        companies.Add(new CompanyRankData("NewLine Construction", "Василий Алексеев", 800f));
        companies.Add(new CompanyRankData("StoneField Projects", "Сэмюэль Харрис", 780f));
        companies.Add(new CompanyRankData("UrbanPrime Holdings", "Михаил Степанов", 760f));
        companies.Add(new CompanyRankData("BuildPro Systems", "Андрей Романов", 740f));
        companies.Add(new CompanyRankData("SolidForm Group", "Роберт Кэмпбелл", 720f));
        companies.Add(new CompanyRankData("SkyPoint Development", "Дмитрий Нестеров", 700f));
        companies.Add(new CompanyRankData("UrbanCraft Engineering", "Александр Ефимов", 680f));
        companies.Add(new CompanyRankData("PrimeSteel Constructors", "Иван Лебедев", 660f));
        companies.Add(new CompanyRankData("EcoBuild Systems", "Эндрю Моррис", 640f));
        companies.Add(new CompanyRankData("StoneArt Construction", "Антон Беляев", 620f));
        companies.Add(new CompanyRankData("MetroEdge Builders", "Даниил Власов", 600f));
        companies.Add(new CompanyRankData("UrbanVision Group", "Александр Мороз", 580f));
        companies.Add(new CompanyRankData("SkyRise Corporation", "Джеймс Уилсон", 560f));
        companies.Add(new CompanyRankData("TitanEdge Engineering", "Павел Савин", 540f));
        companies.Add(new CompanyRankData("NextBuild Alliance", "Владислав Егоров", 520f));
        companies.Add(new CompanyRankData("IronPeak Construction", "Григорий Сидоров", 500f));
        companies.Add(new CompanyRankData("PrimeUrban Group", "Максим Зотов", 480f));
        companies.Add(new CompanyRankData("BuildMatrix Systems", "Алексей Крылов", 460f));
        companies.Add(new CompanyRankData("EcoFrame Builders", "Сергей Панов", 440f));
        companies.Add(new CompanyRankData("UrbanFlow Construction", "Николай Громов", 420f));
        companies.Add(new CompanyRankData("CoreVision Development", "Роберт Митчелл", 400f));
        companies.Add(new CompanyRankData("BuildStar Engineering", "Денис Ильин", 380f));
        companies.Add(new CompanyRankData("SkyAxis Contractors", "Павел Иванов", 360f));
        companies.Add(new CompanyRankData("UrbanNova Group", "Геннадий Смирнов", 340f));
        companies.Add(new CompanyRankData("PrimeForm Builders", "Олег Кузьмин", 320f));
        companies.Add(new CompanyRankData("StoneLink Construction", "Ричард Дженкинс", 300f));
        companies.Add(new CompanyRankData("EcoSpace Development", "Илья Васильев", 280f));
        companies.Add(new CompanyRankData("UrbanPoint Systems", "Дмитрий Романов", 260f));
        companies.Add(new CompanyRankData("SkyBrick Corporation", "Александр Григорьев", 240f));
        companies.Add(new CompanyRankData("PrimeStone Engineering", "Виктор Тарасов", 220f));
        companies.Add(new CompanyRankData("UrbanForge Alliance", "Григорий Иванов", 200f));
        companies.Add(new CompanyRankData("DeltaCore Builders", "Стив Харрис", 190f));
        companies.Add(new CompanyRankData("BuildWave Systems", "Андрей Михайлов", 180f));
        companies.Add(new CompanyRankData("EcoUrban Constructors", "Сергей Платонов", 170f));
        companies.Add(new CompanyRankData("NextTower Development", "Олег Фомин", 160f));
        companies.Add(new CompanyRankData("StoneCraft Engineering", "Павел Никифоров", 150f));
        companies.Add(new CompanyRankData("UrbanDesign Projects", "Николай Романов", 140f));
        companies.Add(new CompanyRankData("SkyBase Construction", "Роман Титов", 130f));
        companies.Add(new CompanyRankData("PrimeVision Builders", "Эдвард Стивенсон", 120f));
        companies.Add(new CompanyRankData("EcoRise Development", "Иван Савельев", 110f));
        companies.Add(new CompanyRankData("BuildMotion Systems", "Артём Морозов", 100f));
        companies.Add(new CompanyRankData("UrbanEdge Group", "Роберт Холл", 90f));
        companies.Add(new CompanyRankData("SteelHouse Constructors", "Алексей Гусев", 80f));
        companies.Add(new CompanyRankData("NextPeak Engineering", "Григорий Матвеев", 70f));
        companies.Add(new CompanyRankData("BuildPoint Corporation", "Сергей Волков", 65f));
        companies.Add(new CompanyRankData("UrbanCore Systems", "Майкл Вуд", 60f));
        companies.Add(new CompanyRankData("EcoBuild Alliance", "Павел Тихонов", 55f));
        companies.Add(new CompanyRankData("PrimeEdge Construction", "Роберт Стэнли", 52f));
        companies.Add(new CompanyRankData("TitanStone Builders", "Джонатан Ким", 50f));
        companies.Add(new CompanyRankData("UrbanFuture Group", "Евгений Макаров", 49f));
        companies.Add(new CompanyRankData("NextBase Development", "Роман Егоров", 48f));
        companies.Add(new CompanyRankData("BuildForce Engineering", "Олег Лавров", 47f));
        companies.Add(new CompanyRankData("SkyCity Builders", "Владимир Никитин", 46f));
        companies.Add(new CompanyRankData("StoneRise Company", "Пётр Еремин", 45f));
        companies.Add(new CompanyRankData("EcoFrame Group", "Алексей Беликов", 44f));
        companies.Add(new CompanyRankData("UrbanLine Construction", "Генри Браун", 43f));
        companies.Add(new CompanyRankData("PrimeCraft Engineering", "Илья Данилов", 42f));
        companies.Add(new CompanyRankData("SkyFrame Development", "Максим Лисицын", 41f));
        companies.Add(new CompanyRankData("BuildNova Corporation", "Владимир Фёдоров", 40f));
        companies.Add(new CompanyRankData("Unaware of Victory", "Виталий Ковалев", 35f));


        SortCompanies();


        // === Остальные компании ===
        for (int i = companies.Count; i < totalCompanies; i++)
        {
            string name = prefixes[Random.Range(0, prefixes.Length)] + " " +
                          suffixes[Random.Range(0, suffixes.Length)];
            string ceo = ceoNames[Random.Range(0, ceoNames.Length)];
            float worth = Random.Range(minWealth, maxWealth);

            companies.Add(new CompanyRankData(name, ceo, worth));
        }

        SortCompanies();
    }

    public void SortCompanies()
    {
        companies.Sort((a, b) => b.netWorth.CompareTo(a.netWorth));
        for (int i = 0; i < companies.Count; i++)
        {
            companies[i].rank = i + 1;
        }
    }

    public void UpdateCompanyValues()
    {
        foreach (var c in companies)
        {
            float changePercent = Random.Range(-3f, 3f);
            c.dailyChange = changePercent;
            c.netWorth += c.netWorth * (changePercent / 100f);

            if (c.netWorth < 1f)
                c.netWorth = 1f;
        }

        SortCompanies();
    }

    public void UpdatePlayerPosition(float money, int vehicles, int workers, int homeLevel, int playerLevel, bool debtCleared)
    {
        if (!debtCleared || money < 1_000_000f)
        {
            playerCompany = null;
            return;
        }

        float playerWorth = money
            + (vehicles * 10_000f)
            + (workers * 5_000f)
            + (homeLevel * 100_000f)
            + (playerLevel * 50_000f);

        if (playerCompany == null)
        {
            playerCompany = new CompanyRankData(GameManager.Instance.Data.companyName, "Игрок", playerWorth);
            companies.Add(playerCompany);
        }
        else
        {
            playerCompany.netWorth = playerWorth;
        }

        SortCompanies();
        playerCompany.rank = companies.IndexOf(playerCompany) + 1;

        if (playerCompany.rank > 100)
        {
            companies.Remove(playerCompany);
            playerCompany.rank = -1;
        }
    }
}
