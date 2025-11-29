using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class NewGameUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject newGamePanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject slotSelectPanel;

    [Header("UI Elements")]
    [SerializeField] private TMP_InputField companyNameInput;
    [SerializeField] private Button nextButton;

    [Header("Selection Buttons")]
    [SerializeField] private Button[] logoButtons;
    [SerializeField] private Button[] heroButtons;

    [Header("Slot Select UI")]
    [SerializeField] private SlotUI[] slotUIs;
    [SerializeField] private Sprite[] logoSprites;
    [SerializeField] private Sprite[] heroSprites;

    [Header("Confirm Overwrite UI")]
    [SerializeField] private GameObject confirmOverwritePanel;
    [SerializeField] private TextMeshProUGUI overwriteMessageText;
    [SerializeField] private Button overwriteYesButton;
    [SerializeField] private Button overwriteNoButton;

    [Header("Game Scene")]
    [SerializeField] private string gameSceneName = "OfficeScene";

    [Header("Preview (опционально)")]
    [SerializeField] private PreviewManager previewManager;

    [SerializeField] private LoadGameUI loadGameUI;


    private int selectedLogoId = -1;
    private int selectedHeroId = -1;
    private string companyName = "";
    private int pendingSlotForOverwrite = -1;

    private InputAction backAction;

    private void Awake()
    {
        backAction = new InputAction(name: "Back", type: InputActionType.Button);
        backAction.AddBinding("<Keyboard>/escape");
        backAction.performed += OnBack;
    }

    private void OnEnable()
    {
        backAction.Enable();
        ValidateNextButton();
        ResetHighlights();
        if (slotSelectPanel != null) slotSelectPanel.SetActive(false);
        if (confirmOverwritePanel != null) confirmOverwritePanel.SetActive(false);
    }

    private void OnDisable()
    {
        backAction.Disable();
    }

    private void OnDestroy()
    {
        backAction.performed -= OnBack;
        backAction.Dispose();
    }

    // ===== Выборы =====
    public void SelectLogo(int id)
    {
        selectedLogoId = id;
        HighlightSelection(logoButtons, id);
        ValidateNextButton();
        if (previewManager != null) previewManager.ShowLogo(id);
    }

    public void SelectHero(int id)
    {
        selectedHeroId = id;
        HighlightSelection(heroButtons, id);
        ValidateNextButton();
        if (previewManager != null) previewManager.ShowHero(id);
    }

    private void HighlightSelection(Button[] buttons, int selectedId)
    {
        if (buttons == null) return;
        for (int i = 0; i < buttons.Length; i++)
        {
            var img = buttons[i].GetComponent<Image>();
            if (img != null) img.color = (i == selectedId) ? new Color(0.7f, 0.7f, 0.7f, 1f) : Color.white;
        }
    }

    private void ResetHighlights()
    {
        HighlightSelection(logoButtons, -1);
        HighlightSelection(heroButtons, -1);
    }

    // ===== Валидация «Далее» =====
    private void ValidateNextButton()
    {
        nextButton.interactable =
            !string.IsNullOrWhiteSpace(companyNameInput?.text) &&
            selectedLogoId >= 0 &&
            selectedHeroId >= 0;
    }

    public void OnCompanyNameChanged(string _)
    {
        if (companyNameInput != null)
        {
            companyName = companyNameInput.text;
        }
        ValidateNextButton();
    }

    // ===== Этап выбора слота =====
    public void OpenSlotSelect()
    {
        slotSelectPanel.SetActive(true);
        RefreshSlotUIs();
    }

    private void RefreshSlotUIs()
    {
        for (int i = 0; i < slotUIs.Length; i++)
        {
            int slotIndex = i + 1;

            GameData data = SaveManager.HasSave(slotIndex) ? SaveManager.PeekSave(slotIndex) : null;

            slotUIs[i].SetupSlot(data, logoSprites, heroSprites);

            var slotBtn = slotUIs[i].GetComponent<Button>();
            if (slotBtn != null)
            {
                slotBtn.onClick.RemoveAllListeners();
                int captured = slotIndex;
                slotBtn.onClick.AddListener(() => OnSlotClicked(captured));
            }

            if (slotUIs[i].DeleteButton != null)
                slotUIs[i].DeleteButton.gameObject.SetActive(false);
        }
    }

    private void OnSlotClicked(int slot)
    {
        if (!SaveManager.HasSave(slot))
        {
            SaveAndStart(slot);
            return;
        }

        var existing = SaveManager.PeekSave(slot);
        pendingSlotForOverwrite = slot;

        if (overwriteMessageText != null)
        {
            string existName = existing != null ? existing.companyName : "неизвестно";
            overwriteMessageText.text = $"Слот {slot} уже содержит профиль «{existName}».\nПерезаписать его новым профилем «{companyName}»?";
        }

        overwriteYesButton.onClick.RemoveAllListeners();
        overwriteYesButton.onClick.AddListener(ConfirmOverwrite);

        overwriteNoButton.onClick.RemoveAllListeners();
        overwriteNoButton.onClick.AddListener(CancelOverwrite);

        confirmOverwritePanel.SetActive(true);
    }

    private void ConfirmOverwrite()
    {
        if (pendingSlotForOverwrite <= 0) { CancelOverwrite(); return; }
        SaveAndStart(pendingSlotForOverwrite);
        pendingSlotForOverwrite = -1;
        confirmOverwritePanel.SetActive(false);
    }

    private void CancelOverwrite()
    {
        pendingSlotForOverwrite = -1;
        confirmOverwritePanel.SetActive(false);
    }

    // ===== СОЗДАНИЕ НОВОЙ ИГРЫ =====
    private void SaveAndStart(int slot)
    {
        var data = new GameData();
        data.companyName = companyName;
        data.selectedLogoId = selectedLogoId;
        data.selectedHeroId = selectedHeroId;

        // 🔧 Выдаём стартовые машины при создании новой игры
        data.GiveStartingVehicles();
        data.GiveStartingWorkers(); // 🆕 Добавляем стартовых работников

        // 🔹 Уровень и опыт по умолчанию
        data.level = 1;
        data.xp = 0;

        // Формат времени из настроек (дефолт: 12ч)
        data.use12HourFormat = PlayerPrefs.GetInt("TimeFormat12h", 1) == 1;

        // 🔹 Сброс времени при новой игре
        data.day = 15;
        data.month = 1;
        data.year = 2017;
        data.hour = 9;
        data.minute = 0;
        data.totalGameSeconds = 0;

        // 🔹 Инициализация долга
        data.currentDebt = data.startingDebt;
        data.totalDebtPaid = 0;

        // Сохраняем данные
        SaveManager.SaveGame(data, slot);

        // Устанавливаем их активными
        GameManager.Instance.SetCurrentGame(data, slot);

        // Загружаем сцену
        SceneManager.LoadScene(gameSceneName);
    }

    // ===== Навигация / ESC =====
    private void OnBack(InputAction.CallbackContext _)
    {
        if (confirmOverwritePanel != null && confirmOverwritePanel.activeSelf)
        {
            CancelOverwrite();
            return;
        }

        slotSelectPanel.SetActive(false);
        newGamePanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        // если панель не активна, ничего не делаем
        if (!newGamePanel.activeInHierarchy)
            return;

        // === ENTER — только "Далее" ===
        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es != null)
            {
                es.SetSelectedGameObject(null);
            }

            if (nextButton != null && nextButton.interactable)
            {
                nextButton.onClick.Invoke();
            }
            return;
        }

        // === ESCAPE — возврат ===
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            OnBack(default);
        }
    }

    // ===== Для работы ESC из MainMenuUI =====
    public bool IsOnSaveSlotSelect
    {
        get
        {
            return slotSelectPanel != null && slotSelectPanel.activeSelf;
        }
    }

    public void ReturnToCompanySetup()
    {
        if (slotSelectPanel != null)
            slotSelectPanel.SetActive(false);

        if (newGamePanel != null)
            newGamePanel.SetActive(true);

        Debug.Log("↩ Возврат к созданию компании");
    }
}

