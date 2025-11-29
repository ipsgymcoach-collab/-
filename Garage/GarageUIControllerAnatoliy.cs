using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;

public class GarageUIControllerAnatoliy : MonoBehaviour
{
    [Header("Навигация камеры")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform defaultView;

    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Главное меню выбора")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private Button dialogueButton;   // О себе
    [SerializeField] private Button upgradeButton;    // Улучшения

    [Header("Панель улучшений")]
    [SerializeField] private GameObject AnatoliyPanel;
    [SerializeField] private Button upgradeBackButton;

    [Header("Кнопка «Назад»")]
    [SerializeField] private Button backToMainButton;

    [Header("Диалоговые реплики Анатолия")]
    [TextArea(2, 5)]
    [SerializeField]
    private string[] dialogueLines =
    {
        "Тест"
    };

    private int currentLine = 0;
    private bool dialogueActive = false;
    private InputAction nextLineAction;
    private float savedSpeed = 1f;

    private GarageMenuController garageController;

    private void Awake()
    {
        nextLineAction = new InputAction("NextLine", binding: "<Keyboard>/anyKey");
        nextLineAction.AddBinding("<Mouse>/leftButton");
        nextLineAction.performed += _ => OnNextLine();

        garageController = FindFirstObjectByType<GarageMenuController>();
    }

    private void OnEnable() => nextLineAction.Enable();
    private void OnDisable() => nextLineAction.Disable();

    // === Старт события ===
    public void StartAnatoliyEvent()
    {
        if (TimeController.Instance != null)
            savedSpeed = TimeController.Instance.GameSpeed;

        TimeController.Instance?.SetPause();
        HUDController.Instance?.DisableControls();

        dialoguePanel.SetActive(true);
        choicePanel.SetActive(false);
        AnatoliyPanel.SetActive(false);

        backToMainButton.gameObject.SetActive(false);
        backToMainButton.onClick.RemoveAllListeners();
        backToMainButton.onClick.AddListener(OnBackToMain);

        dialogueText.text = "Доброго дня!\n\n<color=grey>Нажмите любую клавишу...</color>";
        dialogueActive = true;
        currentLine = -1;
    }

    private void OnNextLine()
    {
        if (!dialogueActive) return;

        if (currentLine == -1)
        {
            dialogueActive = false;
            dialoguePanel.SetActive(false);
            ShowChoicePanel();
            return;
        }

        currentLine++;
        if (currentLine < dialogueLines.Length)
        {
            dialogueText.text = dialogueLines[currentLine] +
                                "\n\n<color=grey>Нажмите любую клавишу...</color>";
        }
        else
        {
            StartCoroutine(ReturnToMenuAfterDelay());
        }
    }

    private void ShowChoicePanel()
    {
        choicePanel.SetActive(true);
        backToMainButton.gameObject.SetActive(true);

        dialogueButton.onClick.RemoveAllListeners();
        dialogueButton.onClick.AddListener(StartDialogueSequence);

        upgradeButton.onClick.RemoveAllListeners();
        upgradeButton.onClick.AddListener(OpenUpgradePanel);
    }

    private void StartDialogueSequence()
    {
        choicePanel.SetActive(false);
        dialoguePanel.SetActive(true);
        dialogueActive = true;
        currentLine = 0;

        backToMainButton.gameObject.SetActive(false);

        dialogueText.text = dialogueLines[currentLine] +
                            "\n\n<color=grey>Нажмите любую клавишу...</color>";
    }

    // === ПАНЕЛЬ УЛУЧШЕНИЙ ===
    private void OpenUpgradePanel()
    {
        choicePanel.SetActive(false);
        AnatoliyPanel.SetActive(true);
        backToMainButton.gameObject.SetActive(false);

        if (upgradeBackButton != null)
        {
            upgradeBackButton.onClick.RemoveAllListeners();
            upgradeBackButton.onClick.AddListener(() =>
            {
                AnatoliyPanel.SetActive(false);
                ShowChoicePanel();
            });
        }
    }

    private IEnumerator ReturnToMenuAfterDelay()
    {
        dialogueActive = false;
        dialogueText.text = dialogueLines[^1];
        yield return new WaitForSeconds(2f);

        dialoguePanel.SetActive(false);
        ShowChoicePanel();
    }

    private void OnBackToMain()
    {
        EndAnatoliyEvent();
        ReturnToDefaultCamera();
    }

    public void EndAnatoliyEvent()
    {
        dialoguePanel.SetActive(false);
        choicePanel.SetActive(false);
        AnatoliyPanel.SetActive(false);

        backToMainButton.gameObject.SetActive(false);

        HUDController.Instance?.EnableControls();
        TimeController.Instance?.SetSpeed(savedSpeed);
    }

    public void ReturnToDefaultCamera()
    {
        if (garageController != null)
        {
            garageController.ReturnCameraToDefault();
        }
        else if (mainCamera && defaultView)
        {
            mainCamera.transform.position = defaultView.position;
            mainCamera.transform.rotation = defaultView.rotation;
        }

        HUDController.Instance?.EnableControls();
        TimeController.Instance?.SetSpeed(savedSpeed);
        FindFirstObjectByType<GarageMenuController>()?.ReturnCameraToDefault();
    }
}
