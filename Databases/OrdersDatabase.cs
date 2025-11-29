using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum OrderCategory
{
    Suburb,   // Пригород
    City,     // Город
    Center,   // Центр
    Special   // Особые заказы
}

[CreateAssetMenu(fileName = "OrdersDatabase", menuName = "Databases/OrdersDatabase", order = 1)]
public class OrdersDatabase : ScriptableObject
{
    [Header("📍 Заказы для Пригорода")]
    public List<OrderInfo> suburbOrders = new List<OrderInfo>();

    [Header("🏙 Заказы для Города")]
    public List<OrderInfo> cityOrders = new List<OrderInfo>();

    [Header("🌆 Заказы для Центра")]
    public List<OrderInfo> centerOrders = new List<OrderInfo>();

    [Header("⭐ Особые заказы")]
    public List<OrderInfo> specialOrders = new List<OrderInfo>();

#if UNITY_EDITOR
    [ContextMenu("🔄 Sync from Code (обновить список)")]
    public void SyncFromCode()
    {
        var data = GetOrdersFromCode();

        suburbOrders = data.suburb;
        cityOrders = data.city;
        centerOrders = data.center;
        specialOrders = data.special;

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        Debug.Log($"[OrdersDatabase] ✅ Синхронизация завершена.\n" +
                  $"Пригород: {suburbOrders.Count} | Город: {cityOrders.Count} | Центр: {centerOrders.Count} | Особые: {specialOrders.Count}");
    }
#endif

