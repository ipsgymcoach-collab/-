using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class ShopItemCardUI : MonoBehaviour
{
    [Header("UI элементы")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyLabel;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private TMP_Text lockedText;
    [SerializeField] private Button infoButton; // 🟡 Кнопка "?"

    private VehicleData vehicle;
    private GameData data;
    private int playerLevel;
    private ShopUIController shop;

    // 🟢 Инициализация карточки
    public void Setup(VehicleData vehicleData, GameData gameData, int playerLvl)
    {
        vehicle = vehicleData;
        data = gameData;
        playerLevel = playerLvl;

        // Ссылка на контроллер магазина
        shop = GetComponentInParent<ShopUIController>();

        // Название и цена
        nameText.text = vehicle.name;
        priceText.text = $"${vehicle.price:N0}";

        // 🖼️ Загружаем иконку
        LoadIcon();

        // Обновляем доступность и статус кнопок
        UpdateState();

        // Кнопка "Купить"
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyClicked);

        // Кнопка "?" — открыть описание
        if (infoButton != null)
        {
            infoButton.onClick.RemoveAllListeners();
            infoButton.onClick.AddListener(OpenInfoPanel);
        }
    }

    // 🖼️ Загрузка иконки транспорта
    private void LoadIcon()
    {
        string iconName = !string.IsNullOrEmpty(vehicle.shopIconId)
            ? vehicle.shopIconId
            : vehicle.iconId;

        if (string.IsNullOrEmpty(iconName))
        {
            Debug.LogWarning($"[ShopItemCardUI] У {vehicle.name} не задан iconId!");
            iconImage.color = new Color(1, 1, 1, 0.3f);
            return;
        }

        // Сначала ищем в /ShopIcons/, затем fallback в /Icons/
        Sprite icon = Resources.Load<Sprite>($"ShopIcons/{iconName}");
        if (icon == null)
            icon = Resources.Load<Sprite>($"Icons/{iconName}");

        if (icon != null)
        {
            iconImage.sprite = icon;
            iconImage.color = Color.white;
        }
        else
        {
            Debug.LogWarning($"[ShopItemCardUI] Не найден спрайт: {iconName}");
            iconImage.color = new Color(1, 1, 1, 0.3f);
        }
    }

    // 🔄 Обновление состояния кнопок в зависимости от условий
    private void UpdateState()
    {
        bool isLocked = playerLevel < vehicle.unlockLevel;
        int ownedCount = data.ownedVehicles.Count(v => v.id == vehicle.id);
        bool isOutOfStock = ownedCount >= vehicle.maxOwnedAllowed;
        bool canAfford = data.money >= vehicle.price;

        // 🚫 Недоступен из-за уровня
        if (isLocked)
        {
            lockedOverlay.SetActive(true);
            lockedText.text = $"Нужна лицензия {vehicle.unlockLevel} уровня";
            buyButton.gameObject.SetActive(false);
            return;
        }

        // ✅ Разблокирован
        lockedOverlay.SetActive(false);
        buyButton.gameObject.SetActive(true);

        // 🚫 Лимит по количеству
        if (isOutOfStock)
        {
            buyButton.interactable = false;
            buyLabel.text = "Нет в наличии";
            return;
        }

        // 🚫 Не хватает денег
        if (!canAfford)
        {
            buyButton.interactable = false;
            buyLabel.text = "Недостаточно средств";
            return;
        }

        // ✅ Можно купить
        buyButton.interactable = true;
        buyLabel.text = "Купить";
    }

    // 💰 Обработка покупки
    private void OnBuyClicked()
    {
        if (data == null || vehicle == null)
        {
            Debug.LogWarning("[ShopItemCardUI] Data или Vehicle не заданы!");
            return;
        }

        // Проверяем уровень
        if (playerLevel < vehicle.unlockLevel)
        {
            shop?.ShowToast($"Необходим {vehicle.unlockLevel} уровень лицензии!");
            return;
        }

        // Проверяем деньги
        if (data.money < vehicle.price)
        {
            shop?.ShowToast("Недостаточно средств!");
            return;
        }

        // Проверяем количество
        int ownedCount = data.ownedVehicles.Count(v => v.id == vehicle.id);
        if (ownedCount >= vehicle.maxOwnedAllowed)
        {
            shop?.ShowToast("Нет в наличии!");
            return;
        }

        // ✅ Совершаем покупку
        data.money -= vehicle.price;
        data.AddVehicleById(vehicle.id);

        SaveManager.SaveGame(data, GameManager.Instance.CurrentSlot);

        shop?.ShowToast("ТС доставлено по вашему адресу");
        shop?.UpdatePlayerInfo();
        UpdateState();
    }

    // ℹ️ Открыть окно описания ТС
    private void OpenInfoPanel()
    {
        if (shop != null && vehicle != null)
            shop.OpenVehicleInfo(vehicle);
    }
}
