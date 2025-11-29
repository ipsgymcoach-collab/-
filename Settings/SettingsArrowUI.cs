using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class SettingsArrowUI : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Display")]
    [SerializeField] private OptionSelectorUI resolutionSelector;
    [SerializeField] private OptionSelectorUI screenModeSelector;

    [Header("Gameplay / UI")]
    [SerializeField] private OptionSelectorUI languageSelector;
    [SerializeField] private OptionSelectorUI timeFormatSelector;
    [SerializeField] private OptionSelectorUI dateFormatSelector;
    [SerializeField] private OptionSelectorUI notificationsSelector;

    [Header("Video Advanced")]
    [SerializeField] private OptionSelectorUI vSyncSelector;
    [SerializeField] private OptionSelectorUI monitorSelector;
    [SerializeField] private OptionSelectorUI frameRateSelector; // FPS-лимит

    private Resolution[] allResolutions;
    private string[] resolutionOptions;
    private readonly string[] screenModeOptions = { "Оконный", "Без рамки", "Полный экран" };

    // Сохраняем предыдущее состояние EventSystem.sendNavigationEvents,
    // чтобы вернуть его при выходе из панели настроек
    private bool prevSendNavigationEvents = true;

    private void OnEnable()
    {
        var es = EventSystem.current;
        if (es != null)
        {
            prevSendNavigationEvents = es.sendNavigationEvents;
            es.sendNavigationEvents = false;           // 🔒 Гасим навигацию (Submit/Move/Cancel) для клавы/геймпада
            es.SetSelectedGameObject(null);            // Снимаем фокус с последней кнопки
        }
    }

    private void OnDisable()
    {
        var es = EventSystem.current;
        if (es != null)
        {
            es.sendNavigationEvents = prevSendNavigationEvents; // 🔓 Возвращаем как было
            es.SetSelectedGameObject(null);
        }
    }

    private void Start()
    {
        SetupOptions();
        LoadSettings();
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (!gameObject.activeInHierarchy) return;

        // ✅ Enter — только применить настройки (не прожимать UI)
        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            var es = EventSystem.current;
            if (es != null) es.SetSelectedGameObject(null); // снимаем фокус с любых кнопок
            ApplySettings();
            return;
        }

        // ✅ R — только сбросить настройки (не прожимать UI)
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            var es = EventSystem.current;
            if (es != null) es.SetSelectedGameObject(null);
            ResetSettings();
            return;
        }
    }

    private void SetupOptions()
    {
        // === Разрешения ===
        allResolutions = Screen.resolutions;
        var list = new List<string>();
        foreach (var r in allResolutions)
        {
            string res = $"{r.width}x{r.height}";
            if (!list.Contains(res))
                list.Add(res);
        }
        resolutionOptions = list.ToArray();
        resolutionSelector.SetOptions(resolutionOptions);

        // === Режим окна ===
        screenModeSelector.SetOptions(screenModeOptions);

        // === VSync ===
        vSyncSelector.SetOptions(new[] { "Off", "On" });

        // === Мониторы ===
        int monitorCount = Display.displays.Length;
        string[] monitors = new string[monitorCount];
        for (int i = 0; i < monitorCount; i++)
            monitors[i] = $"Monitor {i + 1}";
        monitorSelector.SetOptions(monitors);

        // === FPS-лимит ===
        frameRateSelector.SetOptions(new[] { "30", "60", "90", "120", "144", "165", "240", "Unlimited" });

        // === Язык / Форматы ===
        languageSelector.SetOptions(new[] { "English", "Русский" });
        timeFormatSelector.SetOptions(new[] { "24h", "12h" });
        dateFormatSelector.SetOptions(new[] { "DD/MM/YYYY", "MM/DD/YYYY" });
        notificationsSelector.SetOptions(new[] { "On", "Off" });
    }

    // ======= ПРИМЕНИТЬ =======
    public void ApplySettings()
    {
        // 🎧 Громкость
        PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);
        PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);
        AudioListener.volume = masterVolumeSlider.value;

        // 💻 Разрешение + режим окна
        string resString = resolutionSelector.GetCurrentValue();
        string[] parts = resString.Split('x');
        if (parts.Length == 2 && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h))
        {
            int screenMode = screenModeSelectorIndex(screenModeSelector.GetCurrentValue());
            PlayerPrefs.SetString("Resolution", resString);
            PlayerPrefs.SetInt("ScreenMode", screenMode);
            SetScreenMode(w, h, screenMode);
        }

        // 🎞 VSync
        QualitySettings.vSyncCount = vSyncSelector.GetCurrentValue() == "On" ? 1 : 0;
        PlayerPrefs.SetInt("VSync", QualitySettings.vSyncCount);

        // 🖥 Монитор
        int monitorIndex = monitorSelector != null ? monitorSelector.GetCurrentIndexSafe() : 0;
        PlayerPrefs.SetInt("MonitorIndex", monitorIndex);

