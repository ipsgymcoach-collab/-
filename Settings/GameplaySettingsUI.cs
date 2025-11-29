using UnityEngine;

public class GameplaySettingsUI : MonoBehaviour
{
    [Header("Селекторы (стрелочные)")]
    [SerializeField] private OptionSelectorUI languageSelector;
    [SerializeField] private OptionSelectorUI notificationsSelector;
    [SerializeField] private OptionSelectorUI timeFormatSelector;
    [SerializeField] private OptionSelectorUI dayFormatSelector;

    private string defaultLanguage = "English";
    private string defaultNotifications = "On";
    private string defaultTimeFormat = "24h";
    private string defaultDayFormat = "DD/MM/YYYY";

    private void Start()
    {
        SetupOptions();
        LoadSettings();
    }

    private void SetupOptions()
    {
        languageSelector.SetOptions(new[] { "English", "Русский", "Deutsch", "Français" });
        notificationsSelector.SetOptions(new[] { "On", "Off" });
        timeFormatSelector.SetOptions(new[] { "12h", "24h" });
        dayFormatSelector.SetOptions(new[] { "DD/MM/YYYY", "MM/DD/YYYY" });
    }

    public void ApplySettings()
    {
        PlayerPrefs.SetString("Language", languageSelector.GetCurrentValue());
        PlayerPrefs.SetString("Notifications", notificationsSelector.GetCurrentValue());
        PlayerPrefs.SetString("TimeFormat", timeFormatSelector.GetCurrentValue());
        PlayerPrefs.SetString("DayFormat", dayFormatSelector.GetCurrentValue());
        PlayerPrefs.Save();

        Debug.Log($"✅ Настройки сохранены: " +
                  $"Lang={languageSelector.GetCurrentValue()}, " +
                  $"Notif={notificationsSelector.GetCurrentValue()}, " +
                  $"Time={timeFormatSelector.GetCurrentValue()}, " +
                  $"Date={dayFormatSelector.GetCurrentValue()}");
    }

    public void ResetToDefaults()
    {
        languageSelector.SetValue(defaultLanguage);
        notificationsSelector.SetValue(defaultNotifications);
        timeFormatSelector.SetValue(defaultTimeFormat);
        dayFormatSelector.SetValue(defaultDayFormat);
        ApplySettings();
    }

    public void LoadSettings()
    {
        languageSelector.SetValue(PlayerPrefs.GetString("Language", defaultLanguage));
        notificationsSelector.SetValue(PlayerPrefs.GetString("Notifications", defaultNotifications));
        timeFormatSelector.SetValue(PlayerPrefs.GetString("TimeFormat", defaultTimeFormat));
        dayFormatSelector.SetValue(PlayerPrefs.GetString("DayFormat", defaultDayFormat));
    }
}
