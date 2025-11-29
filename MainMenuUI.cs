using UnityEngine;
using UnityEngine.InputSystem;

public class MainMenuUI : MonoBehaviour
{
    [Header("Главные панели")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject newGamePanel;
    [SerializeField] private GameObject loadGamePanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Вкладки настроек")]
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject videoPanel;
    [SerializeField] private GameObject settingsButtons;

    [Header("Скрипты панелей")]
    [SerializeField] private NewGameUI newGameUI;   // перетащить объект с NewGameUI
    [SerializeField] private LoadGameUI loadGameUI; // перетащить объект с LoadGameUI

    // локальный ESC
    private InputAction backAction;

    private void Awake()
    {
        backAction = new InputAction(name: "Back", type: InputActionType.Button);
        backAction.AddBinding("<Keyboard>/escape");
        backAction.performed += OnBackPerformed;
    }

    private void OnEnable() => backAction?.Enable();
    private void OnDisable() => backAction?.Disable();

    private void OnDestroy()
    {
        if (backAction != null)
        {
            backAction.performed -= OnBackPerformed;
            backAction.Dispose();
            backAction = null;
        }
    }

    private void Start()
    {
        ShowMainMenu();
    }

    // ================= ESC =================
    private void OnBackPerformed(InputAction.CallbackContext ctx)
    {
        // Настройки открыты → закрыть и вернуться в главное меню
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            BackFromSettings();
            return;
        }

        // Новая игра открыта
        if (newGamePanel != null && newGamePanel.activeSelf)
        {
            // Если сейчас выбор слота — вернуться на создание компании
            if (newGameUI != null && newGameUI.IsOnSaveSlotSelect)
            {
                newGameUI.ReturnToCompanySetup();
                return;
            }

            // Иначе закрыть «Новая игра» и показать главное меню
            SetPanel(newGamePanel, false);
            SetPanel(mainMenuPanel, true);
            return;
        }

        // Загрузка игры открыта
        if (loadGamePanel != null && loadGamePanel.activeSelf)
        {
            // Сначала закрыть всплывающие окна (удаление/подтверждение)
            if (loadGameUI != null)
                loadGameUI.CloseAllPopups();

            SetPanel(loadGamePanel, false);
            SetPanel(mainMenuPanel, true);
            return;
        }

        // В главном меню — игнор
        if (mainMenuPanel != null && mainMenuPanel.activeSelf)
            return;
    }

    // ================= Панели =================
    public void ShowMainMenu()
    {
        SetPanel(mainMenuPanel, true);
        SetPanel(newGamePanel, false);
        SetPanel(loadGamePanel, false);
        SetPanel(settingsPanel, false);

        SetPanel(gameplayPanel, false);
        SetPanel(controlsPanel, false);
        SetPanel(videoPanel, false);
        SetPanel(settingsButtons, false);
    }

    public void OpenNewGame()
    {
        SetPanel(mainMenuPanel, false);
        SetPanel(newGamePanel, true);
        SetPanel(loadGamePanel, false);
        SetPanel(settingsPanel, false);

        // гарантируем старт с экрана создания компании
        if (newGameUI != null)
            newGameUI.ReturnToCompanySetup();
    }

    public void OpenLoadGame()
    {
        SetPanel(mainMenuPanel, false);
        SetPanel(newGamePanel, false);
        SetPanel(loadGamePanel, true);
        SetPanel(settingsPanel, false);

        // при входе в загрузку — на всякий случай закрыть попапы
        if (loadGameUI != null)
            loadGameUI.CloseAllPopups();
    }

    public void OpenSettings()
    {
        SetPanel(mainMenuPanel, false);
        SetPanel(newGamePanel, false);
        SetPanel(loadGamePanel, false);

        SetPanel(settingsButtons, true);
        SetPanel(settingsPanel, true);

        OpenGameplayPanel(); // первая вкладка по умолчанию
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ================= Вкладки настроек =================
    public void OpenGameplayPanel()
    {
        SetPanel(gameplayPanel, true);
        SetPanel(controlsPanel, false);
        SetPanel(videoPanel, false);
    }

    public void OpenControlsPanel()
    {
        SetPanel(gameplayPanel, false);
        SetPanel(controlsPanel, true);
        SetPanel(videoPanel, false);
    }

    public void OpenVideoPanel()
    {
        SetPanel(gameplayPanel, false);
        SetPanel(controlsPanel, false);
        SetPanel(videoPanel, true);
    }

    public void BackFromSettings()
    {
        SetPanel(settingsButtons, false);
        SetPanel(settingsPanel, false);
        SetPanel(gameplayPanel, false);
        SetPanel(controlsPanel, false);
        SetPanel(videoPanel, false);
        SetPanel(mainMenuPanel, true);
    }

    // ================= Утилита =================
    private static void SetPanel(GameObject panel, bool state)
    {
        if (panel != null && panel.activeSelf != state)
            panel.SetActive(state);
    }
}
