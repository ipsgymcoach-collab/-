using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionSelectorUI : MonoBehaviour
{
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;

    [SerializeField] private string[] options;
    private int currentIndex = 0;

    public System.Action<string> OnValueChanged;

    private void Awake()
    {
        if (leftButton != null)
            leftButton.onClick.AddListener(PrevOption);

        if (rightButton != null)
            rightButton.onClick.AddListener(NextOption);

        UpdateText();
    }

    private void OnDestroy()
    {
        if (leftButton != null)
            leftButton.onClick.RemoveListener(PrevOption);

        if (rightButton != null)
            rightButton.onClick.RemoveListener(NextOption);
    }

    private void PrevOption()
    {
        if (options == null || options.Length == 0) return;
        currentIndex = (currentIndex - 1 + options.Length) % options.Length;
        UpdateText();
    }

    private void NextOption()
    {
        if (options == null || options.Length == 0) return;
        currentIndex = (currentIndex + 1) % options.Length;
        UpdateText();
    }

    private void UpdateText()
    {
        if (options == null || options.Length == 0 || valueText == null) return;
        valueText.text = options[Mathf.Clamp(currentIndex, 0, options.Length - 1)];
        OnValueChanged?.Invoke(options[currentIndex]);
    }

    public void SetOptions(string[] newOptions, int startIndex = 0)
    {
        if (newOptions == null || newOptions.Length == 0) return;
        options = newOptions;
        currentIndex = Mathf.Clamp(startIndex, 0, options.Length - 1);
        UpdateText();
    }

    public string GetCurrentValue()
    {
        if (options == null || options.Length == 0) return "";
        return options[currentIndex];
    }

    public void SetValue(string value)
    {
        if (options == null || options.Length == 0) return;
        for (int i = 0; i < options.Length; i++)
        {
            if (options[i] == value)
            {
                currentIndex = i;
                UpdateText();
                break;
            }
        }
    }

    // ✅ Добавлен метод для SettingsArrowUI
    public int GetCurrentIndexSafe()
    {
        if (options == null || options.Length == 0)
            return 0;

        return Mathf.Clamp(currentIndex, 0, options.Length - 1);
    }
}
