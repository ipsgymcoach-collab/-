using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Привязка UI-элементов ячейки к данным конкретной машины.
/// </summary>
public class VehicleSlotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text conditionText;
    [SerializeField] private TMP_Text maintenanceText;
    [SerializeField] private Button repairButton;
    [SerializeField] private Button sellButton;

    private VehicleData data;
    private GaragePanelController owner;
    public VehicleData Data => data;


    public void Bind(VehicleData vehicle, GaragePanelController controller)
    {
        data = vehicle;
        owner = controller;

        // === Иконка ===
        if (icon != null)
        {
            // Загружаем из нового пути: Assets/CONSTRUCTION/Resources/Icon/
            var sprite = Resources.Load<Sprite>($"Icon/{vehicle.iconId}");

            if (sprite == null)
            {
                Debug.LogWarning($"❌ [VehicleSlotUI] Не найден спрайт: Assets/CONSTRUCTION/Resources/Icon/{vehicle.iconId}.png");
                icon.enabled = false;
            }
            else
            {
                icon.sprite = sprite;
                icon.enabled = true;
                icon.color = Color.white;
                Debug.Log($"✅ [VehicleSlotUI] Загружен спрайт: {vehicle.iconId}");
            }
        }

        // === Текстовые поля ===
        if (nameText)
            nameText.text = vehicle.name;

        if (conditionText)
            conditionText.text = $"Состояние: {Mathf.RoundToInt(vehicle.condition)}%";

        if (maintenanceText)
            maintenanceText.text = $"Обслуживание: ${vehicle.maintenanceCost}/мес";

        // === КНОПКА РЕМОНТА ===
        if (repairButton)
        {
            repairButton.onClick.RemoveAllListeners();
            repairButton.onClick.AddListener(OnRepairClicked);

            var t = repairButton.GetComponentInChildren<TMP_Text>();

            // === 1. Машина в ремонте ===
            if (data.isUnderRepair)
            {
                repairButton.interactable = false;

                if (t)
                    t.text = $"Ремонт: {data.repairDaysLeft} дн.";
            }
            // === 2. Машина повреждена и может быть отремонтирована ===
            else if (data.condition < 100f)
            {
                int cost = CalculateRepairCost(data);
                int playerMoney = GameManager.Instance.CurrentGame.money;

                repairButton.interactable = playerMoney >= cost;

                if (t)
                {
                    if (playerMoney >= cost)
                        t.text = $"РЕМОНТ (${cost})";
                    else
                        t.text = $"РЕМОНТ (${cost})\n❌ Недостаточно денег";
                }
            }
            // === 3. Машина полностью исправна ===
            else
            {
                repairButton.interactable = false;

                if (t)
                    t.text = "Готово";
            }
        }



        // === КНОПКА ПРОДАЖИ ===
        if (sellButton)
        {
            sellButton.onClick.RemoveAllListeners();

            // если техника в ремонте — продажа запрещена
            if (data.isUnderRepair)
            {
                sellButton.interactable = false;
            }
            else
            {
                sellButton.interactable = true;

                // открываем окно подтверждения продажи
                sellButton.onClick.AddListener(() =>
                {
                    if (ConfirmSellUI.Instance == null)
                    {
                        Debug.LogError("❌ ConfirmSellUI.Instance == null — добавь ConfirmSellPanel на сцену и привяжи скрипт.");
                        return;
                    }

                    int salePrice = owner.CalculateSalePrice(data);
                    ConfirmSellUI.Instance.Show(data, salePrice, owner);
                });
            }
        }
    }

    private void OnRepairClicked()
    {
        if (data == null) return;

        var game = GameManager.Instance.CurrentGame;
        if (game == null) return;

        // === 1. Рассчитываем стоимость ремонта ===
        int cost = CalculateRepairCost(data);
        if (game.money < cost)
        {
            Debug.Log("Недостаточно денег для ремонта!");
            return;
        }

        // === 2. Базовое время ремонта ===
        // каждые 10% = 5 дней
        float missing = 100f - data.condition;
        int baseDays = Mathf.CeilToInt(missing / 10f) * 5;

        // === 3. Уменьшение дней ремонта от улучшений гаража ===
        int reduction = game.GetRepairDaysReduction();
        int finalDays = Mathf.Max(1, baseDays - reduction);

        // === 4. Применяем ремонт ===
        data.isUnderRepair = true;
        data.repairDaysLeft = finalDays;
        data.inGarage = false;   // машина уехала на ремонт

        // === 5. Списываем деньги ===
        game.SpendMoney(cost);

        // === 6. Сохраняем игру ===
        SaveManager.SaveGame(game, GameManager.Instance.CurrentSlot);

        // === 7. Обновляем список в гараже ===
        owner?.Rebuild();

        // === 8. Обновляем кнопку ремонта ===
        Bind(data, owner);
    }


    private void OnSellClicked()
    {
        owner?.TrySell(data);
    }

    // ================================================================
    // === РАСЧЁТ СТОИМОСТИ РЕМОНТА (10% = 7% от цены машины)
    // ================================================================
    private int CalculateRepairCost(VehicleData v)
    {
        float damage = 100f - v.condition;     // уровень повреждений
        float damageUnits = damage / 10f;      // каждые 10% = 1 единица
        float cost = v.price * 0.07f * damageUnits;
        return Mathf.RoundToInt(cost);
    }

}
