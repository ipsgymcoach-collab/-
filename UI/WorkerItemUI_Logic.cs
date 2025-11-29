using UnityEngine;
using TMPro;

public class WorkerItemUI_Logic : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text profText;
    [SerializeField] private TMP_Text categoryText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text salaryText;

    private WorkerData data;

    public void Setup(WorkerData w)
    {
        data = w;

        if (nameText != null)
            nameText.text = $"{w.firstName} {w.lastName}";

        if (profText != null)
            profText.text = CleanProfession(w.profession);

        if (categoryText != null)
            categoryText.text = w.category;

        // ⭐ тут была ошибка (w.level)
        if (levelText != null)
            levelText.text = $"{w.skillLevel}";

        if (salaryText != null)
            salaryText.text = $"{w.salary:n0}$ / мес";
    }

    private string CleanProfession(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return raw;

        var parts = raw.Split(' ');

        if (parts.Length >= 2 && int.TryParse(parts[1], out _))
            return parts[0];

        return raw;
    }

}
