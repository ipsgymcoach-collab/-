using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

/// <summary>
/// Простая витрина склада:
/// - Загружает категории/товары из ResourcesData.json
/// - Берёт фактические остатки из GameManager.Instance.Data.warehouseResources (поле quantity)
/// - Рисует заголовки категорий и строки с количеством
/// </summary>
public class WarehouseUI : MonoBehaviour
{
    [Header("Контейнер и префабы")]
    [SerializeField] private Transform categoryContainer;     // ScrollView/Viewport/Content
    [SerializeField] private GameObject categoryTitlePrefab;  // Префаб заголовка категории (TMP_Text)
    [SerializeField] private GameObject itemRowPrefab;        // Префаб строки (должны быть "NameText" и "CountText")

    [Header("Кнопка назад и панели")]
    [SerializeField] private Button backButton;               // Назначь BackButton
    [SerializeField] private GameObject warehousePanel;       // Сам склад (для скрытия)
    [SerializeField] private GameObject choicePanel;          // Панель Гарри (чтобы вернуть её при закрытии)

    private ResourceDatabase db; // кэш JSON, чтобы не грузить каждый раз

    private void OnEnable()
    {
        RefreshWarehouse();

        // Назад
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackPressed);
        }
        else
        {
            Debug.LogWarning("[WarehouseUI] ⚠ Не назначена кнопка BackButton!");
        }
    }

    /// <summary>Публичный вызов из Garry UI</summary>
    public void ForceRefresh()
    {
        RefreshWarehouse();
    }

    private void RefreshWarehouse()
    {
        if (categoryContainer == null)
        {
            Debug.LogError("[WarehouseUI] Не назначен categoryContainer!");
            return;
        }

        // Очистка
        for (int i = categoryContainer.childCount - 1; i >= 0; i--)
            Destroy(categoryContainer.GetChild(i).gameObject);

        // Загружаем JSON (один раз кэшируем)
        if (db == null)
        {
            TextAsset jsonFile = Resources.Load<TextAsset>("Data/ResourcesData");
            if (jsonFile == null)
            {
                Debug.LogError("[WarehouseUI] ❌ Не найден Resources/Data/ResourcesData.json");
                return;
            }

            // оборачиваем массив в объект с полем categories
            db = JsonUtility.FromJson<ResourceDatabase>("{\"categories\":" + jsonFile.text + "}");
            if (db == null || db.categories == null)
            {
                Debug.LogError("[WarehouseUI] ❌ Не удалось распарсить JSON (categories == null)");
                return;
            }
        }

        // Текущее состояние склада
        var data = GameManager.Instance?.Data;
        if (data == null)
        {
            Debug.LogError("[WarehouseUI] ❌ GameManager.Instance.Data == null");
            return;
        }

        var warehouse = data.warehouseResources; // ВАЖНО: именно warehouseResources
        if (warehouse == null)
        {
            Debug.LogError("[WarehouseUI] ❌ Data.warehouseResources == null");
            return;
        }

        Debug.Log($"[WarehouseUI] Загружаем склад. Категорий в JSON: {db.categories.Count}. Позиции на складе: {warehouse.Count}");

        // Проходим по категориям и рисуем
        foreach (var category in db.categories)
        {
            // Заголовок категории
            if (categoryTitlePrefab != null)
            {
                var titleGO = Instantiate(categoryTitlePrefab, categoryContainer);
                var titleText = titleGO.GetComponentInChildren<TMP_Text>();
                if (titleText != null) titleText.text = category.category;
            }
            else
            {
                Debug.LogWarning("[WarehouseUI] ⚠ Не назначен categoryTitlePrefab — заголовки категорий не будут отображаться.");
            }

            if (category.items == null || category.items.Count == 0)
            {
                Debug.LogWarning($"[WarehouseUI] ⚠ В категории '{category.category}' нет items.");
                continue;
            }

            // Товары внутри категории
            foreach (var item in category.items)
            {
                var stored = warehouse.FirstOrDefault(w => w.id == item.id);
                int qty = stored != null ? stored.quantity : 0;

                if (itemRowPrefab == null)
                {
                    Debug.LogError("[WarehouseUI] ❌ Не назначен itemRowPrefab!");
                    return;
                }

                GameObject row = Instantiate(itemRowPrefab, categoryContainer);

                var nameText = row.transform.Find("NameText")?.GetComponent<TMP_Text>();
                var countText = row.transform.Find("CountText")?.GetComponent<TMP_Text>();

                if (nameText == null || countText == null)
                {
                    Debug.LogError("[WarehouseUI] ❌ В префабе itemRowPrefab должны быть объекты 'NameText' и 'CountText' с TMP_Text!");
                    continue;
                }

                nameText.text = item.name;
                countText.text = qty.ToString();
            }
        }

        // Автопрокрутка к началу
        var scroll = categoryContainer.GetComponentInParent<ScrollRect>();
        if (scroll != null)
        {
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1f;
        }

        Debug.Log("[WarehouseUI] ✅ Обновление склада завершено.");
    }

    /// <summary>
    /// Кнопка «Назад» — закрывает склад и возвращает игрока в меню Гарри
    /// </summary>
    private void OnBackPressed()
    {
        Debug.Log("[WarehouseUI] 🔙 Нажата кнопка Назад");

        StartCoroutine(CloseWarehouseSmooth());
    }

    private IEnumerator CloseWarehouseSmooth()
    {
        CanvasGroup group = warehousePanel.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = warehousePanel.AddComponent<CanvasGroup>();
            group.alpha = 1f;
        }

        // 🔹 Плавное исчезновение
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(1f, 0f, t / 0.3f);
            yield return null;
        }
        group.alpha = 0f;

        warehousePanel.SetActive(false);
        if (choicePanel != null)
        {
            choicePanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[WarehouseUI] ⚠ choicePanel не назначен — меню Гарри не появится!");
        }

        Debug.Log("[WarehouseUI] 📦 Склад закрыт и управление возвращено.");
    }
}
