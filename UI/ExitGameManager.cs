using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ExitGameManager : MonoBehaviour
{
    [Header("UI Панель выхода")]
    [SerializeField] private GameObject exitConfirmPanel;
    [SerializeField] private Button saveAndExitButton;
    [SerializeField] private Button exitWithoutSaveButton;
    [SerializeField] private Button cancelButton;

    public static bool IsExitConfirmOpen { get; private set; } = false;

    private void Start()
    {
        if (exitConfirmPanel != null)
            exitConfirmPanel.SetActive(false);

        if (saveAndExitButton != null)
            saveAndExitButton.onClick.AddListener(OnSaveAndExit);

        if (exitWithoutSaveButton != null)
            exitWithoutSaveButton.onClick.AddListener(OnExitWithoutSave);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancel);
    }

    public void ShowExitConfirm()
    {
        if (exitConfirmPanel != null)
            exitConfirmPanel.SetActive(true);

        IsExitConfirmOpen = true;
        Time.timeScale = 0f;
    }

    private void OnSaveAndExit()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentGame != null)
        {
            SaveManager.SaveGame(GameManager.Instance.CurrentGame, GameManager.Instance.CurrentSlot);
        }

        // 🟢 Сбрасываем все флаги UI перед выходом
        if (GameManager.Instance != null)
            GameManager.Instance.IsUIOpen = false;
        UIManager.ResetPanels();

        Time.timeScale = 1f;
        IsExitConfirmOpen = false;

        SceneManager.LoadScene("MainMenu");
    }

    private void OnExitWithoutSave()
    {
        // 🟢 Сбрасываем все флаги UI перед выходом
        if (GameManager.Instance != null)
            GameManager.Instance.IsUIOpen = false;
        UIManager.ResetPanels();

        Time.timeScale = 1f;
        IsExitConfirmOpen = false;

        SceneManager.LoadScene("MainMenu");
    }

    private void OnCancel()
    {
        if (exitConfirmPanel != null)
            exitConfirmPanel.SetActive(false);

        IsExitConfirmOpen = false;
        Time.timeScale = 0f;

        if (GameManager.Instance != null)
            GameManager.Instance.IsUIOpen = false;
    }
}
