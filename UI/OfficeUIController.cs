using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class OfficeUIController : MonoBehaviour
{
    [Header("Панели")]
    [SerializeField] private GameObject bankPanel;
    [SerializeField] private GameObject forbesPanel;
    [SerializeField] private GameObject ordersPanel;

    [Header("Логистика (новая панель)")]
    [SerializeField] private GameObject logisticsPanel;

    [Header("Кнопки открытия панелей")]
    [SerializeField] private Button bankButton;
    [SerializeField] private Button garageButton;
    [SerializeField] private Button forbesButton;
    [SerializeField] private Button ordersButton;
    [SerializeField] private Button logisticsButton;

    [Header("Кнопки назад")]
    [SerializeField] private Button bankBackButton;
    [SerializeField] private Button forbesBackButton;
    [SerializeField] private Button ordersBackButton;
    [SerializeField] private Button logisticsBackButton;

    [Header("Контроллер вкладок логистики")]
    [SerializeField] private LogisticsTabsController logisticsTabsController;

    [Header("Названия сцен")]
    [SerializeField] private string garageSceneName = "GarageScene";
    [SerializeField] private string houseSceneName = "HouseScene";

    private float previousSpeed = 1f;

    private void Start()
    {
        // ============ Открытие панелей ============
        bankButton.onClick.AddListener(() => OpenPanel(bankPanel));
        forbesButton.onClick.AddListener(() => OpenPanel(forbesPanel));
        garageButton.onClick.AddListener(GoToGarage);

        if (ordersButton != null)
            ordersButton.onClick.AddListener(() => OpenPanel(ordersPanel));

        if (logisticsButton != null)
            logisticsButton.onClick.AddListener(() =>
            {
                OpenPanel(logisticsPanel);

                // ← ГАРАНТИЯ: вкладки ВСЕГДА сбрасываются
                if (logisticsTabsController != null)
                    logisticsTabsController.ForceReset();
            });

        // ============ Кнопки Назад ============
        bankBackButton.onClick.AddListener(() => ClosePanel(bankPanel));
        forbesBackButton.onClick.AddListener(() => ClosePanel(forbesPanel));

        if (ordersBackButton != null)
            ordersBackButton.onClick.AddListener(() => ClosePanel(ordersPanel));

        if (logisticsBackButton != null)
            logisticsBackButton.onClick.AddListener(() => ClosePanel(logisticsPanel));

        // ============ Скрываем панели ============
        HideSilent(bankPanel);
        HideSilent(forbesPanel);
        HideSilent(ordersPanel);
        HideSilent(logisticsPanel);

        GameManager.Instance.IsUIOpen = false;
        HUDController.Instance?.EnableControls();

        PlayOfficeMusic();
    }

    private void PlayOfficeMusic()
    {
        if (AudioManager.Instance == null) return;

        AudioClip music = Resources.Load<AudioClip>("Audio/Music/office_theme");
        if (music != null)
            AudioManager.Instance.PlayMusic(music);
    }

    // ============================================================
    // 🔥 ЕДИНЫЙ МЕТОД ОТКРЫТИЯ ПАНЕЛЕЙ
    // ============================================================
    private void OpenPanel(GameObject panel)
    {
        PauseGame();
        panel.SetActive(true);

        EnableRaycaster(panel);
        EventSystem.current?.SetSelectedGameObject(null);
    }

    // ============================================================
    // 🔥 ЕДИНЫЙ МЕТОД ЗАКРЫТИЯ ПАНЕЛЕЙ
    // ============================================================
    private void ClosePanel(GameObject panel)
    {
        panel.SetActive(false);
        DisableRaycaster(panel);

        EventSystem.current?.SetSelectedGameObject(null);
        ResumeGame();
    }

    private void HideSilent(GameObject panel)
    {
        if (panel == null) return;

        panel.SetActive(false);
        DisableRaycaster(panel);
    }

    private void EnableRaycaster(GameObject panel)
    {
        var rc = panel.GetComponent<GraphicRaycaster>();
        if (rc != null) rc.enabled = true;
    }

    private void DisableRaycaster(GameObject panel)
    {
        var rc = panel.GetComponent<GraphicRaycaster>();
        if (rc != null) rc.enabled = false;
    }

    // ============================================================
    // Управление временем
    // ============================================================
    private void PauseGame()
    {
        if (TimeController.Instance != null)
        {
            previousSpeed = TimeController.Instance.GameSpeed;
            TimeController.Instance.SetPause(true);
        }

        GameManager.Instance.IsUIOpen = true;
        HUDController.Instance?.DisableControls();
    }

    public void CloseLogistics()
    {
        if (logisticsPanel != null)
            ClosePanel(logisticsPanel);
    }

    private void ResumeGame()
    {
        GameManager.Instance.IsUIOpen = false;
        HUDController.Instance?.EnableControls();

        if (TimeController.Instance != null)
            TimeController.Instance.SetSpeed(previousSpeed > 0 ? previousSpeed : 1f);
    }

    // ============================================================
    // Переходы между сценами
    // ============================================================
    private void GoToGarage()
    {
        SceneManager.LoadScene(garageSceneName);
    }

    private void GoToHouse()
    {
        SceneManager.LoadScene(houseSceneName);
    }
}
