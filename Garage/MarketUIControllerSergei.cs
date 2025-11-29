using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;

public class MarketUIControllerSergei : MonoBehaviour
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
    [SerializeField] private Button marketButton;

    [Header("Окна")]
    [SerializeField] private GameObject jobsPanel;
    [SerializeField] private Button jobsBackButton;
    [SerializeField] private GameObject marketPanel;

    [Header("Кнопка «Назад» (ТОЛЬКО для MarketSergei UI)")]
    [SerializeField] private Button backToMainButton;

    [Header("Диалоговые реплики (вариант 1)")]
    [TextArea(2, 5)]
    [SerializeField] private string[] dialogueLines;

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

        // сразу ищем GarageMenuController
        garageController = FindFirstObjectByType<GarageMenuController>();
    }

    private void OnEnable() => nextLineAction.Enable();
    private void OnDisable() => nextLineAction.Disable();

    public void StartMarketEvent()
    {
        if (TimeController.Instance != null)
            savedSpeed = TimeController.Instance.GameSpeed;

        if (TimeController.Instance != null)
            TimeController.Instance.SetPause();

        if (HUDController.Instance != null)
            HUDController.Instance.DisableControls();

        dialoguePanel.SetActive(true);
        choicePanel.SetActive(false);
        jobsPanel.SetActive(false);
        marketPanel.SetActive(false);

        if (backToMainButton != null)
        {
            backToMainButton.gameObject.SetActive(false);
            backToMainButton.onClick.RemoveAllListeners();
            backToMainButton.onClick.AddListener(OnBackToMain);
        }

        dialogueText.text = "Привет! Это Сергей, добро пожаловать.\n\n<color=grey>Нажмите любую клавишу...</color>";
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
            dialogueText.text = dialogueLines[currentLine] + "\n\n<color=grey>Нажмите любую клавишу...</color>";
        }
        else
        {
            StartCoroutine(ReturnToMenuAfterDelay());
        }
    }

    public void ShowChoicePanel()
    {
        choicePanel.SetActive(true);

        if (backToMainButton != null)
            backToMainButton.gameObject.SetActive(true);

        dialogueButton.onClick.RemoveAllListeners();
        dialogueButton.onClick.AddListener(StartDialogueSequence);

        jobsButton.onClick.RemoveAllListeners();
        jobsButton.onClick.AddListener(OpenJobs);

        marketButton.onClick.RemoveAllListeners();
        marketButton.onClick.AddListener(OpenMarketPanel);
    }

    private void StartDialogueSequence()
    {
        choicePanel.SetActive(false);
        dialoguePanel.SetActive(true);
        dialogueActive = true;
        currentLine = 0;

        if (backToMainButton != null)
            backToMainButton.gameObject.SetActive(false);

        dialogueText.text = dialogueLines[currentLine] + "\n\n<color=grey>Нажмите любую клавишу...</color>";
    }

    private void OpenJobs()
    {
        choicePanel.SetActive(false);
        jobsPanel.SetActive(true);

        if (backToMainButton != null)
            backToMainButton.gameObject.SetActive(false);

        jobsBackButton.onClick.RemoveAllListeners();
        jobsBackButton.onClick.AddListener(() =>
        {
            jobsPanel.SetActive(false);
            ShowChoicePanel();
        });
    }

    private void OpenMarketPanel()
    {
        choicePanel.SetActive(false);
        marketPanel.SetActive(true);

        if (backToMainButton != null)
            backToMainButton.gameObject.SetActive(false);

        if (HUDController.Instance != null)
            HUDController.Instance.DisableControls();

        ShopUIController shop = marketPanel.GetComponent<ShopUIController>();
        if (shop != null)
            shop.OpenShop(this);
    }

    private IEnumerator ReturnToMenuAfterDelay()
    {
        dialogueActive = false;
        dialogueText.text = dialogueLines[dialogueLines.Length - 1];
        yield return new WaitForSeconds(2f);

        dialoguePanel.SetActive(false);
        ShowChoicePanel();
    }

    public void EndMarketEvent()
    {
        dialoguePanel.SetActive(false);
        choicePanel.SetActive(false);
        jobsPanel.SetActive(false);
        marketPanel.SetActive(false);

        if (backToMainButton != null)
            backToMainButton.gameObject.SetActive(false);

        if (HUDController.Instance != null)
            HUDController.Instance.EnableControls();

        if (TimeController.Instance != null)
            TimeController.Instance.SetSpeed(savedSpeed);
    }

    private void OnBackToMain()
    {
        EndMarketEvent();
        ReturnToDefaultCamera();
    }

    // 🔙 Теперь возврат камеры идёт через GarageMenuController
    public void ReturnToDefaultCamera()
    {
        if (garageController != null)
        {
            garageController.ReturnCameraToDefault();
            Debug.Log("[MarketUIControllerSergei] Камера возвращена через GarageMenuController.");
        }
        else if (mainCamera != null && defaultView != null)
        {
            mainCamera.transform.position = defaultView.position;
            mainCamera.transform.rotation = defaultView.rotation;
            Debug.Log("[MarketUIControllerSergei] Камера возвращена напрямую (GarageMenuController не найден).");
        }

        if (HUDController.Instance != null)
            HUDController.Instance.EnableControls();

        if (TimeController.Instance != null)
            TimeController.Instance.SetSpeed(savedSpeed);

        FindFirstObjectByType<GarageMenuController>()?.ReturnCameraToDefault();
    }
}
