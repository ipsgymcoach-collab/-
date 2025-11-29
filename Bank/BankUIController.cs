using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;
using System;

public class BankUIController : MonoBehaviour
{
    [Header("Tabs (Pages)")]
    [SerializeField] private GameObject debtPage;
    [SerializeField] private GameObject creditPage;
    [SerializeField] private GameObject reportPage;

    [Header("Debt Page")]
    [SerializeField] private TMP_Text nextPaymentText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text progressLabel;
    [SerializeField] private TMP_InputField extraPayInput;
    [SerializeField] private Button extraPayButton;
    [SerializeField] private TMP_Text extraPayMessage;

    [Header("Credit Page")]
    [SerializeField] private Button loan1Button;
    [SerializeField] private Button loan2Button;
    [SerializeField] private Button loan3Button;
    [SerializeField] private TMP_Text loanMessage1;
    [SerializeField] private TMP_Text loanMessage2;
    [SerializeField] private TMP_Text loanMessage3;

    [Header("Report Page (values only)")]
    [SerializeField] private TMP_Text salaryValue;
    [SerializeField] private TMP_Text billsValue;
    [SerializeField] private TMP_Text repairsValue;
    [SerializeField] private TMP_Text loanPaymentsValue;
    [SerializeField] private TMP_Text debtPaymentsValue;
    [SerializeField] private TMP_Text profitSmallValue;
    [SerializeField] private TMP_Text profitMediumValue;
    [SerializeField] private TMP_Text profitLargeValue;
    [SerializeField] private TMP_Text profitSpecialValue;
    [SerializeField] private TMP_Text totalValue;

    [Header("Close")]
    [SerializeField] private Button closeButton;

    [SerializeField] private GameObject reportYearPage;

    private GameData data;
    private InputAction escAction;

    private void Awake()
    {
        escAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/escape");
        escAction.performed += OnEscPerformed;
    }

    private void OnEnable()
    {
        escAction?.Enable();
        UIManager.RegisterPanel(gameObject);
    }

    private void OnDisable()
    {
        escAction?.Disable();
        UIManager.UnregisterPanel(gameObject);
    }

    private void OnDestroy()
    {
        if (escAction != null)
        {
            escAction.performed -= OnEscPerformed;
            escAction.Dispose();
            escAction = null;
        }
    }

    private void Start()
    {
        data = GameManager.Instance.CurrentGame;

        if (extraPayButton) extraPayButton.onClick.AddListener(OnExtraPayment);
        if (closeButton) closeButton.onClick.AddListener(ClosePanel);

        ShowDebtPage();
        RefreshUI();
        ClearMessages();
    }

    private void Update() => RefreshUI();

    // ====== Открытие банка ======
    public void OpenBank()
    {
        gameObject.SetActive(true);
        ShowDebtPage();
        RefreshUI();

        if (TimeController.Instance != null)
            TimeController.Instance.SetPause(true);

        GameManager.Instance.IsUIOpen = true;
        UIManager.RegisterPanel(gameObject);

        var pauseMenu = UnityEngine.Object.FindFirstObjectByType<PauseMenuManager>();
        if (pauseMenu != null) pauseMenu.enabled = false;

        var cam = UnityEngine.Object.FindFirstObjectByType<CameraController>();
        if (cam != null) cam.enabled = false;

        if (HUDController.Instance != null)
            HUDController.Instance.enabled = false;

        ClearMessages();
    }

    // ====== Закрытие банка ======
    public void ClosePanel()
    {
        gameObject.SetActive(false);

        if (TimeController.Instance != null)
            TimeController.Instance.SetPause(false);

        GameManager.Instance.IsUIOpen = false;
        UIManager.UnregisterPanel(gameObject);

        var pauseMenu = UnityEngine.Object.FindFirstObjectByType<PauseMenuManager>();
        if (pauseMenu != null) pauseMenu.enabled = true;

        var cam = UnityEngine.Object.FindFirstObjectByType<CameraController>();
        if (cam != null) cam.enabled = true;

        if (HUDController.Instance != null)
            HUDController.Instance.enabled = true;

        ClearMessages();
    }

    private void OnEscPerformed(InputAction.CallbackContext ctx)
    {
        if (gameObject.activeInHierarchy)
            ClosePanel();
    }

    // ===== Вкладки =====
    public void ShowDebtPage()
    {
        if (debtPage) debtPage.SetActive(true);
        if (creditPage) creditPage.SetActive(false);
        if (reportPage) reportPage.SetActive(false);
    }

    public void ShowCreditPage()
    {
        if (debtPage) debtPage.SetActive(false);
        if (creditPage) creditPage.SetActive(true);
        if (reportPage) reportPage.SetActive(false);
    }

    public void ShowReportPage()
    {

        if (debtPage) debtPage.SetActive(false);
        if (creditPage) creditPage.SetActive(false);
        if (reportPage) reportPage.SetActive(true);
    }

