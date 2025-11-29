using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance;

    [Header("Hero Portrait")]
    [SerializeField] private Image heroPortrait;
    [SerializeField] private Sprite[] heroSprites;

    [Header("Stats UI")]
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Slider xpSlider;

    [Header("XP UI")]
    [SerializeField] private TMP_Text xpText;
    [SerializeField] private Image xpFillImage;
    [SerializeField] private Color xpMinColor = Color.gray;
    [SerializeField] private Color xpMaxColor = new Color(0.3f, 0.5f, 0.9f);

    [Header("Date/Time UI")]
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text dateText;

    [Header("Buttons")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button speed1Button;
    [SerializeField] private Button speed2Button;
    [SerializeField] private Button speed3Button;

    // ❌ brigadeButton убираем — не используется
    // [SerializeField] private Button brigadeButton;

    // 🔔 Toast Notification (спрятан из инспектора, но логика осталась)
    private GameObject toastPanel;
    private TMP_Text toastText;
    private float toastTimer;

    private InputAction pauseAction;
    private InputAction speed1Action;
    private InputAction speed2Action;
    private InputAction speed3Action;

    public static event System.Action OnMoneyChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            string sceneName = SceneManager.GetActiveScene().name;

            // 🔧 Если стартуем в меню — HUD не нужен
            if (sceneName.Contains("Menu") ||
                sceneName.Contains("NewGame") ||
                sceneName.Contains("LoadGame") ||
                sceneName.Contains("Settings"))
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            SceneManager.activeSceneChanged += OnSceneChanged;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        string sceneName = newScene.name;

        if (sceneName.Contains("Menu") ||
            sceneName.Contains("NewGame") ||
            sceneName.Contains("LoadGame") ||
            sceneName.Contains("Settings"))
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        // Input bindings
        pauseAction = new InputAction("Pause", InputActionType.Button);
        pauseAction.AddBinding("<Keyboard>/space");
        pauseAction.performed += _ => TogglePause();
        pauseAction.Enable();

        speed1Action = new InputAction("Speed1", InputActionType.Button);
        speed1Action.AddBinding("<Keyboard>/1");
        speed1Action.performed += _ => SetSpeed(1f);
        speed1Action.Enable();

        speed2Action = new InputAction("Speed2", InputActionType.Button);
        speed2Action.AddBinding("<Keyboard>/2");
        speed2Action.performed += _ => SetSpeed(2f);
        speed2Action.Enable();

        speed3Action = new InputAction("Speed3", InputActionType.Button);
        speed3Action.AddBinding("<Keyboard>/3");
        speed3Action.performed += _ => SetSpeed(3f);
        speed3Action.Enable();

        // UI buttons
        if (pauseButton != null)
            pauseButton.onClick.AddListener(TogglePause);

        if (speed1Button != null)
            speed1Button.onClick.AddListener(() => SetSpeed(1f));

        if (speed2Button != null)
            speed2Button.onClick.AddListener(() => SetSpeed(2f));

        if (speed3Button != null)
            speed3Button.onClick.AddListener(() => SetSpeed(3f));
    }

    private void OnDisable()
    {
        pauseAction?.Disable();
        speed1Action?.Disable();
        speed2Action?.Disable();
        speed3Action?.Disable();
    }

    private void Update()
    {
        RefreshDateTimeUI();
        UpdateSpeedButtonColors();

        // авто-скрытие уведомления (логика старая, не трогаем)
        if (toastPanel != null && toastPanel.activeSelf)
        {
            toastTimer -= Time.deltaTime;
            if (toastTimer <= 0)
                toastPanel.SetActive(false);
        }
    }

    // 📌 Обновление даты и времени
    public void RefreshDateTimeUI()
    {
        if (TimeController.Instance != null)
        {
            if (timeText != null)
                timeText.text = TimeController.Instance.GetTimeString();

            if (dateText != null)
                dateText.text = TimeController.Instance.GetDateString();
        }
    }

    private void TogglePause()
    {
        if (TimeController.Instance != null)
            TimeController.Instance.SetPause();
    }

    private void SetSpeed(float speed)
    {
        if (TimeController.Instance != null)
            TimeController.Instance.SetSpeed(speed);
    }

    public void UpdateSpeedButtonColors()
    {
        if (TimeController.Instance == null) return;

        float currentSpeed = TimeController.Instance.GameSpeed;

        if (speed1Button != null)
            speed1Button.image.color = currentSpeed == 1f ? Color.green : Color.white;

        if (speed2Button != null)
            speed2Button.image.color = currentSpeed == 2f ? Color.green : Color.white;

        if (speed3Button != null)
            speed3Button.image.color = currentSpeed == 3f ? Color.green : Color.white;

        if (pauseButton != null)
            pauseButton.image.color = currentSpeed == 0f ? Color.red : Color.white;
    }

    public void DisableControls()
    {
        pauseAction?.Disable();
        speed1Action?.Disable();
        speed2Action?.Disable();
        speed3Action?.Disable();
    }

    public void EnableControls()
    {
        pauseAction?.Enable();
        speed1Action?.Enable();
        speed2Action?.Enable();
        speed3Action?.Enable();
    }

    /// <summary>
    /// 🔧 Обновление всего HUD на основе данных игры
    /// </summary>
    public void UpdateHUD(GameData data)
    {
        if (data == null) return;

        // Деньги
        if (moneyText != null)
            moneyText.text = $"{data.money:n0}$";
        OnMoneyChanged?.Invoke();

        // Уровень
        if (levelText != null)
            levelText.text = $"Ур. {data.level}";

        // XP
        if (xpSlider != null)
        {
            xpSlider.maxValue = 100;
            xpSlider.value = data.xp;
        }

        if (xpText != null)
            xpText.text = $"{data.xp}/100";

        if (xpFillImage != null)
            xpFillImage.color = Color.Lerp(xpMinColor, xpMaxColor, data.xp / 100f);

        // Портрет героя
        if (heroPortrait != null &&
            data.selectedHeroId >= 0 &&
            data.selectedHeroId < heroSprites.Length)
        {
            heroPortrait.sprite = heroSprites[data.selectedHeroId];
        }
    }

    // 🔔 Простое уведомление (Toast) — логика как раньше, просто поля спрятаны
    public void ShowToast(string message, float duration = 3f)
    {
        if (toastPanel == null || toastText == null)
        {
            Debug.LogWarning("⚠ HUDController.ShowToast: Toast UI не назначен в инспекторе!");
            return;
        }

        toastText.text = message;
        toastPanel.SetActive(true);
        toastTimer = duration;
    }

    // 🔹 Быстрое обновление только денег без полного RefreshHUD
    public void UpdateMoney(int newAmount)
    {
        if (moneyText != null)
            moneyText.text = $"{newAmount:n0}$";

        OnMoneyChanged?.Invoke();
    }
}
