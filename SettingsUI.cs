using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SettingsUI : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider; // 🔊 для эффектов

    [Header("Graphics")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Language")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    [Header("Gameplay")]
    [SerializeField] private Toggle timeFormatToggle;   // ✅ 12/24ч
    [SerializeField] private TMP_Dropdown dateFormatDropdown; // ✅ ДД/ММ или ММ/ДД
    [SerializeField] private Toggle notificationsToggle; // ✅ Новый toggle "Всплывающие уведомления"

    [Header("Buttons")]
    [SerializeField] private Button resetButton;

    private Resolution[] resolutions;
    private List<string> options = new List<string>();

    private void Start()
    {
        // --- Инициализация разрешений ---
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        HashSet<string> usedResolutions = new HashSet<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = $"{resolutions[i].width} x {resolutions[i].height}";

            if (!usedResolutions.Contains(option))
            {
                options.Add(option);
                usedResolutions.Add(option);

                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = options.Count - 1;
                }
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        // --- Подписка ---
        masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);

        resolutionDropdown.onValueChanged.AddListener(SetResolution);
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        languageDropdown.onValueChanged.AddListener(SetLanguage);

        if (timeFormatToggle != null)
            timeFormatToggle.onValueChanged.AddListener(SetTimeFormat);

        if (dateFormatDropdown != null)
            dateFormatDropdown.onValueChanged.AddListener(SetDateFormat);

        if (notificationsToggle != null)
            notificationsToggle.onValueChanged.AddListener(SetNotifications);

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetSettings);

        LoadSettings();
    }

    // --- Методы управления ---
    private void SetMasterVolume(float value)
    {
        PlayerPrefs.SetFloat("MasterVolume", value);
        ApplyVolumeSettings();
    }

    private void SetMusicVolume(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
        ApplyVolumeSettings();
    }

    private void SetSFXVolume(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
        ApplyVolumeSettings();
    }

    // 🔊 Перерасчёт всех громкостей
    private void ApplyVolumeSettings()
    {
        float master = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float music = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        float sfx = PlayerPrefs.GetFloat("SFXVolume", 0.8f);

        // Общая громкость влияет на всё
        float finalMusic = master * music;
        float finalSfx = master * sfx;

        // Главная громкость (глобальная)
        AudioListener.volume = master;

        // Если есть менеджер звуков — обновляем каналы
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(master);
            AudioManager.Instance.SetMusicVolume(music);
            AudioManager.Instance.SetSFXVolume(sfx);
        }

        Debug.Log($"🔊 Master={master:F2}, Music={music:F2}, SFX={sfx:F2}");
    }

    private void SetResolution(int index)
    {
        string[] resParts = options[index].Split('x');
        int width = int.Parse(resParts[0].Trim());
        int height = int.Parse(resParts[1].Trim());

        Screen.SetResolution(width, height, Screen.fullScreen);
        PlayerPrefs.SetInt("ResolutionIndex", index);
    }

    private void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }

    private void SetLanguage(int index)
    {
        PlayerPrefs.SetInt("Language", index);
    }

    // ✅ Формат времени (12/24ч)
    public void SetTimeFormat(bool is12h)
    {
        PlayerPrefs.SetInt("TimeFormat12h", is12h ? 1 : 0);

        if (GameManager.Instance != null && GameManager.Instance.CurrentGame != null)
            GameManager.Instance.CurrentGame.use12HourFormat = is12h;

        HUDController.Instance?.RefreshDateTimeUI();
    }

    // ✅ Формат даты (ДД/ММ или ММ/ДД)
    public void SetDateFormat(int index)
    {
        PlayerPrefs.SetInt("DateFormat", index);

        if (GameManager.Instance != null && GameManager.Instance.CurrentGame != null)
            GameManager.Instance.CurrentGame.isDateFormatDDMM = (index == 0);

        HUDController.Instance?.RefreshDateTimeUI();
    }

    // ✅ Новый метод: включение/выключение всплывающих уведомлений
    private void SetNotifications(bool enabled)
    {
        PlayerPrefs.SetInt("NotificationsEnabled", enabled ? 1 : 0);

        if (GameManager.Instance != null && GameManager.Instance.Data != null)
            GameManager.Instance.Data.notificationsEnabled = enabled;

        Debug.Log($"🔔 Всплывающие уведомления: {(enabled ? "включены" : "выключены")}");
    }

    private void LoadSettings()
    {
        float defaultMaster = 0.8f;
        float defaultMusic = 0.7f;
        float defaultSfx = 0.8f;
        bool defaultFullscreen = true;
        int defaultLang = 0;
        int defaultResIndex = resolutionDropdown.value;
        bool defaultTimeFormat12h = false;
        int defaultDateFormat = 0;
        bool defaultNotifications = true;

        // --- Громкость ---
        float master = PlayerPrefs.GetFloat("MasterVolume", defaultMaster);
        masterVolumeSlider.value = master;

        float music = PlayerPrefs.GetFloat("MusicVolume", defaultMusic);
        musicVolumeSlider.value = music;

        float sfx = PlayerPrefs.GetFloat("SFXVolume", defaultSfx);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = sfx;

        ApplyVolumeSettings();

        // --- Разрешение ---
        int resIndex = PlayerPrefs.GetInt("ResolutionIndex", defaultResIndex);
        resIndex = Mathf.Clamp(resIndex, 0, options.Count - 1);
        resolutionDropdown.value = resIndex;
        resolutionDropdown.RefreshShownValue();

        string[] resParts = options[resIndex].Split('x');
        int width = int.Parse(resParts[0].Trim());
        int height = int.Parse(resParts[1].Trim());
        Screen.SetResolution(width, height, PlayerPrefs.GetInt("Fullscreen", defaultFullscreen ? 1 : 0) == 1);

        // --- Полный экран ---
        bool fullscreen = PlayerPrefs.GetInt("Fullscreen", defaultFullscreen ? 1 : 0) == 1;
        fullscreenToggle.isOn = fullscreen;
        Screen.fullScreen = fullscreen;

        // --- Язык ---
        int langIndex = PlayerPrefs.GetInt("Language", defaultLang);
        languageDropdown.value = langIndex;
        languageDropdown.RefreshShownValue();

        // --- Формат времени ---
        bool use12h = PlayerPrefs.GetInt("TimeFormat12h", defaultTimeFormat12h ? 1 : 0) == 1;
        if (timeFormatToggle != null)
            timeFormatToggle.isOn = use12h;
        if (GameManager.Instance != null && GameManager.Instance.CurrentGame != null)
            GameManager.Instance.CurrentGame.use12HourFormat = use12h;

        // --- Формат даты ---
        int dateFormatIndex = PlayerPrefs.GetInt("DateFormat", defaultDateFormat);
        if (dateFormatDropdown != null)
            dateFormatDropdown.value = dateFormatIndex;
        if (GameManager.Instance != null && GameManager.Instance.CurrentGame != null)
            GameManager.Instance.CurrentGame.isDateFormatDDMM = (dateFormatIndex == 0);

        // --- Всплывающие уведомления ---
        bool notifEnabled = PlayerPrefs.GetInt("NotificationsEnabled", defaultNotifications ? 1 : 0) == 1;
        if (notificationsToggle != null)
            notificationsToggle.isOn = notifEnabled;
        if (GameManager.Instance != null && GameManager.Instance.Data != null)
            GameManager.Instance.Data.notificationsEnabled = notifEnabled;
    }

    private void ResetSettings()
    {
        Debug.Log("[SettingsUI] Сброс настроек");

        PlayerPrefs.DeleteKey("MasterVolume");
        PlayerPrefs.DeleteKey("MusicVolume");
        PlayerPrefs.DeleteKey("SFXVolume");
        PlayerPrefs.DeleteKey("ResolutionIndex");
        PlayerPrefs.DeleteKey("Fullscreen");
        PlayerPrefs.DeleteKey("Language");
        PlayerPrefs.DeleteKey("TimeFormat12h");
        PlayerPrefs.DeleteKey("DateFormat");
        PlayerPrefs.DeleteKey("NotificationsEnabled");

        LoadSettings();
    }
}
