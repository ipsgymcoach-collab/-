using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeLevelItem : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public TMP_Text priceText;
    public Button buyButton;
    public Image lockImage;

    [Header("Data")]
    public int levelIndex;   // 1..5
    public int price;
    public string title;
    public string description;

    private System.Action<int> onBuy;

    public void Setup(int levelIndex, string title, string desc, int price, System.Action<int> onBuy)
    {
        this.levelIndex = levelIndex;
        this.title = title;
        this.description = desc;
        this.price = price;
        this.onBuy = onBuy;

        titleText.text = title;
        descriptionText.text = desc;
        priceText.text = price + "$";

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => onBuy(levelIndex));
    }

    public void SetStatus(bool purchased, bool available)
    {
        // ----- 1. Покупка -----
        if (purchased)
        {
            lockImage.gameObject.SetActive(true);    // lock ТОЛЬКО для купленных уровней
            buyButton.interactable = false;

            // Покупленный уровень — слегка приглушим кнопку, но текст НЕ трогаем
            titleText.color = Color.white;
            descriptionText.color = Color.white;
            priceText.color = new Color(1f, 1f, 1f, 0.5f);
            return;
        }

        // ----- 2. Не куплено -----
        lockImage.gameObject.SetActive(false);

        // Доступен для покупки (следующий уровень)
        if (available)
        {
            buyButton.interactable = true;
            titleText.color = Color.white;
            descriptionText.color = Color.white;
            priceText.color = Color.white;
        }
        else
        {
            // Недоступен, но НЕ скрываем title/description
            buyButton.interactable = false;

            // слегка приглушаем ВСЁ (не исчезая)
            Color c = new Color(0.75f, 0.75f, 0.75f, 1f);
            titleText.color = c;
            descriptionText.color = c;
            priceText.color = c;
        }
    }

}
