using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class WorkerRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text workerName;
    [SerializeField] private TMP_Text workerJob;
    [SerializeField] private TMP_Text workerLevel;
    [SerializeField] private Image background;

    private Button button;
    private Action<WorkerRowUI> onSelect;

    private Color normalColor = new Color(1f, 1f, 1f, 0.15f);
    private Color selectedColor = new Color(0.3f, 0.6f, 1f, 0.35f);
    private Color grayColor = new Color(0.6f, 0.6f, 0.6f, 0.25f);

    public WorkerData Data { get; private set; }
    public bool IsSelected { get; private set; } = false;

    private void Awake()
    {
        button = GetComponent<Button>() ?? gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = background;
    }

    public void Setup(WorkerData data, Action<WorkerRowUI> onSelectAction)
    {
        Data = data;
        onSelect = onSelectAction;

        string fullName = $"{data.firstName} {data.lastName}".Trim();
        workerName.text = string.IsNullOrWhiteSpace(fullName) ? "Без имени" : fullName;

        workerJob.text = !string.IsNullOrWhiteSpace(data.professionName)
            ? data.professionName
            : CleanProfession(data.profession);

        workerLevel.text = $"Ур. {data.skillLevel}";

        SetSelected(false);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onSelect?.Invoke(this));
    }

    public void SetSelected(bool state)
    {
        IsSelected = state;
        background.color = state ? selectedColor : normalColor;
    }

    public void SetInteractable(bool state)
    {
        button.interactable = state;
    }

    public void SetGray(bool state)
    {
        background.color = state ? grayColor : normalColor;
    }

    private string CleanProfession(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return raw;

        var parts = raw.Split(' ');

        // "Секретарь 3" → "Секретарь"
        if (parts.Length >= 2 && int.TryParse(parts[1], out _))
            return parts[0];

        return raw;
    }

}
