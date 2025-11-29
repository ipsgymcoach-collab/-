using UnityEngine;

[CreateAssetMenu(
    fileName = "OrderCategoryConfig",
    menuName = "Databases/Order Category Config",
    order = 10)]
public class OrderCategoryConfig : ScriptableObject
{
    [Header("Базовые настройки")]
    public OrderCategory category;          // Suburb / City / Center / Special
    public string displayName = "Категория";

    [Tooltip("Минимальный уровень игрока для открытия этой категории.")]
    public int requiredPlayerLevel = 1;

    [Tooltip("Сколько слотов заказов показывать одновременно в этой категории.")]
    public int orderSlots = 4;

    [Header("Оформление UI (по желанию)")]
    public Sprite categoryIcon;
    public Color headerColor = Color.white;
}
