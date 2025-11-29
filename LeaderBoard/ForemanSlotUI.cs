using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Linq;

public class ForemanSlotUI : MonoBehaviour
{
    [Header("UI элементов")]
    [SerializeField] private Image portrait;            // фото
    [SerializeField] private TMP_Text hireText;         // текст "Нанять..."
    [SerializeField] private TMP_Text nameText;         // имя бригадира
    [SerializeField] private GameObject lockOverlay;    // затемнение при блокировке
    [SerializeField] private TMP_Text lockText;         // "Недоступно (ур. X)"

    [Header("Увольнение")]
    [SerializeField] private Button fireButton;         // кнопка "Уволить" (крестик)
    [SerializeField] private GameObject confirmPanel;   // окно подтверждения
    [SerializeField] private TMP_Text confirmText;      // текст подтверждения
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;

    private Button button;
    private bool isUnlocked;
    private ForemanData assignedForeman;

    public event Action<ForemanSlotUI> OnHireClicked;

    // 👉 Требуемый уровень для данного слота
    public int RequiredLevel { get; private set; }

    // 🔹 Публичный доступ для LeadershipBoardUI
    public Button FireButton => fireButton;

    public void SetRequiredLevel(int level)
    {
        RequiredLevel = level;
    }

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(OnClick);

        if (fireButton != null)
            fireButton.onClick.AddListener(OnFireClicked);

        if (confirmYesButton != null)
            confirmYesButton.onClick.AddListener(ConfirmFire);

        if (confirmNoButton != null)
            confirmNoButton.onClick.AddListener(() => { if (confirmPanel) confirmPanel.SetActive(false); });

        if (confirmPanel != null)
            confirmPanel.SetActive(false);
    }

    private void OnEnable()
    {
        // При показе слота сразу синхронизируем доступность крестика
        RefreshFireLockState();
    }

    /// <summary>
    /// Глобальная проверка занятости бригадира и блокировка крестика.
    /// </summary>
    public void RefreshFireLockState()
    {
        bool busy = IsAssignedForemanBusy();
        if (fireButton != null)
            fireButton.interactable = !busy;
    }

    /// <summary>
    /// Проверка — занят ли назначенный бригадир в активной бригаде
    /// </summary>
    private bool IsAssignedForemanBusy()
    {
        if (assignedForeman == null)
            return false;

        var data = GameManager.Instance?.Data;
        if (data == null)
            return false;

        // Проверяем все бригады, где этот бригадир руководит активной стройкой
        foreach (var brigade in data.allBrigades)
        {
            if (brigade == null) continue;
            if (brigade.foremanId == assignedForeman.id && brigade.isWorking)
                return true;
        }

        return false;
    }

    public void UpdateSlot(int playerLevel, int requiredLevel)
    {
        SetRequiredLevel(requiredLevel);
        isUnlocked = playerLevel >= requiredLevel;

        if (!isUnlocked)
        {
            if (lockOverlay) lockOverlay.SetActive(true);
            if (lockText) lockText.text = $"🔒 Недоступно (ур. {requiredLevel})";
            if (button) button.interactable = false;
            if (portrait) portrait.sprite = null;
            if (hireText) hireText.gameObject.SetActive(false);
            if (nameText) nameText.gameObject.SetActive(false);
            if (fireButton) fireButton.gameObject.SetActive(false);
        }
        else
        {
            if (lockOverlay) lockOverlay.SetActive(false);
            if (button) button.interactable = true;

            if (assignedForeman != null)
            {
                if (portrait) portrait.sprite = Resources.Load<Sprite>($"Icon/{assignedForeman.iconId}");
                if (hireText) hireText.gameObject.SetActive(false);
                if (nameText) { nameText.text = assignedForeman.name; nameText.gameObject.SetActive(true); }
                if (fireButton) fireButton.gameObject.SetActive(true);
            }
            else
            {
                if (portrait) portrait.sprite = null;
                if (hireText) hireText.gameObject.SetActive(true);
                if (nameText) nameText.gameObject.SetActive(false);
                if (fireButton) fireButton.gameObject.SetActive(false);
            }
        }

        // После любого обновления синхронизируем доступность крестика
        RefreshFireLockState();
    }

    private void OnClick()
    {
        if (!isUnlocked) return;
        if (assignedForeman == null)
            StartCoroutine(DelayedHireInvoke());
    }

    private IEnumerator DelayedHireInvoke()
    {
        yield return new WaitForSeconds(0.05f);
        OnHireClicked?.Invoke(this);
    }

    public void AssignForeman(ForemanData foreman)
    {
        assignedForeman = foreman;
        if (portrait) portrait.sprite = Resources.Load<Sprite>($"Icon/{foreman.iconId}");
        if (hireText) hireText.gameObject.SetActive(false);
        if (nameText) { nameText.text = foreman.name; nameText.gameObject.SetActive(true); }
        if (fireButton) fireButton.gameObject.SetActive(true);

        RefreshFireLockState();
    }

    private void OnFireClicked()
    {
        if (assignedForeman == null) return;

        // 🔒 Защита — если бригадир занят, не показываем Confirm
        if (IsAssignedForemanBusy())
        {
            HUDController.Instance?.ShowToast($"❌ {assignedForeman.name} сейчас работает на объекте!");
            return;
        }

        if (confirmPanel != null)
        {
            if (confirmText != null)
                confirmText.text = $"Уволить {assignedForeman.name}?\nОн будет недоступен 7 дней.";
            confirmPanel.SetActive(true);
        }
    }

    private void ConfirmFire()
    {
        if (assignedForeman == null) return;

        // Повторная защита на случай гонок событий
        if (IsAssignedForemanBusy())
        {
            HUDController.Instance?.ShowToast($"❌ {assignedForeman.name} сейчас работает на объекте!");
            if (confirmPanel) confirmPanel.SetActive(false);
            return;
        }

        assignedForeman.isFired = true;
        assignedForeman.isHired = false;
        assignedForeman.rehireAvailableDay = TimeController.Instance.day + 7;

        Debug.Log($"🔥 Уволен бригадир: {assignedForeman.name}. Доступен после дня {assignedForeman.rehireAvailableDay}.");

        assignedForeman = null;

        if (confirmPanel) confirmPanel.SetActive(false);
        if (fireButton) fireButton.gameObject.SetActive(false);
        if (hireText) hireText.gameObject.SetActive(true);
        if (nameText) nameText.gameObject.SetActive(false);
        if (portrait) portrait.sprite = null;
    }
}
