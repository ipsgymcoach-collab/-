using UnityEngine;
using TMPro;

public class GuardUpgradeTabController : MonoBehaviour
{
    [Header("Родитель для уровней")]
    public Transform levelsContainer;

    [Header("Префаб одного уровня")]
    public UpgradeLevelItem levelPrefab;

    [Header("Цены уровней")]
    public int[] prices = new int[5];

    [Header("Названия уровней")]
    [TextArea] public string[] titles = new string[5];

    [Header("Описания уровней")]
    [TextArea] public string[] descriptions = new string[5];

    public enum TabType { Territory, Garage, Warehouse }
    public TabType tabType;

    private bool initialized = false;

    private GameData Data => GameManager.Instance.CurrentGame;

    // === Инициализация ===
    private void Start()
    {
        initialized = true;
        BuildLevels();
    }

    // === При активации вкладки ===
    private void OnEnable()
    {
        if (initialized)
            BuildLevels();
    }

    // === Построение 5 уровней ===
    public void BuildLevels()
    {
        foreach (Transform child in levelsContainer)
            Destroy(child.gameObject);

        int purchasedLevels = GetPurchased();

        for (int i = 0; i < 5; i++)
        {
            int index = i + 1;
            var item = Instantiate(levelPrefab, levelsContainer);

            item.Setup(index, titles[i], descriptions[i], prices[i], OnBuy);

            bool purchased = index <= purchasedLevels;
            bool available = index == purchasedLevels + 1;

            item.SetStatus(purchased, available);
        }
    }

    private void OnBuy(int levelIndex)
    {
        int price = prices[levelIndex - 1];
        if (!Data.SpendMoney(price))
            return;

        SetPurchased(levelIndex);

        // Если улучшается вкладка GARAGE
        if (tabType == TabType.Garage)
        {
            // Применяем баф HP ко всем машинам
            Data.ApplyGarageHPBuffToAllVehicles();
        }

        SaveManager.SaveGame(Data, GameManager.Instance.CurrentSlot);

        BuildLevels();
    }


    // === Получение текущего уровня ===
    private int GetPurchased()
    {
        switch (tabType)
        {
            case TabType.Territory: return Data.anatoliyGuardTabLevel;
            case TabType.Garage: return Data.anatoliyGarageTabLevel;
            case TabType.Warehouse: return Data.anatoliyWarehouseTabLevel;
        }
        return 0;
    }

    // === Установка уровня ===
    private void SetPurchased(int val)
    {
        switch (tabType)
        {
            case TabType.Territory: Data.anatoliyGuardTabLevel = val; break;
            case TabType.Garage: Data.anatoliyGarageTabLevel = val; break;
            case TabType.Warehouse: Data.anatoliyWarehouseTabLevel = val; break;
        }
    }
}
