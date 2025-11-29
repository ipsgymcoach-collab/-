using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class LeadershipBoardUI : MonoBehaviour
{
    [Header("Главная панель")]
    [SerializeField] private GameObject leadershipPanel;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    [Header("Персонажи")]
    [SerializeField] private Image playerPortrait;
    [SerializeField] private Image aiPortrait;

    [Header("NPC гаража (3 портрета)")]
    [SerializeField] private List<Image> garageNpcPortraits = new List<Image>();

    [Header("Слоты бригадиров")]
    [SerializeField] private Transform foremanContainer;
    private List<ForemanSlotUI> foremanSlots = new List<ForemanSlotUI>();

    [Header("Панель найма бригадиров")]
    [SerializeField] private ForemanHireUI hirePanel;

    private void Awake()
    {
        if (foremanContainer != null)
            foremanSlots.AddRange(foremanContainer.GetComponentsInChildren<ForemanSlotUI>(true));

        foreach (var slot in foremanSlots)
            slot.OnHireClicked += OpenHirePanel;

        if (openButton != null) openButton.onClick.AddListener(OpenPanel);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);

        if (leadershipPanel != null)
            leadershipPanel.SetActive(false);
    }

    public void OpenPanel()
    {
        if (leadershipPanel == null) return;
        leadershipPanel.SetActive(true);
        GameManager.Instance.IsUIOpen = true;
        UpdateSlots();
    }

    private void ClosePanel()
    {
        if (leadershipPanel == null) return;
        leadershipPanel.SetActive(false);
        GameManager.Instance.IsUIOpen = false;
    }

    private void OpenHirePanel(ForemanSlotUI slot)
    {
        if (hirePanel == null) return;
        StartCoroutine(OpenHireInstant(slot));
    }

    private IEnumerator OpenHireInstant(ForemanSlotUI slot)
    {
        hirePanel.gameObject.SetActive(true);
        yield return new WaitForEndOfFrame();
        hirePanel.Open(slot);
    }

    // ============================================================
    // 🔹 ОБНОВЛЕНИЕ СЛОТОВ БРИГАДИРОВ
    // ============================================================
    public void UpdateSlots()
    {
        int level = GameManager.Instance.Data.playerLevel;

        for (int i = 0; i < foremanSlots.Count; i++)
        {
            int req = RequiredLevel(i);
            foremanSlots[i].UpdateSlot(level, req);
        }

        var foremen = GameManager.Instance.Data.foremen;
        if (foremen != null && foremen.Count > 0)
        {
            foreach (var foreman in foremen)
            {
                if (!foreman.isHired) continue;

                // === ✅ Автоматически создаём бригады, если их нет ===
                if (foreman.brigades == null || foreman.brigades.Count == 0)
                {
                    var data = GameManager.Instance.Data;
                    foreman.brigades = new List<BrigadeData>();

                    int count = Mathf.Max(1, foreman.extraBrigades + 1);
                    for (int i = 1; i <= count; i++)
                    {
                        string brigadeName = $"Бригада {foreman.name} №{i}";

                        var newBrigade = new BrigadeData
                        {
                            id = $"{foreman.id}_brigade_{i}",
                            foremanId = foreman.id,
                            name = brigadeName,
                            workers = new List<WorkerData>(),
                            completedOrders = 0,
                            isWorking = false
                        };

                        foreman.brigades.Add(newBrigade);
                        data.allBrigades.Add(newBrigade);
                        Debug.Log($"✅ Создана {brigadeName} для {foreman.name}");
                    }

                    SaveManager.SaveGame(data, GameManager.Instance.CurrentSlot);

                    // 🔹 Обновляем панель бригад сразу
                    BrigadePanelUI.Instance?.RefreshBrigadeList();
                }

                var slot = foremanSlots.Find(s => s.RequiredLevel == foreman.requiredLevel);
                if (slot == null) continue;

                slot.AssignForeman(foreman);

                bool foremanBusy = IsForemanInActiveOrder(foreman.id);
                if (slot.FireButton != null)
                    slot.FireButton.interactable = !foremanBusy;

                slot.RefreshFireLockState();
            }
        }

        UpdatePortraits();
    }

    // 🔹 После найма бригадира (вызов из ForemanHireUI)
    public void OnForemanHired(ForemanData foreman)
    {
        if (foreman == null) return;

        Debug.Log($"👷 Новый бригадир нанят: {foreman.name}");
        UpdateSlots(); // пересоздаёт слоты
        BrigadePanelUI.Instance?.RefreshBrigadeList(); // обновляем список бригад
    }

    private bool IsForemanInActiveOrder(string foremanId)
    {
        var data = GameManager.Instance?.Data;
        if (data == null) return false;

        return data.allBrigades.Any(b => b.foremanId == foremanId && b.isWorking);
    }

    private int RequiredLevel(int index)
    {
        switch (index)
        {
            case 0: return 1;
            case 1: return 4;
            case 2: return 6;
            case 3: return 8;
            case 4: return 10;
            default: return 99;
        }
    }

    private void UpdatePortraits()
    {
        int heroId = GameManager.Instance.Data.selectedHeroId;
        int displayId = heroId + 1;

        string playerPath = $"Png/CA{displayId}";
        Sprite playerSprite = Resources.Load<Sprite>(playerPath);
        if (playerPortrait != null) playerPortrait.sprite = playerSprite;

        Sprite assistantSprite = Resources.Load<Sprite>("Icon/assistant");
        if (aiPortrait != null) aiPortrait.sprite = assistantSprite;

        if (garageNpcPortraits != null && garageNpcPortraits.Count >= 3)
        {
            garageNpcPortraits[0].sprite = Resources.Load<Sprite>("Icon/npc_sergey");
            garageNpcPortraits[1].sprite = Resources.Load<Sprite>("Icon/npc_eddy");
            garageNpcPortraits[2].sprite = Resources.Load<Sprite>("Icon/npc_third");
        }
    }
}
