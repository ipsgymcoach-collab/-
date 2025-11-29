using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class WorkerCardUI : MonoBehaviour
{
    [Header("Тексты")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text professionText;
    [SerializeField] private TMP_Text levelText;

    [Header("Кнопки")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button fireButton;

    [Header("Фон карточки")]
    [SerializeField] private Image background;

    private WorkerData workerData;
    private WorkersPanelUI panelUI;

    private const int MAX_LEVEL = 5;

    private Color normalColor = new Color(1f, 1f, 1f, 1f);
    private Color grayColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);

    public void Setup(WorkersPanelUI parentPanel, WorkerData data)
    {
        workerData = data;
        panelUI = parentPanel;

        nameText.text = $"{data.firstName} {data.lastName}";
        professionText.text = CleanProfession(data.profession);
        levelText.text = $"Ур. {data.skillLevel}";

        upgradeButton.onClick.RemoveAllListeners();
        fireButton.onClick.RemoveAllListeners();

        upgradeButton.onClick.AddListener(OnUpgradePressed);
        fireButton.onClick.AddListener(OnFirePressed);

        if (workerData.skillLevel >= MAX_LEVEL)
        {
            upgradeButton.interactable = false;
            levelText.text = "Ур. MAX";
        }

        bool isInActiveBrigade =
            BrigadeManager.Instance != null &&
            BrigadeManager.Instance.IsWorkerInActiveBrigade(workerData);

        SetFireButtonInteractable(!isInActiveBrigade);
        SetGray(isInActiveBrigade);
    }

    private void OnUpgradePressed()
    {
        if (workerData.skillLevel >= MAX_LEVEL)
        {
            upgradeButton.interactable = false;
            return;
        }

        float cost = workerData.upgradeCost;
        panelUI.ShowUpgradeConfirm(this, cost);
    }

    public void UpgradeConfirmed()
    {
        if (workerData == null) return;

        workerData.skillLevel++;
        workerData.salary += 200;

        if (workerData.skillLevel >= MAX_LEVEL)
        {
            workerData.skillLevel = MAX_LEVEL;
            upgradeButton.interactable = false;
            levelText.text = "Ур. MAX";
        }
        else
        {
            levelText.text = $"Ур. {workerData.skillLevel}";
        }
    }

    private void OnFirePressed()
    {
        var brigActive = BrigadeManager.Instance?
            .IsWorkerInActiveBrigade(workerData) ?? false;

        if (brigActive)
        {
            HUDController.Instance?.ShowToast("❌ Этот рабочий задействован в активной бригаде!");
            return;
        }

        panelUI.ShowFireConfirm(this);
    }

    public void FireConfirmed()
    {
        var data = GameManager.Instance.Data;
        if (data == null || workerData == null) return;

        var brigActive =
            BrigadeManager.Instance?.IsWorkerInActiveBrigade(workerData) ?? false;

        if (brigActive)
        {
            HUDController.Instance?.ShowToast("❌ Нельзя уволить — рабочий в бригаде!");
            return;
        }

        if (data.hiredWorkers.Contains(workerData))
            data.hiredWorkers.Remove(workerData);

        workerData.isHired = false;
        workerData.recentlyFired = true;
        workerData.restDaysLeft = 7;
        workerData.isBusy = false;

        if (!WorkersDatabase.Instance.workers.Contains(workerData))
            WorkersDatabase.Instance.workers.Add(workerData);

        Destroy(gameObject);
        panelUI.RefreshWorkers();

        HUDController.Instance?.ShowToast($"🔥 {workerData.firstName} уволен.");
    }

    public void SetFireButtonInteractable(bool state)
    {
        fireButton.interactable = state;
    }

    public void SetGray(bool state)
    {
        background.color = state ? grayColor : normalColor;
    }

    private string CleanProfession(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return raw;

        var parts = raw.Split(' ');

        if (parts.Length >= 2 && int.TryParse(parts[1], out _))
            return parts[0];

        return raw;
    }


    public WorkerData GetWorkerData() => workerData;
    public string GetWorkerName() => $"{workerData.firstName} {workerData.lastName}";
}