    // ===== UI обновление =====
    private void RefreshUI()
    {
        if (data == null) return;

        // --- Дата следующего платежа ---
        if (nextPaymentText)
        {
            if (DateTime.TryParse(data.nextDebtPaymentDate, out DateTime parsedDate))
            {
                bool ddmm = data.isDateFormatDDMM;
                string formatted = ddmm
                    ? parsedDate.ToString("dd/MM/yyyy")
                    : parsedDate.ToString("MM/dd/yyyy");

                nextPaymentText.text = $"Следующий платёж: {formatted}";
            }
            else
            {
                nextPaymentText.text = $"Следующий платёж: {data.nextDebtPaymentDate}";
            }
        }

        if (progressLabel)
            progressLabel.text = $"{data.totalDebtPaid:N0} / {data.startingDebt:N0} $";

        if (progressBar)
        {
            progressBar.maxValue = data.startingDebt;
            progressBar.value = data.startingDebt - data.currentDebt;
        }

        if (progressText)
        {
            if (data.totalDebtPaid >= data.startingDebt)
            {
                progressText.text = "Долг погашен!";
                var fill = progressBar.fillRect.GetComponent<Image>();
                if (fill) fill.color = Color.green;
                if (extraPayButton) extraPayButton.interactable = false;
                if (extraPayInput) extraPayInput.interactable = false;
            }
            else
            {
                float percent = (data.startingDebt > 0)
                    ? (data.totalDebtPaid / (float)data.startingDebt) * 100f
                    : 0f;

                progressText.text = $"{percent:F1}% выплачено";
                var fill = progressBar.fillRect.GetComponent<Image>();
                if (fill) fill.color = Color.red;
                if (extraPayButton) extraPayButton.interactable = true;
                if (extraPayInput) extraPayInput.interactable = true;
            }
        }

        // Кредиты
        RefreshLoanButton(loan1Button, 1, 10000, 1583, loanMessage1);
        RefreshLoanButton(loan2Button, 2, 50000, 6245, loanMessage2);
        RefreshLoanButton(loan3Button, 3, 150000, 15912, loanMessage3);

        // === Отчёт ===
        if (salaryValue) salaryValue.text = data.yearlySalaryExpenses > 0 ? $"-{data.yearlySalaryExpenses:N0}$" : "0$";
        if (billsValue) billsValue.text = data.yearlyBills > 0 ? $"-{data.yearlyBills:N0}$" : "0$";
        if (repairsValue) repairsValue.text = data.yearlyRepairs > 0 ? $"-{data.yearlyRepairs:N0}$" : "0$";
        if (loanPaymentsValue) loanPaymentsValue.text = data.yearlyLoanPayments > 0 ? $"-{data.yearlyLoanPayments:N0}$" : "0$";
        if (debtPaymentsValue) debtPaymentsValue.text = data.yearlyDebtPayments > 0 ? $"-{data.yearlyDebtPayments:N0}$" : "0$";

        if (profitSmallValue) profitSmallValue.text = $"{data.yearlyProfitSmall:N0}$";
        if (profitMediumValue) profitMediumValue.text = $"{data.yearlyProfitMedium:N0}$";
        if (profitLargeValue) profitLargeValue.text = $"{data.yearlyProfitLarge:N0}$";
        if (profitSpecialValue) profitSpecialValue.text = $"{data.yearlyProfitSpecial:N0}$";

        // Итог
        int total = 0;
        total -= data.yearlySalaryExpenses;
        total -= data.yearlyBills;
        total -= data.yearlyRepairs;
        total -= data.yearlyLoanPayments;
        total -= data.yearlyDebtPayments;
        total += data.yearlyProfitSmall;
        total += data.yearlyProfitMedium;
        total += data.yearlyProfitLarge;
        total += data.yearlyProfitSpecial;

        if (totalValue) totalValue.text = $"{total:N0}$";
    }

    private void RefreshLoanButton(Button button, int loanId, int baseAmount, int monthlyPayment, TMP_Text msgField)
    {
        if (button == null) return;

        var txt = button.GetComponentInChildren<TMP_Text>();
        var loan = data.activeLoans.Find(l => l.loanId == loanId && l.isActive);

        button.onClick.RemoveAllListeners();

        if (loanId == 2 && data.level < 4)
        {
            if (txt) txt.text = "Доступно с 4 уровня";
            button.interactable = false;
            return;
        }
        if (loanId == 3 && data.level < 6)
        {
            if (txt) txt.text = "Доступно с 6 уровня";
            button.interactable = false;
            return;
        }

        button.interactable = true;

        if (loan == null)
        {
            if (txt) txt.text = $"Взять {baseAmount:N0}$";
            button.onClick.AddListener(() =>
            {
                bool ok = BankManager.Instance.TakeLoan(loanId, baseAmount, monthlyPayment,
                    new System.DateTime(data.year, data.month, data.day));
                if (!ok) StartCoroutine(ShowMessage(msgField, "Не хватает средств"));
                RefreshUI();
            });
        }
        else
        {
            if (txt) txt.text = $"Выплатить (осталось {loan.remainingAmount:N0}$)";
            button.onClick.AddListener(() =>
            {
                if (data.money >= loan.remainingAmount)
                {
                    BankManager.Instance.ExtraLoanPayment(loanId, loan.remainingAmount);
                }
                else
                {
                    StartCoroutine(ShowMessage(msgField, "Не хватает средств"));
                }
                RefreshUI();
            });
        }
    }

    private void OnExtraPayment()
    {
        if (extraPayInput == null) return;

        string rawInput = extraPayInput.text.Trim().Replace(" ", "");
        if (int.TryParse(rawInput, out int amount) && amount > 0)
        {
            if (data.money >= amount)
            {
                BankManager.Instance.ExtraDebtPayment(amount);
            }
            else
            {
                StartCoroutine(ShowMessage(extraPayMessage, "Не хватает средств"));
            }

            extraPayInput.text = "";
            RefreshUI();
        }
        else
        {
            StartCoroutine(ShowMessage(extraPayMessage, "Некорректная сумма"));
        }
    }

    private IEnumerator ShowMessage(TMP_Text field, string text)
    {
        if (field == null) yield break;

        field.text = text;
        yield return new WaitForSecondsRealtime(2f);
        field.text = "";
    }

    private void ClearMessages()
    {
        if (extraPayMessage) extraPayMessage.text = "";
        if (loanMessage1) loanMessage1.text = "";
        if (loanMessage2) loanMessage2.text = "";
        if (loanMessage3) loanMessage3.text = "";
    }
}