#if !UNITY_EDITOR
        if (monitorIndex < Display.displays.Length)
        {
            try { Display.displays[monitorIndex].Activate(); }
            catch { Debug.LogWarning("⚠ Не удалось переключить монитор (ограничения ОС)."); }
        }
#endif

        // 🎮 FPS-лимит
        string fpsValue = frameRateSelector.GetCurrentValue();
        if (fpsValue == "Unlimited")
        {
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0; // на всякий случай отключаем VSync при безлимите
        }
        else if (int.TryParse(fpsValue, out int fps))
        {
            Application.targetFrameRate = fps;
            PlayerPrefs.SetInt("TargetFPS", fps);
        }

        // 🌐 Язык / Форматы / Оповещения
        PlayerPrefs.SetString("Language", languageSelector.GetCurrentValue());
        PlayerPrefs.SetInt("TimeFormat12h", timeFormatSelector.GetCurrentValue() == "12h" ? 1 : 0);
        PlayerPrefs.SetInt("DateFormat", dateFormatSelector.GetCurrentValue() == "DD/MM/YYYY" ? 0 : 1);
        PlayerPrefs.SetInt("NotificationsEnabled", notificationsSelector.GetCurrentValue() == "On" ? 1 : 0);

        PlayerPrefs.Save();
        Debug.Log("✅ Настройки применены (Enter)");
    }

    // ======= ЗАГРУЗИТЬ =======
    public void LoadSettings()
    {
        masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.8f);

        string savedRes = PlayerPrefs.GetString("Resolution", $"{Screen.currentResolution.width}x{Screen.currentResolution.height}");
        resolutionSelector.SetValue(savedRes);

        int screenMode = PlayerPrefs.GetInt("ScreenMode", 2);
        screenModeSelector.SetValue(screenModeOptions[Mathf.Clamp(screenMode, 0, 2)]);

        int vs = PlayerPrefs.GetInt("VSync", 1);
        vSyncSelector.SetValue(vs == 1 ? "On" : "Off");

        int mon = PlayerPrefs.GetInt("MonitorIndex", 0);
        monitorSelector.SetValue($"Monitor {mon + 1}");

        int savedFPS = PlayerPrefs.GetInt("TargetFPS", -1);
        if (savedFPS == -1)
            frameRateSelector.SetValue("Unlimited");
        else
            frameRateSelector.SetValue(savedFPS.ToString());
        Application.targetFrameRate = savedFPS;

        languageSelector.SetValue(PlayerPrefs.GetString("Language", "English"));
        bool use12h = PlayerPrefs.GetInt("TimeFormat12h", 0) == 1;
        timeFormatSelector.SetValue(use12h ? "12h" : "24h");
        bool ddmm = PlayerPrefs.GetInt("DateFormat", 0) == 0;
        dateFormatSelector.SetValue(ddmm ? "DD/MM/YYYY" : "MM/DD/YYYY");
        bool notif = PlayerPrefs.GetInt("NotificationsEnabled", 1) == 1;
        notificationsSelector.SetValue(notif ? "On" : "Off");
    }

    // ======= СБРОС =======
    public void ResetSettings()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        LoadSettings();
        Debug.Log("🔄 Настройки сброшены (R)");
    }

    // ======= ВСПОМОГАТЕЛЬНЫЕ =======
    private void SetScreenMode(int width, int height, int mode)
    {
        switch (mode)
        {
            case 0:
                Screen.fullScreenMode = FullScreenMode.Windowed;
                Screen.SetResolution(width, height, FullScreenMode.Windowed);
                break;
            case 1:
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
                break;
            case 2:
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                Screen.SetResolution(width, height, FullScreenMode.ExclusiveFullScreen);
                break;
        }
    }

    private int screenModeSelectorIndex(string mode)
    {
        switch (mode)
        {
            case "Оконный": return 0;
            case "Без рамки": return 1;
            case "Полный экран": return 2;
            default: return 2;
        }
    }
}
