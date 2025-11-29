using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;

public class GarageUIControllerEddy : MonoBehaviour
{
    [Header("Навигация камеры")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform defaultView;

    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Главное меню выбора")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private Button dialogueButton;
    [SerializeField] private Button jobsButton;
    [SerializeField] private Button garageButton;

    [Header("Окна")]
    [SerializeField] private GameObject jobsPanel;
    [SerializeField] private Button jobsBackButton;

    [Header("Гараж (панель)")]
    [SerializeField] private GameObject garagePanel;
    [SerializeField] private Button garageBackButton;

    [Header("Кнопка «Назад» (ТОЛЬКО для Eddy UI)")]
    [SerializeField] private Button backToMainButton;

    [Header("Диалоговые реплики Эдди")]
    [TextArea(2, 5)][SerializeField] private string[] dialogueLines;

    private int currentLine = 0;
    private bool dialogueActive = false;
    private InputAction nextLineAction;
    private float savedSpeed = 1f;

    private void Awake()
    {
        nextLineAction = new InputAction("NextLine", binding: "<Keyboard>/anyKey");
        nextLineAction.AddBinding("<Mouse>/leftButton");
        nextLineAction.performed += _ => OnNextLine();
    }

    private void OnEnable() => nextLineAction.Enable();
    private void OnDisable() => nextLineAction.Disable();

    // === СТАРТ СОБЫТИЯ ЭДДИ ===
    public void StartEddyEvent()
    {
        if (TimeController.Instance != null)
            savedSpeed = TimeController.Instance.GameSpeed;

        TimeController.Instance?.SetPause();
        HUDController.Instance?.DisableControls();

        Debug.Log("[EddyUI] StartEddyEvent() запущен");

        dialoguePanel.SetActive(true);
        choicePanel.SetActive(false);
        jobsPanel.SetActive(false);
        if (garagePanel != null) garagePanel.SetActive(false);

        backToMainButton.gameObject.SetActive(false);
        backToMainButton.onClick.RemoveAllListeners();
        backToMainButton.onClick.AddListener(OnBackToMain);

        dialogueText.text = "Привет! Я Эдди, рад тебя видеть.\n\n<color=grey>Нажмите любую клавишу...</color>";
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
            dialogueText.text = dialogueLines[currentLine] + "\n\n<color=grey>Нажмите любую клавишу...</color>";
        else
            StartCoroutine(ReturnToMenuAfterDelay());
    }

    private void ShowChoicePanel()
    {
        Debug.Log("[EddyUI] Показано меню выбора");

        choicePanel.SetActive(true);
        backToMainButton.gameObject.SetActive(true);

        dialogueButton.onClick.RemoveAllListeners();
        dialogueButton.onClick.AddListener(StartDialogueSequence);

        jobsButton.onClick.RemoveAllListeners();
        jobsButton.onClick.AddListener(OpenJobs);

        garageButton.onClick.RemoveAllListeners();
        garageButton.onClick.AddListener(OpenGaragePanel);
    }

    private void StartDialogueSequence()
    {
        choicePanel.SetActive(false);
        dialoguePanel.SetActive(true);
        dialogueActive = true;
        currentLine = 0;

        backToMainButton.gameObject.SetActive(false);
        dialogueText.text = dialogueLines[currentLine] + "\n\n<color=grey>Нажмите любую клавишу...</color>";
    }

    private void OpenJobs()
    {
        Debug.Log("[EddyUI] Открыто окно Jobs");

        choicePanel.SetActive(false);
        jobsPanel.SetActive(true);
        backToMainButton.gameObject.SetActive(false);

        if (jobsBackButton != null)
        {
            jobsBackButton.onClick.RemoveAllListeners();
            jobsBackButton.onClick.AddListener(() =>
            {
                Debug.Log("[EddyUI] Возврат из Jobs");
                jobsPanel.SetActive(false);
                ShowChoicePanel();
            });
        }
    }

    private void OpenGaragePanel()
    {
        Debug.Log("[EddyUI] Нажата кнопка Garage");

        choicePanel.SetActive(false);

        if (garagePanel == null)
        {
            Debug.LogError("[EddyUI] garagePanel не назначен в инспекторе!");
            return;
        }

        if (!garagePanel.activeSelf)
        {
            // если Canvas родителя выключен — включаем его
            var parentCanvas = garagePanel.GetComponentInParent<Canvas>();
            if (parentCanvas != null && !parentCanvas.gameObject.activeSelf)
            {
                Debug.Log("[EddyUI] Canvas родителя выключен — включаю его вручную.");
                parentCanvas.gameObject.SetActive(true);
            }

            garagePanel.SetActive(true);
            Debug.Log("[EddyUI] garagePanel активирован!");
        }

        backToMainButton.gameObject.SetActive(false);

        if (garageBackButton != null)
        {
            garageBackButton.onClick.RemoveAllListeners();
            garageBackButton.onClick.AddListener(() =>
            {
                Debug.Log("[EddyUI] Нажат Back в GaragePanel");
                garagePanel.SetActive(false);
                ShowChoicePanel();
            });
        }
        else
        {
            Debug.LogWarning("[EddyUI] garageBackButton не назначен!");
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

    public void EndEddyEvent()
    {
        Debug.Log("[EddyUI] Завершение события Эдди");

        dialoguePanel.SetActive(false);
        choicePanel.SetActive(false);
        jobsPanel.SetActive(false);
        if (garagePanel != null) garagePanel.SetActive(false);

        backToMainButton.gameObject.SetActive(false);
        HUDController.Instance?.EnableControls();
        TimeController.Instance?.SetSpeed(savedSpeed);
        FindFirstObjectByType<GarageMenuController>()?.ReturnCameraToDefault();
    }

    private void OnBackToMain()
    {
        Debug.Log("[EddyUI] Возврат к DefaultView");
        EndEddyEvent();

        if (mainCamera && defaultView)
        {
            mainCamera.transform.position = defaultView.position;
            mainCamera.transform.rotation = defaultView.rotation;
        }
    }
}
