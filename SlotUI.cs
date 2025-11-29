using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI companyNameText;
    [SerializeField] private TextMeshProUGUI saveTimeText; // 🔹 новое поле для отображения даты/времени сохранения
    [SerializeField] private Image logoImage;
    [SerializeField] private Image heroImage;
    [SerializeField] private Button deleteButton;   // кнопка "Удалить"

    public Button DeleteButton => deleteButton;

    public void SetupSlot(GameData data, Sprite[] logoSprites, Sprite[] heroSprites)
    {
        if (data == null)
        {
            if (companyNameText != null)
                companyNameText.text = "(пусто)";

            if (saveTimeText != null)
                saveTimeText.text = "";

            if (logoImage != null) logoImage.enabled = false;
            if (heroImage != null) heroImage.enabled = false;

            if (deleteButton != null) deleteButton.gameObject.SetActive(false);
            return;
        }

        // --- Название компании ---
        if (companyNameText != null)
            companyNameText.text = $"Компания: «{data.companyName}»";

        // --- Дата/время сохранения (внутриигровые) ---
        if (saveTimeText != null)
        {
            string formattedTime = $"{data.hour:D2}:{data.minute:D2}";
            string formattedDate = data.isDateFormatDDMM
                ? $"{data.day:D2}/{data.month:D2}/{data.year:D4}"
                : $"{data.month:D2}/{data.day:D2}/{data.year:D4}";

            saveTimeText.text = $"Сохранено: {formattedTime} {formattedDate}";
        }

        // --- Лого ---
        if (logoImage != null)
        {
            if (data.selectedLogoId >= 0 && data.selectedLogoId < logoSprites.Length)
            {
                logoImage.sprite = logoSprites[data.selectedLogoId];
                logoImage.enabled = true;
            }
            else
            {
                logoImage.enabled = false;
            }
        }

        // --- Герой ---
        if (heroImage != null)
        {
            if (data.selectedHeroId >= 0 && data.selectedHeroId < heroSprites.Length)
            {
                heroImage.sprite = heroSprites[data.selectedHeroId];
                heroImage.enabled = true;
            }
            else
            {
                heroImage.enabled = false;
            }
        }

        // --- Кнопка удаления ---
        if (deleteButton != null) deleteButton.gameObject.SetActive(true);
    }
}
