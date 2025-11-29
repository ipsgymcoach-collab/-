using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class ForemanHireUI : MonoBehaviour
{
    [Header("Основная панель")]
    [SerializeField] private GameObject hirePanel;
    [SerializeField] private Transform listContainer;
    [SerializeField] private GameObject foremanCardPrefab;
    [SerializeField] private Button closeButton;

    private ForemanSlotUI currentSlot;

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (hirePanel != null)
            hirePanel.SetActive(false);
    }

    public void Open(ForemanSlotUI slot)
    {
        currentSlot = slot;

        if (hirePanel != null)
            hirePanel.SetActive(true);

        GameManager.Instance.IsUIOpen = true;

        foreach (Transform child in listContainer)
            Destroy(child.gameObject);

        PopulateList();
    }

    public void Close()
    {
        if (hirePanel != null)
            hirePanel.SetActive(false);

        GameManager.Instance.IsUIOpen = false;
    }

    private void PopulateList()
    {
        // 1) ВСЕГДА генерим полный каталог
        var fullCatalog = GenerateAllForemen();

        // 2) Накладываем сохранённые статусы (если есть в GameData)
        var saved = GameManager.Instance.Data.foremen;
        if (saved != null && saved.Count > 0)
        {
            foreach (var s in saved)
            {
                var match = fullCatalog.Find(f => f.id == s.id);
                if (match != null)
                {
                    match.isHired = s.isHired;
                    match.isFired = s.isFired;
                    match.rehireAvailableDay = s.rehireAvailableDay;
                    match.brigades = s.brigades;          // переносим созданные бригады
                    match.extraBrigades = s.extraBrigades; // на всякий случай, если менялось
                }
            }
        }

        // 3) Сохраняем обратно в GameData (единая истина)
        GameManager.Instance.Data.foremen = fullCatalog;

        // 4) Фильтруем по требуемому уровню слота
        int requiredLevel = currentSlot.RequiredLevel;
        var availableForemen = fullCatalog.Where(f => f.requiredLevel == requiredLevel).ToList();

        foreach (var data in availableForemen)
        {
            GameObject card = Instantiate(foremanCardPrefab, listContainer);
            ForemanHireCardUI ui = card.GetComponent<ForemanHireCardUI>();

            if (data.isFired && TimeController.Instance != null && TimeController.Instance.day < data.rehireAvailableDay)
            {
                int daysLeft = data.rehireAvailableDay - TimeController.Instance.day;
                ui.Setup(data, null);
                ui.DisableHireButton($"Недоступен ({daysLeft} дн.)");
            }
            else if (data.isHired)
            {
                ui.Setup(data, null);
                ui.DisableHireButton("Уже нанят");
            }
            else
            {
                ui.Setup(data, OnHireConfirmed);
            }
        }
    }

    private void OnHireConfirmed(ForemanData data)
    {
        var playerData = GameManager.Instance.Data;

        if (playerData.money < data.hireCost)
            return;

        playerData.money -= data.hireCost;

        data.isHired = true;
        data.isFired = false;
        data.rehireAvailableDay = 0;

        // Обновим HUD (деньги)
        HUDController.Instance?.UpdateMoney(playerData.money);

        // Назначим бригадира в слот
        currentSlot.AssignForeman(data);

        // Создаём бригады, если их нет (минимум 1 + extraBrigades)
        if (data.brigades == null)
            data.brigades = new List<BrigadeData>();

        if (data.brigades.Count == 0)
        {
            int count = Mathf.Max(1, data.extraBrigades + 1);
            if (GameManager.Instance.Data.allBrigades == null)
                GameManager.Instance.Data.allBrigades = new List<BrigadeData>();

            for (int i = 1; i <= count; i++)
            {
                // ✅ Теперь создаём через GameData.AddNewBrigade(), чтобы настроение = 100
                var newBr = GameManager.Instance.Data.AddNewBrigade(
                    data.id,
                    $"Бригада {data.name} №{i}"
                );

                data.brigades.Add(newBr);
                GameManager.Instance.Data.allBrigades.Add(newBr);
            }
        }

        // 💾 Сохраняем игру после найма
        SaveManager.SaveGame(GameManager.Instance.Data, 0);

        Debug.Log($"✅ Нанят бригадир {data.name}, создано {data.brigades.Count} бригад со 100 настроением");

        Close();
    }


    // ====== КАТАЛОГ БРИГАДИРОВ ======
    private List<ForemanData> GenerateAllForemen()
    {
        List<ForemanData> f = new List<ForemanData>();

        // === Уровень 1 === (5 шт: 1 спец + 4 обычных)
        f.Add(new ForemanData()
        {
            id = "f1_special",
            name = "Семён Дорохов",
            buff = "🏗 +1 бригада",
            debuff = "↓ Качество итоговой работы -25%",
            hireCost = 2000,
            salary = 300,
            iconId = "foreman_icon_special1",
            requiredLevel = 1,
            isSpecialLeader = true,
            extraBrigades = 1,
        });

        f.Add(new ForemanData() { id = "f1_1", name = "Иван Петров", buff = "↑ Скорость +5%", debuff = "↓ Настроение -2%", hireCost = 1200, salary = 200, iconId = "foreman_icon_1", requiredLevel = 1 });
        f.Add(new ForemanData() { id = "f1_2", name = "Алексей Смирнов", buff = "↑ Качество +7%", debuff = "↓ Скорость -2%", hireCost = 1400, salary = 250, iconId = "foreman_icon_2", requiredLevel = 1 });
        f.Add(new ForemanData() { id = "f1_3", name = "Павел Егоров", buff = "↑ Производительность +6%", debuff = "↓ Мотивация -3%", hireCost = 1600, salary = 260, iconId = "foreman_icon_3", requiredLevel = 1 });
        f.Add(new ForemanData() { id = "f1_4", name = "Дмитрий Орлов", buff = "↑ Организация +5%", debuff = "↓ Скорость -2%", hireCost = 1500, salary = 240, iconId = "foreman_icon_32", requiredLevel = 1 });

        // === Уровень 4 === (5 шт)
        f.Add(new ForemanData()
        {
            id = "f4_special",
            name = "Аркадий Полозов",
            buff = "🏗 +1 бригада",
            debuff = "↓ Качество итоговой работы -25%",
            hireCost = 2600,
            salary = 380,
            iconId = "foreman_icon_special2",
            requiredLevel = 4,
            isSpecialLeader = true,
            extraBrigades = 1,
        });
        f.Add(new ForemanData() { id = "f4_1", name = "Владимир Соколов", buff = "↑ Качество отделки +10%", debuff = "↓ Скорость -3%", hireCost = 2000, salary = 300, iconId = "foreman_icon_4", requiredLevel = 4 });
        f.Add(new ForemanData() { id = "f4_2", name = "Михаил Кузнецов", buff = "↑ Экономия материалов +8%", debuff = "↓ Скорость -2%", hireCost = 2200, salary = 320, iconId = "foreman_icon_5", requiredLevel = 4 });
        f.Add(new ForemanData() { id = "f4_3", name = "Григорий Романов", buff = "↑ Производительность +9%", debuff = "↓ Качество -3%", hireCost = 2400, salary = 350, iconId = "foreman_icon_6", requiredLevel = 4 });
        f.Add(new ForemanData() { id = "f4_4", name = "Юрий Иванов", buff = "↑ Контроль затрат +7%", debuff = "↓ Скорость -2%", hireCost = 2600, salary = 370, iconId = "foreman_icon_7", requiredLevel = 4 });

        // === Уровень 6 === (7 шт: 1 спец + 6 обычных)
        f.Add(new ForemanData()
        {
            id = "f6_special",
            name = "Глеб Синицын",
            buff = "🏗 +1 бригада",
            debuff = "↓ Качество итоговой работы -25%",
            hireCost = 4000,
            salary = 500,
            iconId = "foreman_icon_special3",
            requiredLevel = 6,
            isSpecialLeader = true,
            extraBrigades = 1,
        });
        f.Add(new ForemanData() { id = "f6_1", name = "Виктор Платонов", buff = "↑ Эффективность +10%", debuff = "↓ Настроение -4%", hireCost = 3200, salary = 420, iconId = "foreman_icon_8", requiredLevel = 6 });
        f.Add(new ForemanData() { id = "f6_2", name = "Денис Козлов", buff = "↑ Качество +9%", debuff = "↓ Скорость -3%", hireCost = 3400, salary = 430, iconId = "foreman_icon_9", requiredLevel = 6 });
        f.Add(new ForemanData() { id = "f6_3", name = "Николай Захаров", buff = "↑ Производительность +8%", debuff = "↓ Качество -4%", hireCost = 3600, salary = 440, iconId = "foreman_icon_10", requiredLevel = 6 });
        f.Add(new ForemanData() { id = "f6_4", name = "Артём Громов", buff = "↑ Экономия материалов +12%", debuff = "↓ Скорость -2%", hireCost = 3800, salary = 460, iconId = "foreman_icon_11", requiredLevel = 6 });
        f.Add(new ForemanData() { id = "f6_5", name = "Евгений Мороз", buff = "↑ Производительность +11%", debuff = "↓ Настроение -5%", hireCost = 4000, salary = 480, iconId = "foreman_icon_12", requiredLevel = 6 });
        f.Add(new ForemanData() { id = "f6_6", name = "Александр Рубцов", buff = "↑ Качество отделки +10%", debuff = "↓ Точность сметы -4%", hireCost = 4200, salary = 500, iconId = "foreman_icon_13", requiredLevel = 6 });

        // === Уровень 8 === (9 шт: 1 спец + 8 обычных)
        f.Add(new ForemanData()
        {
            id = "f8_special",
            name = "Руслан Греков",
            buff = "🏗 +2 бригады",
            debuff = "↓ Качество итоговой работы -25%",
            hireCost = 5400,
            salary = 650,
            iconId = "foreman_icon_special4",
            requiredLevel = 8,
            isSpecialLeader = true,
            extraBrigades = 2,
        });
        f.Add(new ForemanData() { id = "f8_1", name = "Игорь Киселёв", buff = "↑ Скорость строительства +12%", debuff = "↓ Качество -4%", hireCost = 4200, salary = 520, iconId = "foreman_icon_14", requiredLevel = 8 });
        f.Add(new ForemanData() { id = "f8_2", name = "Роман Павлов", buff = "↑ Экономия материалов +10%", debuff = "↓ Скорость -3%", hireCost = 4400, salary = 530, iconId = "foreman_icon_15", requiredLevel = 8 });
        f.Add(new ForemanData() { id = "f8_3", name = "Станислав Крылов", buff = "↑ Качество отделки +15%", debuff = "↓ Производительность -5%", hireCost = 4600, salary = 540, iconId = "foreman_icon_16", requiredLevel = 8 });
        f.Add(new ForemanData() { id = "f8_4", name = "Егор Беляев", buff = "↑ Производительность +12%", debuff = "↓ Мотивация -4%", hireCost = 4800, salary = 560, iconId = "foreman_icon_17", requiredLevel = 8 });
        f.Add(new ForemanData() { id = "f8_5", name = "Вадим Артемьев", buff = "↑ Точность сметы +10%", debuff = "↓ Скорость -3%", hireCost = 5000, salary = 580, iconId = "foreman_icon_18", requiredLevel = 8 });
        f.Add(new ForemanData() { id = "f8_6", name = "Михаил Денисов", buff = "↑ Скорость монтажа +15%", debuff = "↓ Качество отделки -5%", hireCost = 5200, salary = 600, iconId = "foreman_icon_19", requiredLevel = 8 });
        f.Add(new ForemanData() { id = "f8_7", name = "Даниил Волков", buff = "↑ Качество фундамента +20%", debuff = "↓ Скорость -6%", hireCost = 5400, salary = 620, iconId = "foreman_icon_20", requiredLevel = 8 });
        f.Add(new ForemanData() { id = "f8_8", name = "Антон Гаврилов", buff = "↑ Производительность +15%", debuff = "↓ Настроение рабочих -5%", hireCost = 5600, salary = 640, iconId = "foreman_icon_21", requiredLevel = 8 });

        // === Уровень 10 === (11 шт: 1 спец + 10 обычных)
        f.Add(new ForemanData()
        {
            id = "f10_special",
            name = "Тимофей Рогачёв",
            buff = "🏗 +3 бригады",
            debuff = "↓ Качество итоговой работы -25%",
            hireCost = 7800,
            salary = 900,
            iconId = "foreman_icon_special5",
            requiredLevel = 10,
            isSpecialLeader = true,
            extraBrigades = 3,
        });
        f.Add(new ForemanData() { id = "f10_1", name = "Виктор Соловьёв", buff = "↑ Качество стройки +20%", debuff = "↓ Скорость -5%", hireCost = 6000, salary = 700, iconId = "foreman_icon_22", requiredLevel = 10 });
        f.Add(new ForemanData() { id = "f10_2", name = "Максим Корнеев", buff = "↑ Эффективность +18%", debuff = "↓ Мотивация -6%", hireCost = 6200, salary = 720, iconId = "foreman_icon_23", requiredLevel = 10 });
        f.Add(new ForemanData() { id = "f10_3", name = "Александр Пронин", buff = "↑ Производительность +22%", debuff = "↓ Качество -4%", hireCost = 6400, salary = 740, iconId = "foreman_icon_24", requiredLevel = 10 });
        f.Add(new ForemanData() { id = "f10_4", name = "Никита Лавров", buff = "↑ Скорость отделки +18%", debuff = "↓ Точность сметы -6%", hireCost = 6600, salary = 760, iconId = "foreman_icon_25", requiredLevel = 10 });
        f.Add(new ForemanData() { id = "f10_5", name = "Владислав Гурьев", buff = "↑ Экономия материалов +15%", debuff = "↓ Скорость -5%", hireCost = 6800, salary = 780, iconId = "foreman_icon_26", requiredLevel = 10 });
        f.Add(new ForemanData() { id = "f10_6", name = "Дмитрий Чернов", buff = "↑ Качество фундамента +25%", debuff = "↓ Производительность -7%", hireCost = 7000, salary = 800, iconId = "foreman_icon_27", requiredLevel = 10 });
        f.Add(new ForemanData() { id = "f10_7", name = "Георгий Иванов", buff = "↑ Контроль затрат +18%", debuff = "↓ Скорость -4%", hireCost = 7200, salary = 820, iconId = "foreman_icon_28", requiredLevel = 10 });
        f.Add(new ForemanData() { id = "f10_8", name = "Андрей Никитин", buff = "↑ Репутация компании +10%", debuff = "↓ Мотивация -6%", hireCost = 7400, salary = 850, iconId = "foreman_icon_29", requiredLevel = 10 });
        f.Add(new ForemanData() { id = "f10_9", name = "Сергей Матвеев", buff = "↑ Скорость всех процессов +15%", debuff = "↓ Качество -5%", hireCost = 7600, salary = 870, iconId = "foreman_icon_30", requiredLevel = 10 });
        f.Add(new ForemanData() { id = "f10_10", name = "Олег Данилов", buff = "↑ Мотивация рабочих +15%", debuff = "↓ Экономия материалов -8%", hireCost = 7800, salary = 900, iconId = "foreman_icon_31", requiredLevel = 10 });

        return f;
    }
}
