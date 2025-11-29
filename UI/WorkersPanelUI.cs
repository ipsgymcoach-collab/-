using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class WorkersPanelUI : MonoBehaviour
{
    [Header("Вкладки")]
    [SerializeField] private Button officeTabButton;
    [SerializeField] private Button constructionTabButton;

    [Header("Контейнеры для вкладок")]
    [SerializeField] private Transform officeContent;
    [SerializeField] private Transform constructionContent;

    [Header("Префаб карточки работника")]
    [SerializeField] private GameObject workerCardPrefab;

    [Header("Окна подтверждения улучшения")]
    [SerializeField] private GameObject confirmUpgradePanel;
    [SerializeField] private TMP_Text upgradeText;
    [SerializeField] private Button upgradeYesButton;
    [SerializeField] private Button upgradeNoButton;

    [Header("Окна подтверждения увольнения")]
    [SerializeField] private GameObject confirmFirePanel;
    [SerializeField] private TMP_Text fireText;
    [SerializeField] private Button fireYesButton;
    [SerializeField] private Button fireNoButton;

    private bool showingOffice;
    private WorkerCardUI pendingCard;
    private float pendingUpgradeCost;

    // ==============================
    // ОТКРЫТИЕ / ЗАКРЫТИЕ ПАНЕЛИ
    // ==============================
    public void OpenPanel()
    {
        gameObject.SetActive(true);
        RefreshWorkers();
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    private void Start()
    {
        if (confirmUpgradePanel != null)
            confirmUpgradePanel.SetActive(false);

        if (confirmFirePanel != null)
            confirmFirePanel.SetActive(false);

        if (officeTabButton != null)
            officeTabButton.onClick.AddListener(() => SwitchTab(true));

        if (constructionTabButton != null)
            constructionTabButton.onClick.AddListener(() => SwitchTab(false));

        if (upgradeYesButton != null)
            upgradeYesButton.onClick.AddListener(ConfirmUpgradeYes);

        if (upgradeNoButton != null)
            upgradeNoButton.onClick.AddListener(() =>
            {
                if (confirmUpgradePanel != null)
                    confirmUpgradePanel.SetActive(false);
            });

        if (fireYesButton != null)
            fireYesButton.onClick.AddListener(ConfirmFireYes);

        if (fireNoButton != null)
            fireNoButton.onClick.AddListener(() =>
            {
                if (confirmFirePanel != null)
                    confirmFirePanel.SetActive(false);
            });

        SwitchTab(false); // по умолчанию открываем "Стройка"
    }

    private void OnEnable()
    {
        RefreshWorkers();
    }

    // ==============================
    // ВКЛАДКИ
    // ==============================
    private void SwitchTab(bool office)
    {
        showingOffice = office;

        if (officeContent != null && officeContent.parent != null && officeContent.parent.parent != null)
            officeContent.parent.parent.gameObject.SetActive(office);

        if (constructionContent != null && constructionContent.parent != null && constructionContent.parent.parent != null)
            constructionContent.parent.parent.gameObject.SetActive(!office);

        if (officeTabButton != null)
            officeTabButton.interactable = !office;

        if (constructionTabButton != null)
            constructionTabButton.interactable = office;

        UpdateTabVisuals();
        RefreshWorkers();
    }

    private void UpdateTabVisuals()
    {
        if (officeTabButton == null || constructionTabButton == null)
            return;

        Color active = new Color(0.7f, 0.7f, 0.7f);
        Color normal = Color.white;

        var officeColors = officeTabButton.colors;
        var constrColors = constructionTabButton.colors;

        if (showingOffice)
        {
            officeColors.normalColor = active;
            constrColors.normalColor = normal;
        }
        else
        {
            officeColors.normalColor = normal;
            constrColors.normalColor = active;
        }

        officeTabButton.colors = officeColors;
        constructionTabButton.colors = constrColors;
    }

    // ==============================
    // ОБНОВЛЕНИЕ СПИСКА РАБОТНИКОВ
    // ==============================
    public void RefreshWorkers()
    {
        var data = GameManager.Instance != null ? GameManager.Instance.Data : null;
        if (data == null)
        {
            Debug.LogError("[WorkersPanelUI] GameData не инициализирована!");
            return;
        }

        if (officeContent == null || constructionContent == null || workerCardPrefab == null)
            return;

        foreach (Transform child in officeContent)
            Destroy(child.gameObject);

        foreach (Transform child in constructionContent)
            Destroy(child.gameObject);

        foreach (var worker in data.hiredWorkers)
        {
            if (worker == null) continue;

            string category = string.IsNullOrEmpty(worker.category) ? "Стройка" : worker.category;
            Transform parent = category == "Офис" ? officeContent : constructionContent;

            GameObject cardGO = Object.Instantiate(workerCardPrefab, parent);
            var cardUI = cardGO.GetComponent<WorkerCardUI>();
            if (cardUI != null)
            {
                cardUI.Setup(this, worker);
                UpdateCardVisual(cardUI, worker);
            }
        }
    }

    private void UpdateCardVisual(WorkerCardUI card, WorkerData worker)
    {
        if (card == null || worker == null) return;

        bool busy = worker.isBusy ||
                    (BrigadeManager.Instance != null &&
                     BrigadeManager.Instance.IsWorkerInActiveBrigade(worker));

        var buttons = card.GetComponentsInChildren<Button>(true);
        var background = card.GetComponent<Image>();

        foreach (var btn in buttons)
            btn.interactable = !busy;

        if (background != null)
            background.color = busy ? new Color(0.7f, 0.7f, 0.7f) : Color.white;

        var hireBtn = buttons.FirstOrDefault(b => b.name.ToLower().Contains("hire"));
        if (hireBtn != null)
        {
            var txt = hireBtn.GetComponentInChildren<TMP_Text>();
            if (txt != null)
                txt.text = busy ? "На объекте" : "Hire";
        }
    }

    // ==============================
    // УЛУЧШЕНИЕ РАБОТНИКА
    // ==============================
    public void ShowUpgradeConfirm(WorkerCardUI card, float cost)
    {
        if (confirmFirePanel != null)
            confirmFirePanel.SetActive(false);

        pendingCard = card;
        pendingUpgradeCost = cost;

        if (confirmUpgradePanel != null)
            confirmUpgradePanel.SetActive(true);

        if (upgradeText != null)
            upgradeText.text = $"Улучшить за {cost}$?";
    }

    private void ConfirmUpgradeYes()
    {
        if (confirmUpgradePanel != null)
            confirmUpgradePanel.SetActive(false);

        pendingCard?.UpgradeConfirmed();
        RefreshWorkers();
    }

    // ==============================
    // УВОЛЬНЕНИЕ
    // ==============================
    public void ShowFireConfirm(WorkerCardUI card)
    {
        if (confirmUpgradePanel != null)
            confirmUpgradePanel.SetActive(false);

        pendingCard = card;

        var worker = card?.GetWorkerData();
        if (worker != null)
        {
            if (worker.isBusy ||
                (BrigadeManager.Instance != null &&
                 BrigadeManager.Instance.IsWorkerInActiveBrigade(worker)))
            {
                HUDController.Instance?.ShowToast("Работник сейчас занят!");
                return;
            }
        }

        if (confirmFirePanel != null)
            confirmFirePanel.SetActive(true);

        if (fireText != null && card != null)
            fireText.text = $"Уволить {card.GetWorkerName()}?";
    }

    private void ConfirmFireYes()
    {
        if (confirmFirePanel != null)
            confirmFirePanel.SetActive(false);

        pendingCard?.FireConfirmed();
        RefreshWorkers();
    }
}
