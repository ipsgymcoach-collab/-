using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class IntroNpcEventGarage : MonoBehaviour
{
    [Header("UI элементы диалога")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("Катсцена (заставка)")]
    [SerializeField] private CutsceneController cutsceneController;
    [SerializeField] private Sprite cleanupSprite;

    [Header("Объекты для очистки")]
    [SerializeField] private GameObject garageProps;

    [Header("Игрок и деньги")]
    [SerializeField] private int cleanupCost = 1000;

    [Header("Диалоговые реплики (первый визит)")]
    [TextArea(2, 5)]
    [SerializeField] private string[] dialogueLines;

    private int currentLine = 0;
    private bool dialogueActive = false;

    private InputAction nextLineAction;

    private GameData Data => GameManager.Instance.CurrentGame;
    private const string CleanupEventKey = "cleanup_event";

    private float savedSpeed = 1f; // ⏳ запоминаем скорость игрока

    private void Awake()
    {
        nextLineAction = new InputAction("NextLine", binding: "<Keyboard>/anyKey");
        nextLineAction.AddBinding("<Mouse>/leftButton");
        nextLineAction.performed += _ => OnNextLine();
    }

    private void OnEnable() => nextLineAction.Enable();
    private void OnDisable() => nextLineAction.Disable();

    private void Start()
    {
        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);
        dialoguePanel.SetActive(false);

        yesButton.onClick.AddListener(OnYes);
        noButton.onClick.AddListener(OnNo);

        // 🔎 Сохраняем текущую скорость игрока при входе в гараж
        if (TimeController.Instance != null)
            savedSpeed = TimeController.Instance.GameSpeed;

        int state = 0;
        if (Data != null && Data.eventFlags.ContainsKey(CleanupEventKey))
            state = Data.eventFlags[CleanupEventKey];

        if (state == 2)
        {
            if (garageProps != null) garageProps.SetActive(false);
            Debug.Log("[Garage] Уборка завершена ранее, NPC не появляется.");
            return;
        }

        if (state == 1)
        {
            StartShortDialogue("Привет! Появились деньги на расчистку?");
            return;
        }

        StartDialogue();
    }

    private void StartDialogue()
    {
        dialoguePanel.SetActive(true);
        dialogueActive = true;
        currentLine = 0;

        if (HUDController.Instance != null)
            HUDController.Instance.DisableControls();

        // ⏸ ставим игру на паузу
        if (TimeController.Instance != null)
            TimeController.Instance.SetPause();

        ShowCurrentLine();
    }

    private void StartShortDialogue(string text)
    {
        dialoguePanel.SetActive(true);
        dialogueText.text = text;

        if (HUDController.Instance != null)
            HUDController.Instance.DisableControls();

        if (TimeController.Instance != null)
            TimeController.Instance.SetPause();

        yesButton.gameObject.SetActive(true);
        noButton.gameObject.SetActive(true);
    }

    private void ShowCurrentLine()
    {
        dialogueText.text = dialogueLines[currentLine] +
                            "\n\n<color=grey>Нажмите любую клавишу...</color>";
    }

    private void OnNextLine()
    {
        if (!dialogueActive || yesButton.gameObject.activeSelf) return;

        currentLine++;

        if (currentLine < dialogueLines.Length)
        {
            ShowCurrentLine();
        }
        else
        {
            dialogueActive = false;
            dialogueText.text =
                "Но грязи после них осталось очень много, да и эти баннеры... " +
                "Я знаю людей, за 1,000$ — они тут быстро наведут порядок и даже постеры распечатают. По рукам?";

            yesButton.gameObject.SetActive(true);
            noButton.gameObject.SetActive(true);
        }
    }

    private void OnYes()
    {
        if (Data == null) return;

        if (Data.money >= cleanupCost)
        {
            Data.money -= cleanupCost;
            Data.eventFlags[CleanupEventKey] = 2;

            HUDController.Instance?.UpdateHUD(Data);
            SaveManager.SaveGame(Data, GameManager.Instance.CurrentSlot);

            dialoguePanel.SetActive(false);

            cutsceneController.PlayCutscene(
                cleanupSprite,
                "Рабочие приехали и начали уборку территории...",
                ClearGarage
            );
        }
        else
        {
            dialogueText.text = "У тебя пока нет таких денег!";
            yesButton.gameObject.SetActive(false);
            noButton.gameObject.SetActive(false);
            Invoke(nameof(HideDialogue), 2f);
        }
    }

    private void OnNo()
    {
        if (Data == null) return;

        Data.eventFlags[CleanupEventKey] = 1;

        dialogueText.text = "Ладно... Возвращайся, когда будут деньги.";
        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);

        SaveManager.SaveGame(Data, GameManager.Instance.CurrentSlot);

        Invoke(nameof(ReturnToOffice), 2f);
    }

    private void HideDialogue()
    {
        dialoguePanel.SetActive(false);

        if (HUDController.Instance != null)
            HUDController.Instance.EnableControls();

        // ⏯ возвращаем игрока к сохранённой скорости
        if (TimeController.Instance != null)
            TimeController.Instance.SetSpeed(savedSpeed);
    }

    private void ClearGarage()
    {
        if (garageProps != null)
            garageProps.SetActive(false);

        if (HUDController.Instance != null)
            HUDController.Instance.EnableControls();

        if (TimeController.Instance != null)
            TimeController.Instance.SetSpeed(savedSpeed); // восстановить прежнюю скорость

        Debug.Log("[Garage] Уборка завершена, пропы удалены!");
    }

    private void ReturnToOffice()
    {
        if (HUDController.Instance != null)
            HUDController.Instance.EnableControls();

        if (TimeController.Instance != null)
            TimeController.Instance.SetSpeed(savedSpeed); // восстановить прежнюю скорость

        SceneManager.LoadScene("OfficeScene"); // 👉 подставь точное имя сцены офиса
    }
}
