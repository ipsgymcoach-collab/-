using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Кнопка выхода")]
    [SerializeField] private Button exitButton;

    private bool isPaused = false;
    private InputAction pauseAction;

    private readonly List<Graphic> disabledGraphics = new List<Graphic>();
    private ExitGameManager exitGameManager;

    private void Awake()
    {
        pauseAction = new InputAction(name: "Pause", type: InputActionType.Button);
        pauseAction.AddBinding("<Keyboard>/escape");
        pauseAction.performed += OnPausePerformed;
    }

    private void Start()
    {
        exitGameManager = FindFirstObjectByType<ExitGameManager>();
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitPressed);
    }

    private void OnEnable()
    {
        pauseAction.Enable();

        if (!isPaused && pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void OnDisable()
    {
        pauseAction.Disable();
    }

    private void OnDestroy()
    {
        pauseAction.performed -= OnPausePerformed;
        pauseAction.Dispose();
    }

    private void OnPausePerformed(InputAction.CallbackContext _)
    {
        // 🚫 Если открыто окно подтверждения выхода — игнорируем ESC
        if (ExitGameManager.IsExitConfirmOpen)
        {
            Debug.Log("[PauseMenuManager] ESC проигнорирован (ExitConfirm открыто).");
            return;
        }

        if (!isPaused && UIManager.IsAnyPanelOpen())
            return;

        if (isPaused) ResumeGame();
        else PauseGame();
    }

    // ===== Пауза =====
    public void PauseGame()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);

        UIManager.RegisterPanel(pauseMenuPanel);
        isPaused = true;

        if (TimeController.Instance != null)
            TimeController.Instance.SetPause(true);

        if (GameManager.Instance != null)
            GameManager.Instance.IsUIOpen = true;

        DisableAllGameplayUI();

        var cam = Object.FindFirstObjectByType<CameraController>();
        if (cam != null) cam.enabled = false;

        if (HUDController.Instance != null)
            HUDController.Instance.enabled = false;
    }

    public void ResumeGame()
    {
        HideAllPanels();
        isPaused = false;

        if (TimeController.Instance != null)
            TimeController.Instance.SetPause(false);

        RestoreDisabledUI();

        if (GameManager.Instance != null)
            GameManager.Instance.IsUIOpen = false;

        UIManager.UnregisterPanel(pauseMenuPanel);

        var cam = Object.FindFirstObjectByType<CameraController>();
        if (cam != null) cam.enabled = true;

        if (HUDController.Instance != null)
            HUDController.Instance.enabled = true;
    }

    // ===== Панели =====
    private void HideAllPanels()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        UIManager.UnregisterPanel(pauseMenuPanel);
        UIManager.UnregisterPanel(settingsPanel);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            UIManager.RegisterPanel(settingsPanel);

            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
                UIManager.UnregisterPanel(pauseMenuPanel);
            }

            if (TimeController.Instance != null)
                TimeController.Instance.SetPause(true);

            var cam = Object.FindFirstObjectByType<CameraController>();
            if (cam != null) cam.enabled = false;

            if (HUDController.Instance != null)
                HUDController.Instance.enabled = false;
        }
    }

    public void BackToPauseMenu(GameObject currentPanel)
    {
        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
            UIManager.UnregisterPanel(currentPanel);
        }

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            UIManager.RegisterPanel(pauseMenuPanel);

            if (TimeController.Instance != null)
                TimeController.Instance.SetPause(true);
        }

        var cam = Object.FindFirstObjectByType<CameraController>();
        if (cam != null) cam.enabled = false;

        if (HUDController.Instance != null)
            HUDController.Instance.enabled = false;
    }

    // ===== Сохранение =====
    public void SaveGame()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentGame == null)
        {
            Debug.LogError("[PauseMenu] Нет активной игры для сохранения!");
            return;
        }

        int slot = GameManager.Instance.CurrentSlot;
        if (slot <= 0)
        {
            Debug.LogError("[PauseMenu] Не выбран слот для сохранения!");
            return;
        }

        var data = GameManager.Instance.CurrentGame;

        if (TimeController.Instance != null)
            TimeController.Instance.SaveToGameData(data);

        if (SceneManager.GetActiveScene().name == "OfficeScene")
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                data.cameraPosX = cam.transform.position.x;
                data.cameraPosY = cam.transform.position.y;
                data.cameraPosZ = cam.transform.position.z;

                data.cameraRotX = cam.transform.eulerAngles.x;
                data.cameraRotY = cam.transform.eulerAngles.y;
                data.cameraRotZ = cam.transform.eulerAngles.z;

                data.hasSavedCamera = true;
            }
        }
        else
        {
            data.hasSavedCamera = false;
        }

        data.lastSaveTime = $"{data.day:D2}/{data.month:D2}/{data.year} {data.hour:D2}:{data.minute:D2}";
        SaveManager.SaveGame(data, slot);

        Debug.Log($"[PauseMenu] Сохранено в слот {slot} ({data.companyName}) — {data.lastSaveTime}");
    }

    // ===== Выход =====
    private void OnExitPressed()
    {
        if (exitGameManager != null)
        {
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
                UIManager.UnregisterPanel(pauseMenuPanel); // 🟢 фикс
            }

            if (GameManager.Instance != null)
                GameManager.Instance.IsUIOpen = true; // окно выхода считаем UI

            exitGameManager.ShowExitConfirm();
        }
        else
        {
            Debug.LogError("[PauseMenuManager] ExitGameManager не найден!");
        }
    }

    // ===== Блокировка игровых UI =====
    private void DisableAllGameplayUI()
    {
        disabledGraphics.Clear();
        var all = Object.FindObjectsByType<Graphic>(FindObjectsSortMode.None);

        foreach (var g in all)
        {
            if (g == null) continue;
            if (IsInsidePausePanels(g.transform)) continue;

            if (g.raycastTarget)
            {
                g.raycastTarget = false;
                disabledGraphics.Add(g);
            }
        }
    }

    private void RestoreDisabledUI()
    {
        for (int i = 0; i < disabledGraphics.Count; i++)
        {
            if (disabledGraphics[i] != null)
                disabledGraphics[i].raycastTarget = true;
        }
        disabledGraphics.Clear();
    }

    private bool IsInsidePausePanels(Transform t)
    {
        if (pauseMenuPanel != null && t.IsChildOf(pauseMenuPanel.transform))
            return true;

        if (settingsPanel != null && t.IsChildOf(settingsPanel.transform))
            return true;

        return false;
    }
}
