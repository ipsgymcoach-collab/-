using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ResourceCartUI : MonoBehaviour
{
    [Header("UI Корзины")]
    [SerializeField] private Transform cartContainer;     // Контейнер строк корзины
    [SerializeField] private GameObject cartItemPrefab;   // Префаб одной строки
    [SerializeField] private Button buyButton;            // Кнопка "Купить"
    [SerializeField] private TMP_Text totalText;          // 💰 Итоговая сумма
    [SerializeField] private TMP_Text budgetText;         // 💵 Бюджет игрока

    [Header("Дополнительные кнопки")]
    [SerializeField] private Button clearButton;          // 🗑️ Очистить корзину
    [SerializeField] private Toggle selectAllToggle;      // ✅ Выбрать/снять всё

    private List<CartItem> cart = new();

    private void Start()
    {
        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuy);

        if (clearButton != null)
            clearButton.onClick.AddListener(ClearCart);

        if (selectAllToggle != null)
            selectAllToggle.onValueChanged.AddListener(OnSelectAllToggled);

        UpdateTotal();
    }

    // ➕ Добавление товара в корзину
    public void AddToCart(ResourceItem item, int count)
    {
        var existing = cart.FirstOrDefault(c => c.item.id == item.id);
        if (existing != null)
            existing.count = Mathf.Clamp(existing.count + count, 0, 100);
        else
            cart.Add(new CartItem(item, count));

        RefreshCart();
    }

    // 🔁 Обновление списка
    private void RefreshCart()
    {
        foreach (Transform t in cartContainer)
            Destroy(t.gameObject);

        foreach (var c in cart)
        {
            GameObject row = Instantiate(cartItemPrefab, cartContainer);
            CartItemUI ui = row.GetComponent<CartItemUI>();

            if (ui == null)
            {
                Debug.LogError("❌ На CartItemPrefab нет CartItemUI!");
                continue;
            }

            ui.nameText.text = c.item.name;
            ui.countText.text = $"{c.count} шт.";
            ui.priceText.text = $"{c.item.price * c.count} $";
            ui.checkToggle.isOn = c.toBuy;

            // Обновляем выбор
            ui.checkToggle.onValueChanged.RemoveAllListeners();
            ui.checkToggle.onValueChanged.AddListener(v =>
            {
                c.toBuy = v;
                UpdateTotal();
            });

            // Удаление строки
            ui.deleteButton.onClick.RemoveAllListeners();
            ui.deleteButton.onClick.AddListener(() =>
            {
                cart.Remove(c);
                RefreshCart();
            });

            // === 🔧 РЕДАКТИРОВАНИЕ КОЛИЧЕСТВА ===

            if (ui.amountInput != null)
                ui.amountInput.text = c.count.ToString();

            // --- Уменьшить количество ---
            if (ui.minusButton != null)
            {
                ui.minusButton.onClick.RemoveAllListeners();
                ui.minusButton.onClick.AddListener(() =>
                {
                    c.count = Mathf.Max(1, c.count - 1);
                    RefreshCart();
                });
            }

            // --- Увеличить количество ---
            if (ui.plusButton != null)
            {
                ui.plusButton.onClick.RemoveAllListeners();
                ui.plusButton.onClick.AddListener(() =>
                {
                    c.count++;
                    RefreshCart();
                });
            }

            // --- Ввод количества вручную ---
            if (ui.amountInput != null)
            {
                ui.amountInput.onEndEdit.RemoveAllListeners();
                ui.amountInput.onEndEdit.AddListener((txt) =>
                {
                    if (int.TryParse(txt, out int val))
                        c.count = Mathf.Max(1, val);

                    RefreshCart();
                });
            }

            // --- Удаление выбранного количества ---
            if (ui.deleteSelectedButton != null)
            {
                ui.deleteSelectedButton.onClick.RemoveAllListeners();
                ui.deleteSelectedButton.onClick.AddListener(() =>
                {
                    int remove = 0;

                    if (ui.amountInput != null)
                        int.TryParse(ui.amountInput.text, out remove);

                    remove = Mathf.Max(1, remove);

                    c.count -= remove;

                    if (c.count <= 0)
                        cart.Remove(c);

                    RefreshCart();
                });
            }

        }

        UpdateTotal();
        UpdateSelectAllToggle();

        // Прокрутка вниз
        ScrollRect scroll = cartContainer.GetComponentInParent<ScrollRect>();
        if (scroll != null)
            StartCoroutine(ScrollToBottomSmooth(scroll));
    }

    // 💰 Подсчёт итоговой суммы
    private void UpdateTotal()
    {
        int total = cart.Where(c => c.toBuy).Sum(c => c.item.price * c.count);
        int budget = GameManager.Instance.Data.money;

        if (totalText != null)
            totalText.text = $"Итого: {total} $";

        if (budgetText != null)
            budgetText.text = $"Бюджет: {budget} $";

        if (buyButton != null)
        {
            buyButton.interactable = total > 0 && budget >= total;

            var colors = buyButton.colors;
            colors.normalColor = (budget < total) ? new Color(0.6f, 0.6f, 0.6f, 1f) : Color.white;
            buyButton.colors = colors;
        }
    }

    // 🔄 Прокрутка к последнему элементу
    private IEnumerator ScrollToBottomSmooth(ScrollRect scroll)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();

        float t = 0f;
        float start = scroll.verticalNormalizedPosition;
        float target = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * 10f;
            scroll.verticalNormalizedPosition = Mathf.Lerp(start, target, t);
            yield return null;
        }

        scroll.verticalNormalizedPosition = target;
    }

    // 💳 Покупка
    private void OnBuy()
    {
        int total = cart.Where(c => c.toBuy).Sum(c => c.item.price * c.count);
        int budget = GameManager.Instance.Data.money;

        if (budget < total)
        {
            Debug.LogWarning("Недостаточно денег!");
            return;
        }

        GameManager.Instance.Data.money -= total;

        foreach (var c in cart.Where(c => c.toBuy))
            GameManager.Instance.Data.AddToWarehouse(c.item.id, c.count);

        cart.RemoveAll(c => c.toBuy);
        RefreshCart();

        // === 🔥 ДОБАВЛЕНО ===
        var shopUI = FindFirstObjectByType<ResourceShopUI>();
        shopUI?.UpdateStorageInfo();

        var garryUI = FindFirstObjectByType<ResourcesUIControllerGarry>();
        garryUI?.UpdateStorageInfo();

        SaveManager.SaveGame(GameManager.Instance.Data, GameManager.Instance.CurrentSlot);

        HUDController.Instance?.UpdateHUD(GameManager.Instance.Data);
    }


    // 🧹 Очистка корзины
    private void ClearCart()
    {
        if (cart.Count == 0)
        {
            Debug.Log("Корзина уже пуста.");
            return;
        }

        cart.Clear();
        RefreshCart();
        Debug.Log("🗑️ Корзина полностью очищена.");
    }

    // ✅ Выбрать/снять всё
    private void OnSelectAllToggled(bool isOn)
    {
        foreach (var c in cart)
            c.toBuy = isOn;

        RefreshCart();
    }

    // 🔄 Синхронизация состояния галочки “Выбрать всё”
    private void UpdateSelectAllToggle()
    {
        if (selectAllToggle == null) return;
        if (cart.Count == 0)
        {
            selectAllToggle.isOn = false;
            selectAllToggle.interactable = false;
            return;
        }

        selectAllToggle.interactable = true;
        selectAllToggle.isOn = cart.All(c => c.toBuy);
    }

    // 🧱 Класс корзины
    private class CartItem
    {
        public ResourceItem item;
        public int count;
        public bool toBuy;

        public CartItem(ResourceItem i, int c)
        {
            item = i;
            count = c;
            toBuy = true;
        }
    }
}
