using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ForemanHireCardUI : MonoBehaviour
{
    [Header("Элементы карточки")]
    [SerializeField] private Image portrait;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text buffText;
    [SerializeField] private TMP_Text debuffText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button hireButton;
    [SerializeField] private TMP_Text hireButtonText;

    private ForemanData data;
    private System.Action<ForemanData> onHire;

    public void Setup(ForemanData d, System.Action<ForemanData> onHireAction)
    {
        data = d;
        onHire = onHireAction;

        if (nameText) nameText.text = d.name;
        if (buffText) buffText.text = d.buff;
        if (debuffText) debuffText.text = d.debuff;
        if (costText) costText.text = $"Найм: {d.hireCost}$ + {d.salary}$/мес";

        if (portrait)
        {
            var sprite = Resources.Load<Sprite>($"Icon/{d.iconId}");
            if (sprite != null)
            {
                portrait.sprite = sprite;
                portrait.enabled = true;
                portrait.preserveAspect = true;
            }
            else portrait.enabled = false;
        }

        if (hireButton)
        {
            hireButton.onClick.RemoveAllListeners();
            hireButton.onClick.AddListener(() => onHire?.Invoke(data));
            if (hireButtonText) hireButtonText.text = "Нанять";
        }
    }

    public void DisableHireButton(string text)
    {
        if (hireButton != null)
        {
            hireButton.interactable = false;
            if (hireButtonText != null)
                hireButtonText.text = text;
        }
    }
}
