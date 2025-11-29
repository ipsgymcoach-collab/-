using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class LoadGameUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject loadGamePanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject confirmPanel;       // окно подтверждения загрузки

    [Header("Confirm Load UI")]
    [SerializeField] private TextMeshProUGUI confirmMessage;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("Delete Confirm UI")]
    [SerializeField] private GameObject confirmDeletePanel; // окно подтверждения удаления
    [SerializeField] private TextMeshProUGUI deleteMessage;
    [SerializeField] private Button deleteYesButton;
    [SerializeField] private Button deleteNoButton;

    [Header("Slot UI")]
    [SerializeField] private SlotUI[] slotUIs;
    [SerializeField] private Sprite[] logoSprites;
    [SerializeField] private Sprite[] heroSprites;

    [Header("Game Scene")]
    [SerializeField] private string gameSceneName = "OfficeScene";

    [Header("Mode")]
    [SerializeField] private bool isPauseMenuLoader = false; // true = из ESC-меню, false = главное меню

    private int pendingSlot = -1;
    private int pendingDeleteSlot = -1;
    private InputAction backAction;

    private void Awake()
    {
        // Настройка ESC для возврата в меню
        backAction = new InputAction(name: "Back", type: InputActionType.Button);
        backAction.AddBinding("<Keyboard>/escape");
        backAction.performed += ctx => BackToMain();
    }

    private void OnEnable()
    {
        backAction.Enable();
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (confirmDeletePanel != null) confirmDeletePanel.SetActive(false);
        RefreshSlots();
    }

    private void OnDisable()
    {
        backAction.Disable();
    }

    // === Обновление слотов ===
    private void RefreshSlots()
    {
        for (int i = 0; i < slotUIs.Length; i++)
        {
            int slotIndex = i + 1;
            GameData previewData = SaveManager.HasSave(slotIndex) ? SaveManager.LoadGame(slotIndex) : null;

            slotUIs[i].SetupSlot(previewData, logoSprites, heroSprites);

            int captured = slotIndex;
            var btn = slotUIs[i].GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnSlotClicked(captured, previewData));
            }

            if (slotUIs[i].DeleteButton != null)
            {
                slotUIs[i].DeleteButton.onClick.RemoveAllListeners();
                slotUIs[i].DeleteButton.onClick.AddListener(() => OnDeleteClicked(captured, previewData));
            }
        }
    }

    // === Выбор слота ===
    private void OnSlotClicked(int slot, GameData data)
    {
        if (data == null)
        {
            Debug.Log("[LoadGameUI] Слот пустой");
            return;
        }

        pendingSlot = slot;

        if (confirmMessage != null)
            confirmMessage.text = $"Вы хотите загрузить профиль компании «{data.companyName}»?";

        yesButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(ConfirmLoad);

        noButton.onClick.RemoveAllListeners();
        noButton.onClick.AddListener(CancelLoad);

        if (confirmPanel != null) confirmPanel.SetActive(true);

    }


    // === Подтверждение загрузки ===
    private void ConfirmLoad()
    {
        if (pendingSlot > 0)
        {
            GameData data = SaveManager.LoadGame(pendingSlot);
            if (data == null)
            {
                Debug.LogError($"[LoadGameUI] Ошибка загрузки из слота {pendingSlot}");
                return;
            }

            // Проверка уровня и опыта
            if (data.level <= 0) data.level = 1;
            if (data.xp < 0) data.xp = 0;

            // Формат времени
            data.use12HourFormat = PlayerPrefs.GetInt("TimeFormat12h", 1) == 1;

            // Устанавливаем активную игру
            GameManager.Instance.SetCurrentGame(data, pendingSlot);

            if (!isPauseMenuLoader)
            {
                // Главное меню → загружаем сцену
                SceneManager.LoadScene(gameSceneName);
            }
            else
            {
                // ESC меню → обновляем в текущей сцене
                var time = TimeController.Instance;
                var hud = HUDController.Instance;

                if (time != null)
                    time.LoadFromGameData(data);

                if (hud != null)
                {
                    if (!hud.enabled) hud.enabled = true;
                    hud.UpdateHUD(data);
                    hud.RefreshDateTimeUI();
                    hud.UpdateSpeedButtonColors();
                }

                if (time != null)
                    time.SetPause(true); // оставляем паузу

                if (loadGamePanel != null)
                    loadGamePanel.SetActive(false);

                Debug.Log($"[LoadGameUI] Загрузка из ESC: {data.day:D2}/{data.month:D2}/{data.year} {data.hour:D2}:{data.minute:D2}");
            }
        }

        if (confirmPanel != null) confirmPanel.SetActive(false);
        pendingSlot = -1;
    }

    private void CancelLoad()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);
        pendingSlot = -1;
    }

    // === Удаление сохранения ===
    private void OnDeleteClicked(int slot, GameData data)
    {
        if (data == null)
        {
            Debug.Log("[LoadGameUI] Нечего удалять, слот пустой");
            return;
        }

        pendingDeleteSlot = slot;

        if (deleteMessage != null)
            deleteMessage.text = $"Удалить сохранение компании «{data.companyName}» из слота {slot}?";

        deleteYesButton.onClick.RemoveAllListeners();
        deleteYesButton.onClick.AddListener(ConfirmDelete);

        deleteNoButton.onClick.RemoveAllListeners();
        deleteNoButton.onClick.AddListener(CancelDelete);

        if (confirmDeletePanel != null) confirmDeletePanel.SetActive(true);
    }

    private void ConfirmDelete()
    {
        if (pendingDeleteSlot > 0)
        {
            SaveManager.DeleteSave(pendingDeleteSlot);
            RefreshSlots();
        }

        if (confirmDeletePanel != null) confirmDeletePanel.SetActive(false);
        pendingDeleteSlot = -1;
    }

    private void CancelDelete()
    {
        if (confirmDeletePanel != null) confirmDeletePanel.SetActive(false);
        pendingDeleteSlot = -1;
    }

    // === Навигация ===
    private void BackToMain()
    {
        if (loadGamePanel != null) loadGamePanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void CloseAllPopups()
    {
        // Закрываем окно подтверждения удаления
        if (confirmDeletePanel != null && confirmDeletePanel.activeSelf)
            confirmDeletePanel.SetActive(false);

        // Можно добавить другие окна, если появятся
        Debug.Log("🧹 Окна подтверждения в LoadGameUI закрыты.");
    }
}
