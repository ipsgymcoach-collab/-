using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;

public class ResourcesUIControllerGarry : MonoBehaviour
{
    [Header("Навигация камеры")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform defaultView;

    [Header("UI панели")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Меню выбора")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private Button dialogueButton;   // "Кто ты?"
    [SerializeField] private Button shopButton;       // "Заказ ресурсов"
    [SerializeField] private Button warehouseButton;  // "Склад ресурсов"

    [Header("Панели окон")]
    [SerializeField] private GameObject resourceShopPanel;  // панель магазина
    [SerializeField] private Button resourceShopBackButton; // кнопка "назад" из магазина
    [SerializeField] private GameObject warehousePanel;     // панель склада
    [SerializeField] private Button warehouseBackButton;    // кнопка "назад" из склада

    [Header("Кнопка «Назад» (ТОЛЬКО для Garry UI)")]
    [SerializeField] private Button backToMainButton;

    [SerializeField] private TMP_Text storageInfoText;


    [Header("Диалоговые реплики Гарри")]
    [TextArea(2, 5)][SerializeField] private string[] dialogueLines;

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

    // === СТАРТ СОБЫТИЯ ГАРРИ ===
    public void StartGarryEvent()
    {
        if (TimeController.Instance != null)
            savedSpeed = TimeController.Instance.GameSpeed;

        TimeController.Instance?.SetPause();
        HUDController.Instance?.DisableControls();

        Debug.Log("[GarryUI] StartGarryEvent() запущен");

        dialoguePanel.SetActive(true);
        choicePanel.SetActive(false);
        if (resourceShopPanel != null) resourceShopPanel.SetActive(false);
        if (warehousePanel != null) warehousePanel.SetActive(false);

        backToMainButton.gameObject.SetActive(false);
        backToMainButton.onClick.RemoveAllListeners();
        backToMainButton.onClick.AddListener(OnBackToMain);

        dialogueText.text = "Привет! Я Гарри. Отвечаю за материалы и склад.\n\n<color=grey>Нажмите любую клавишу...</color>";
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
        Debug.Log("[GarryUI] Показано меню выбора");

        choicePanel.SetActive(true);
        backToMainButton.gameObject.SetActive(true);

        dialogueButton.onClick.RemoveAllListeners();
        dialogueButton.onClick.AddListener(StartDialogueSequence);

        shopButton.onClick.RemoveAllListeners();
        shopButton.onClick.AddListener(OpenResourceShop);

        warehouseButton.onClick.RemoveAllListeners();
        warehouseButton.onClick.AddListener(OpenWarehousePanel);
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

    private void OpenResourceShop()
    {
        Debug.Log("[GarryUI] Открыт магазин ресурсов");

        choicePanel.SetActive(false);

        if (resourceShopPanel == null)
        {
            Debug.LogError("[GarryUI] resourceShopPanel не назначен!");
            return;
        }

        // Включаем панель магазина
        if (!resourceShopPanel.activeSelf)
        {
            var parentCanvas = resourceShopPanel.GetComponentInParent<Canvas>();
            if (parentCanvas != null && !parentCanvas.gameObject.activeSelf)
                parentCanvas.gameObject.SetActive(true);

            resourceShopPanel.SetActive(true);
        }

        backToMainButton.gameObject.SetActive(false);

        // Кнопка "Назад" из магазина
        if (resourceShopBackButton != null)
        {
            resourceShopBackButton.onClick.RemoveAllListeners();
            resourceShopBackButton.onClick.AddListener(() =>
            {
                resourceShopPanel.SetActive(false);

                // 🔥 пусть сам магазин обновится
                resourceShopPanel.GetComponent<ResourceShopUI>()?.OnShopClosed();

                // 🔥 обновляем склад у Гарри
                UpdateStorageInfo();

                ShowChoicePanel();
            });

        }

    }


    private void OpenWarehousePanel()
    {
        Debug.Log("[GarryUI] Открыт склад ресурсов");

        choicePanel.SetActive(false);

        if (warehousePanel == null)
        {
            Debug.LogError("[GarryUI] warehousePanel не назначен!");
            return;
        }

        // Включаем панель склада
        if (!warehousePanel.activeSelf)
        {
            var parentCanvas = warehousePanel.GetComponentInParent<Canvas>();
            if (parentCanvas != null && !parentCanvas.gameObject.activeSelf)
                parentCanvas.gameObject.SetActive(true);

            warehousePanel.SetActive(true);
        }

        // 🔥 ВСЕГДА обновляем информацию склада при открытии
        UpdateStorageInfo();

        // Обновляем UI склада (ресурсы в таблице)
        var warehouseUI = warehousePanel.GetComponentInChildren<WarehouseUI>();
        if (warehouseUI != null)
            warehouseUI.ForceRefresh();
        else
            Debug.LogWarning("[GarryUI] WarehouseUI не найден!");

        backToMainButton.gameObject.SetActive(false);

        if (warehouseBackButton != null)
        {
            warehouseBackButton.onClick.RemoveAllListeners();
            warehouseBackButton.onClick.AddListener(() =>
            {
                warehousePanel.SetActive(false);
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

    public void EndGarryEvent()
    {
        Debug.Log("[GarryUI] Завершение события Гарри");

        dialoguePanel.SetActive(false);
        choicePanel.SetActive(false);
        if (resourceShopPanel != null) resourceShopPanel.SetActive(false);
        if (warehousePanel != null) warehousePanel.SetActive(false);

        backToMainButton.gameObject.SetActive(false);
        HUDController.Instance?.EnableControls();
        TimeController.Instance?.SetSpeed(savedSpeed);
        FindFirstObjectByType<GarageMenuController>()?.ReturnCameraToDefault();
    }

    private void OnBackToMain()
    {
        Debug.Log("[GarryUI] Возврат к DefaultView");

        EndGarryEvent();

        if (garageController != null)
        {
            garageController.ReturnCameraToDefault();
            Debug.Log("[GarryUI] Камера возвращена через GarageMenuController");
        }
        else if (mainCamera && defaultView)
        {
            mainCamera.transform.position = defaultView.position;
            mainCamera.transform.rotation = defaultView.rotation;
            Debug.Log("[GarryUI] Камера возвращена напрямую");
        }
    }
    public void UpdateStorageInfo()
    {
        var data = GameManager.Instance?.CurrentGame;
        if (data == null || storageInfoText == null) return;

        int used = data.GetWarehouseCurrentUsed();
        int max = data.GetWarehouseCapacity();

        storageInfoText.text = $"{used} / {max}";
    }


}