    // === Твоя база заказов (здесь ты добавляешь новые) ===
    private (List<OrderInfo> suburb, List<OrderInfo> city, List<OrderInfo> center, List<OrderInfo> special) GetOrdersFromCode()
    {
        // --- Пригород ---
        var suburb = new List<OrderInfo>
{
    new OrderInfo
    {
        id = "sub_01",
        address = "ул. Берёзовая, 15",
        description = "Одноэтажный дом для семьи из трёх человек. Простое строительство, фундамент и кровля. ыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыыы",
        payment = 12000,
        duration = 20,
        difficulty = 1
    },
    new OrderInfo
    {
        id = "sub_02",
        address = "ул. Озёрная, 24",
        description = "Дом с подведением воды и электричества. Минимальная отделка внутри.",
        payment = 15000,
        duration = 25,
        difficulty = 2
    },
    new OrderInfo
    {
        id = "sub_03",
        address = "ул. Полевая, 6",
        description = "Двухкомнатный дом с мансардой. Требуется установка окон и крыши.",
        payment = 18000,
        duration = 28,
        difficulty = 2
    },
    new OrderInfo
    {
        id = "sub_04",
        address = "ул. Солнечная, 8",
        description = "Дом 8×10 с деревянной верандой и навесом. Основной упор на отделку фасада.",
        payment = 20000,
        duration = 30,
        difficulty = 2
    },
    new OrderInfo
    {
        id = "sub_05",
        address = "ул. Сиреневая, 3",
        description = "Кирпичный коттедж в пригороде. Установка фундамента и внутренних стен.",
        payment = 22000,
        duration = 35,
        difficulty = 3
    },
    new OrderInfo
    {
        id = "sub_06",
        address = "ул. Вишнёвая, 11",
        description = "Дом с чердаком и подвалом. Монтаж лестницы и крыши.",
        payment = 25000,
        duration = 37,
        difficulty = 3
    },
    new OrderInfo
    {
        id = "sub_07",
        address = "ул. Лесная, 5",
        description = "Строительство дома 6×9, частичная внутренняя отделка.",
        payment = 17000,
        duration = 27,
        difficulty = 2
    },
    new OrderInfo
    {
        id = "sub_08",
        address = "ул. Липовая, 2",
        description = "Дом с верандой и деревянным навесом. Необходимо утепление стен.",
        payment = 19000,
        duration = 32,
        difficulty = 2
    },
    new OrderInfo
    {
        id = "sub_09",
        address = "ул. Садовая, 17",
        description = "Постройка одноэтажного дома с гаражом. Прокладка коммуникаций.",
        payment = 26000,
        duration = 38,
        difficulty = 3
    },
    new OrderInfo
    {
        id = "sub_10",
        address = "ул. Речная, 12",
        description = "Дом у реки, повышенные требования к гидроизоляции и фундаменту.",
        payment = 28000,
        duration = 40,
        difficulty = 3
    },
    new OrderInfo
    {
        id = "sub_11",
        address = "ул. Цветочная, 4",
        description = "Дом из бруса, ручная кладка и покраска стен.",
        payment = 21000,
        duration = 33,
        difficulty = 2
    },
    new OrderInfo
    {
        id = "sub_12",
        address = "ул. Ромашковая, 9",
        description = "Дом с двумя спальнями и большой кухней. Установка окон и пола.",
        payment = 23000,
        duration = 34,
        difficulty = 2
    },
    new OrderInfo
    {
        id = "sub_13",
        address = "ул. Камышовая, 13",
        description = "Дом на холме. Укрепление фундамента и облицовка.",
        payment = 27000,
        duration = 39,
        difficulty = 3
    },
    new OrderInfo
    {
        id = "sub_14",
        address = "ул. Горная, 22",
        description = "Двухэтажный коттедж с балконом. Работы по лестницам и фасаду.",
        payment = 32000,
        duration = 45,
        difficulty = 3
    },
    new OrderInfo
    {
        id = "sub_15",
        address = "ул. Песочная, 1",
        description = "Одноэтажный дом с плоской крышей. Простая постройка под сдачу.",
        payment = 14000,
        duration = 22,
        difficulty = 1
    },
    new OrderInfo
    {
        id = "sub_16",
        address = "ул. Зелёная, 16",
        description = "Дом с гаражом на две машины. Теплоизоляция и кровля.",
        payment = 29000,
        duration = 42,
        difficulty = 3
    },
    new OrderInfo
    {
        id = "sub_17",
        address = "ул. Береговая, 7",
        description = "Дом на участке с уклоном. Особое внимание фундаменту.",
        payment = 31000,
        duration = 48,
        difficulty = 2
    },
    new OrderInfo
    {
        id = "sub_18",
        address = "ул. Кленовая, 19",
        description = "Маленький дачный дом. Простая конструкция без внутренней отделки.",
        payment = 11000,
        duration = 18,
        difficulty = 1
    },
    new OrderInfo
    {
        id = "sub_19",
        address = "ул. Тихая, 21",
        description = "Дом 7×8, утепление стен и установка дверей.",
        payment = 20000,
        duration = 29,
        difficulty = 2
    },
    new OrderInfo
    {
        id = "sub_20",
        address = "ул. Речная, 18",
        description = "Постройка дома из газобетона. Установка перекрытий и крыши.",
        payment = 26000,
        duration = 36,
        difficulty = 3
    },
    new OrderInfo
    {
        id = "sub_21",
        address = "ул. Звёздная, 9",
        description = "Дом из клеёного бруса. Требуется покраска и герметизация швов.",
        payment = 27000,
        duration = 38,
        difficulty = 3
    },
    new OrderInfo
    {
        id = "sub_22",
        address = "ул. Сосновая, 14",
        description = "Дом 9×9, внутренняя отделка и установка лестницы на чердак.",
        payment = 30000,
        duration = 41,
        difficulty = 3
    },
    new OrderInfo
    {
        id = "sub_23",
        address = "ул. Озёрная, 25",
        description = "Дом у озера с террасой. Требуется гидроизоляция и навес.",
        payment = 32000,
        duration = 43,
        difficulty = 3
    },
    new OrderInfo
    {
        id = "sub_24",
        address = "ул. Берёзовая, 18",
        description = "Постройка домика под сдачу. Минимальные требования, быстрая сборка.",
        payment = 13000,
        duration = 21,
        difficulty = 1
    },
    new OrderInfo
    {
        id = "sub_25",
        address = "ул. Кедровая, 4",
        description = "Двухэтажный дом с гаражом и балконом. Отделка под ключ.",
        payment = 35000,
        duration = 50,
        difficulty = 3
    },
    new OrderInfo
    {
        id = "sub_26",
        address = "ул. Тенистая, 20",
        description = "Дом с мансардой, декоративная отделка фасада.",
        payment = 24000,
        duration = 33,
        difficulty = 2
    },
    new OrderInfo
    {
        id = "sub_27",
        address = "ул. Родниковая, 30",
        description = "Строительство небольшого домика для отдыха. Установка коммуникаций.",
        payment = 15000,
        duration = 24,
        difficulty = 1
    },
    new OrderInfo
    {
        id = "sub_28",
        address = "ул. Солнечная, 28",
        description = "Дом 10×10 с верандой и навесом для автомобиля.",
        payment = 33000,
        duration = 47,
        difficulty = 2
    },
    new OrderInfo
    {
        id = "sub_29",
        address = "ул. Луговая, 8",
        description = "Дом из кирпича с односкатной крышей. Требуется утепление стен.",
        payment = 21000,
        duration = 30,
        difficulty = 2
    },
    new OrderInfo
    {
        id = "sub_30",
        address = "ул. Весенняя, 6",
        description = "Маленький дом 6×6, без отделки. Минимальные затраты времени.",
        payment = 10000,
        duration = 17,
        difficulty = 1
    },
    new OrderInfo
    {
        id = "sub_31",
        address = "ул. Фруктовая, 2",
        description = "Дом с навесом для машины и небольшим подвалом.",
        payment = 27000,
        duration = 40,
        difficulty = 3
    },
    new OrderInfo
    {
        id = "sub_32",
        address = "ул. Речная, 33",
        description = "Кирпичный дом 8×8. Требуется кладка стен и монтаж крыши.",
        payment = 23000,
        duration = 35,
        difficulty = 2
    },
    new OrderInfo
    {
        id = "sub_33",
        address = "ул. Лугова, 19",
        description = "Дом с деревянной отделкой фасада и навесом для террасы.",
        payment = 22000,
        duration = 32,
        difficulty = 2
    },
    new OrderInfo
    {
        id = "sub_34",
        address = "ул. Яблоневая, 7",
        description = "Одноэтажный дом для пожилой пары. Простая планировка и быстрые сроки.",
        payment = 12000,
        duration = 19,
        difficulty = 1
    },
    new OrderInfo
    {
        id = "sub_35",
        address = "ул. Сосновая, 41",
        description = "Дом из кирпича с утеплением и внешней отделкой.",
        payment = 31000,
        duration = 45,
        difficulty = 3
    },
    new OrderInfo
    {
        id = "sub_36",
        address = "ул. Озёрная, 29",
        description = "Строительство дома из газобетона с внутренней отделкой.",
        payment = 28000,
        duration = 38,
        difficulty = 3
    },
    new OrderInfo
    {
        id = "sub_37",
        address = "ул. Заречная, 15",
        description = "Дом с мансардой и деревянной лестницей. Обустройство чердака.",
        payment = 26000,
        duration = 36,
        difficulty = 3
    },
    new OrderInfo
    {
        id = "sub_38",
        address = "ул. Берёзовая, 33",
        description = "Дом под сдачу, простая сборка, установка кровли и дверей.",
        payment = 15000,
        duration = 23,
        difficulty = 1
    },
    new OrderInfo
    {
        id = "sub_39",
        address = "ул. Тихая, 8",
        description = "Дом с пристройкой под котельную. Необходима кладка фундамента.",
        payment = 24000,
        duration = 31,
        difficulty = 2
    },
    new OrderInfo
    {
        id = "sub_40",
        address = "ул. Полевая, 26",
        description = "Большой коттедж с двумя балконами. Полный цикл строительства.",
        payment = 40000,
        duration = 55,
        difficulty = 2
    },
};


        // --- Город ---
        var city = new List<OrderInfo>
        {
            new OrderInfo
            {
                id = "city_01",
                address = "ул. Центральная, 40",
                description = "Двухэтажный городской дом. Включает облицовку фасада и монтаж парковки.",
                payment = 42000,
                duration = 45,
                difficulty = 4
            }
        };

        // --- Центр ---
        var center = new List<OrderInfo>
        {
            new OrderInfo
            {
                id = "center_01",
                address = "пр. Свободы, 12",
                description = "Реконструкция офисного здания в центре города. Высокая сложность, требует координации бригад.",
                payment = 95000,
                duration = 60,
                difficulty = 5
            }
        };

        // --- Особые ---
        var special = new List<OrderInfo>
        {
            new OrderInfo
            {
                id = "spec_01",
                address = "ул. Банковская, 5",
                description = "Строительство отделения банка. Требуется бронирование помещений и монтаж систем безопасности.",
                payment = 150000,
                duration = 90,
                difficulty = 6
            }
        };

        return (suburb, city, center, special);
    }

    // === Методы доступа для UI ===
    public List<OrderInfo> GetSuburbOrders() => suburbOrders;
    public List<OrderInfo> GetCityOrders() => cityOrders;
    public List<OrderInfo> GetCenterOrders() => centerOrders;
    public List<OrderInfo> GetSpecialOrders() => specialOrders;

    public OrderInfo GetOrderById(string id)
    {
        foreach (var list in new[] { suburbOrders, cityOrders, centerOrders, specialOrders })
        {
            var found = list.Find(o => o.id == id);
            if (found != null)
                return found;
        }
        return null;
    }
}
