using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ResourceShopUI : MonoBehaviour
{
    [Header("Основные элементы UI")]
    [SerializeField] private TMP_InputField searchField;
    [SerializeField] private Transform itemContainer;
    [SerializeField] private GameObject itemRowPrefab;
    [SerializeField] private ResourceCartUI cartPanel;

    [SerializeField] private TMP_Text storageInfoText;

    [Header("Кнопки категорий (ручное подключение)")]
    [SerializeField] private Button buttonFundament;
    [SerializeField] private Button buttonMain;
    [SerializeField] private Button buttonFinishing;
    [SerializeField] private Button buttonRoof;

    private List<ResourceCategory> categories;
    private ResourceCategory activeCategory;
    private List<GameObject> currentItemRows = new();
    private CanvasGroup fadeGroup;
    private bool isAnimating = false;

    private void Start()
    {
        LoadResourcesFromJSON();

        if (buttonFundament) buttonFundament.onClick.AddListener(() => OpenCategory("Фундамент и перекрытия"));
        if (buttonMain) buttonMain.onClick.AddListener(() => OpenCategory("Основные материалы"));
        if (buttonFinishing) buttonFinishing.onClick.AddListener(() => OpenCategory("Вспомогательные и отделочные"));
        if (buttonRoof) buttonRoof.onClick.AddListener(() => OpenCategory("Кровельные материалы"));

        if (searchField != null)
            searchField.onValueChanged.AddListener(OnSearchChanged);

        UpdateStorageInfo();
    }

    // ═══════════════════════════════════════════════════════════
    // 🔥 ОБНОВЛЕНИЕ ТЕКСТА "Used / Max" в магазине
    // ═══════════════════════════════════════════════════════════
    public void UpdateStorageInfo()
    {
        var data = GameManager.Instance?.CurrentGame;
        if (data == null || storageInfoText == null) return;

        int used = data.GetWarehouseCurrentUsed();
        int max = data.GetWarehouseCapacity();

        storageInfoText.text = $"{used} / {max}";
    }


    private void LoadResourcesFromJSON()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Data/ResourcesData");
        if (jsonFile == null)
        {
            Debug.LogError("❌ Не найден файл ResourcesData.json в папке Resources/Data/");
            return;
        }
        categories = JsonUtility.FromJson<ResourceDatabase>("{\"categories\":" + jsonFile.text + "}").categories;
    }

    // 🔹 Открывает категорию
    private void OpenCategory(string categoryName)
    {
        if (isAnimating) return;
        if (categories == null)
        {
            Debug.LogError("❌ Категории не загружены!");
            return;
        }

        var cat = categories.FirstOrDefault(c => c.category == categoryName);
        if (cat == null)
        {
            Debug.LogWarning($"⚠ Категория '{categoryName}' не найдена в JSON!");
            return;
        }

        if (activeCategory == cat)
        {
            StartCoroutine(FadeOut());
            activeCategory = null;
            return;
        }

        if (fadeGroup != null)
            StartCoroutine(SwitchCategory(cat));
        else
            StartCoroutine(FadeInNew(cat));
    }

    private IEnumerator SwitchCategory(ResourceCategory newCat)
    {
        isAnimating = true;
        yield return FadeOut();
        yield return FadeInNew(newCat);
        isAnimating = false;
    }

    private IEnumerator FadeInNew(ResourceCategory newCat)
    {
        ClearList();

        if (fadeGroup == null)
        {
            fadeGroup = itemContainer.GetComponent<CanvasGroup>();
            if (fadeGroup == null)
                fadeGroup = itemContainer.gameObject.AddComponent<CanvasGroup>();
        }

        fadeGroup.alpha = 0;
        activeCategory = newCat;
        RefreshItemList(newCat.items);

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 5f;
            fadeGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }
        fadeGroup.alpha = 1f;
    }

    private IEnumerator FadeOut()
    {
        if (fadeGroup == null) yield break;
        isAnimating = true;

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 5f;
            fadeGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        fadeGroup.alpha = 0f;
        ClearList();
        isAnimating = false;
    }

    private void ClearList()
    {
        foreach (var obj in currentItemRows)
            if (obj != null)
                Destroy(obj);
        currentItemRows.Clear();
    }

    // 🧩 Генерация списка товаров
    private void RefreshItemList(List<ResourceItem> items)
    {
        foreach (var obj in currentItemRows)
            Destroy(obj);
        currentItemRows.Clear();

        foreach (var item in items)
        {
            GameObject row = Instantiate(itemRowPrefab, itemContainer);
            currentItemRows.Add(row);

            ShopItemRowUI ui = row.GetComponent<ShopItemRowUI>();
            if (ui == null)
            {
                Debug.LogError("❌ На префабе ItemRowPrefab нет ShopItemRowUI!");
                continue;
            }

            ui.itemId = item.id;

            int count = 0;
            ui.nameText.text = item.name;
            ui.countInput.text = "0";
            ui.priceText.text = $"{item.price} $/шт.";

            // ─────────── КНОПКА "-" ───────────
            ui.minusButton.onClick.AddListener(() =>
            {
                int step = 1;

                if (Keyboard.current != null)
                {
                    if (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed)
                        step = 10;
                    else if (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed)
                        step = 100;
                }

                count = Mathf.Max(0, count - step);
                ui.countInput.text = count.ToString();
            });

            // ─────────── КНОПКА "+" ───────────
            ui.plusButton.onClick.AddListener(() =>
            {
                int step = 1;

                if (Keyboard.current != null)
                {
                    if (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed)
                        step = 10;
                    else if (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed)
                        step = 100;
                }

                count = Mathf.Min(9999, count + step);
                ui.countInput.text = count.ToString();
            });

            ui.countInput.onEndEdit.AddListener(val =>
            {
                if (int.TryParse(val, out int n))
                    count = Mathf.Clamp(n, 0, 9999);
                ui.countInput.text = count.ToString();
            });

            // ─────────── ДОБАВИТЬ В КОРЗИНУ ───────────
            ui.addButton.onClick.AddListener(() =>
            {
                int finalCount = Mathf.Clamp(count, 0, 9999);

                if (finalCount <= 0)
                {
                    Debug.Log($"⚠ Количество для {item.name} не выбрано!");
                    return;
                }

                // 🔹 Проверяем доступное место на складе
                int space = GameManager.Instance.Data.GetAvailableSpace(item.id);

                // если склад полностью переполнен — блокируем кнопку
                if (space <= 0)
                {
                    Debug.Log($"🚫 Склад переполнен: {item.name}");
                    ui.addButton.interactable = false;

                    TMP_Text btnText = ui.addButton.GetComponentInChildren<TMP_Text>();
                    if (btnText != null)
                        btnText.text = "Нет места";

                    return;
                }

                // если можно добавить меньше, чем игрок запросил
                if (finalCount > space)
                {
                    Debug.Log($"⚠ {item.name}: можно добавить только {space}, т.к. склад почти заполнен");
                    finalCount = space;

                    // обновим надпись кнопки
                    TMP_Text btnText = ui.addButton.GetComponentInChildren<TMP_Text>();
                    if (btnText != null)
                        btnText.text = "Нет места";
                    ui.addButton.interactable = false;
                }

                // ✅ Добавляем в корзину допустимое количество
                cartPanel.AddToCart(item, finalCount);
                Debug.Log($"🛒 Добавлено в корзину: {item.name} x{finalCount} (доступно на складе ещё {space - finalCount})");

                UpdateStorageInfo();

                // 🔄 Сбрасываем количество после добавления
                count = 0;
                ui.countInput.text = "0";

                // 🛠 Возвращаем кнопку в нормальное состояние
                TMP_Text btnTextRestore = ui.addButton.GetComponentInChildren<TMP_Text>();
                if (btnTextRestore != null)
                    btnTextRestore.text = "В корзину";

                ui.addButton.interactable = true;

            });

        }
    }

    // 🔍 Реакция на поиск
    private void OnSearchChanged(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            if (activeCategory != null)
                RefreshItemList(activeCategory.items);
            return;
        }

        query = query.ToLower();

        var foundCat = categories.FirstOrDefault(c => c.items.Any(i => i.name.ToLower().Contains(query)));
        if (foundCat == null)
            return;

        var foundItem = foundCat.items.FirstOrDefault(i => i.name.ToLower().Contains(query));
        if (foundItem == null)
            return;

        ClearList();
        activeCategory = foundCat;
        StartCoroutine(ShowSingleItem(foundItem));
    }

    // 🧱 Отображение одного найденного товара
    private IEnumerator ShowSingleItem(ResourceItem item)
    {
        if (fadeGroup == null)
        {
            fadeGroup = itemContainer.GetComponent<CanvasGroup>();
            if (fadeGroup == null)
                fadeGroup = itemContainer.gameObject.AddComponent<CanvasGroup>();
        }

        fadeGroup.alpha = 0;
        yield return new WaitForEndOfFrame();

        GameObject row = Instantiate(itemRowPrefab, itemContainer);
        currentItemRows.Add(row);

        ShopItemRowUI ui = row.GetComponent<ShopItemRowUI>();
        ui.itemId = item.id;
        ui.nameText.text = item.name;
        ui.priceText.text = $"{item.price} $/шт.";
        ui.countInput.text = "0";

        int count = 0;

        ui.minusButton.onClick.AddListener(() =>
        {
            int step = 1;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed)
                    step = 10;
                else if (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed)
                    step = 100;
            }

            count = Mathf.Max(0, count - step);
            ui.countInput.text = count.ToString();
        });

        ui.plusButton.onClick.AddListener(() =>
        {
            int step = 1;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed)
                    step = 10;
                else if (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed)
                    step = 100;
            }

            count = Mathf.Min(9999, count + step);
            ui.countInput.text = count.ToString();
        });

        ui.addButton.onClick.AddListener(() =>
        {
            int finalCount = Mathf.Clamp(count, 0, 9999);
            if (finalCount > 0)
            {
                int space = GameManager.Instance.Data.GetAvailableSpace(item.id);

                if (space <= 0)
                {
                    Debug.Log($"🚫 Склад переполнен для {item.name}");
                    return;
                }

                if (finalCount > space)
                {
                    Debug.Log($"⚠ {item.name}: можно добавить только {space}, т.к. склад почти заполнен");
                    finalCount = space;
                }

                cartPanel.AddToCart(item, finalCount);
                Debug.Log($"🛒 Добавлено в корзину: {item.name} x{finalCount}");

                UpdateStorageInfo();

                // 🔄 Сброс количества
                count = 0;
                ui.countInput.text = "0";

                // 🛠 Вернуть кнопку в нормальный вид
                TMP_Text btnTextRestore = ui.addButton.GetComponentInChildren<TMP_Text>();
                if (btnTextRestore != null)
                    btnTextRestore.text = "В корзину";

                ui.addButton.interactable = true;

            }
            else
            {
                Debug.Log($"⚠ Количество для {item.name} не выбрано!");
            }
        });

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 6f;
            fadeGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }
        fadeGroup.alpha = 1f;
    }

    public void OnShopClosed()
    {
        UpdateStorageInfo();
    }
}
