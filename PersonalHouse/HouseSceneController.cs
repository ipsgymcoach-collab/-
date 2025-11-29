using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HouseSceneController : MonoBehaviour
{
    [Header("🎥 Камера и пролет")]
    [SerializeField] private Camera mainCam;
    [SerializeField] private Transform[] cameraPoints;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 3f;
    [SerializeField] private float rotationSmoothness = 2f;

    [Header("🌗 Эффект появления сцены")]
    [SerializeField] private Image overlayFade;
    [SerializeField] private float fadeDuration = 2.5f;

    [Header("🧭 Управление")]
    [SerializeField] private GameObject skipHint;

    [Header("💰 Панель покупки (с плавным появлением)")]
    [SerializeField] private CanvasGroup buyPanelGroup;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button buyLaterButton; // 🆕 "Купить позже"
    [SerializeField] private TMP_Text priceText;

    private int currentIndex = 0;
    private bool isFlying = true;
    private bool canSkip = false;
    private bool alreadyVisited = false;

    private int housePrice = 2500000;

    private void Start()
    {
        // === Камера ===
        if (mainCam == null)
        {
            mainCam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        }
        if (mainCam == null)
        {
            Debug.LogError("[HouseSceneController] ❌ Камера не найдена!");
            enabled = false;
            return;
        }

        // === UI подготовка ===
        PrepareOverlay();
        PrepareBuyPanel();

        skipHint?.SetActive(false);
        alreadyVisited = PlayerPrefs.GetInt("HouseSceneVisited", 0) == 1;

        // === Если дом уже куплен — сразу пропускаем ===
        if (GameManager.Instance.Data.hasOwnHouse)
        {
            SkipFly();
            return;
        }

        // === Начальная позиция ===
        if (cameraPoints.Length > 0)
        {
            mainCam.transform.position = cameraPoints[0].position;
            mainCam.transform.rotation = cameraPoints[0].rotation;
        }

        // === Деньги ===
        int money = GameManager.Instance.Data.money;
        buyButton.interactable = money >= housePrice;
        priceText.text = $"Купить дом за ${housePrice:N0}?";

        buyButton.onClick.AddListener(OnBuyClicked);
        if (buyLaterButton != null)
            buyLaterButton.onClick.AddListener(OnBuyLaterClicked);

        if (alreadyVisited)
        {
            canSkip = true;
            skipHint?.SetActive(true);
        }
        else
        {
            Invoke(nameof(EnableSkip), 2.5f);
        }

        // ⚡ Камера движется сразу, fade начинается чуть позже
        Invoke(nameof(BeginFadeIn), 0.1f);
    }

    private void PrepareOverlay()
    {
        if (overlayFade == null) return;

        var rt = overlayFade.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var c = overlayFade.color;
        c.r = 0f; c.g = 0f; c.b = 0f; c.a = 1f;
        overlayFade.color = c;
        overlayFade.gameObject.SetActive(true);
        overlayFade.raycastTarget = false; // не блокирует клики
        overlayFade.enabled = true;
        overlayFade.transform.SetAsLastSibling();
    }

    private void PrepareBuyPanel()
    {
        if (buyPanelGroup == null) return;
        buyPanelGroup.alpha = 0f;
        buyPanelGroup.gameObject.SetActive(false);
    }

    private void BeginFadeIn()
    {
        StartCoroutine(FadeInScene());
    }

    private IEnumerator FadeInScene()
    {
        if (overlayFade == null) yield break;

        float timer = 0f;
        Color c = overlayFade.color;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fadeDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            c.a = Mathf.Lerp(1f, 0f, smoothT);
            overlayFade.color = c;
            yield return null;
        }

        // 🔹 Полная прозрачность и отключение
        c.a = 0f;
        overlayFade.color = c;
        overlayFade.enabled = false;
        overlayFade.raycastTarget = false;
        overlayFade.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isFlying) return;

        FlyCamera();

        if (canSkip && Keyboard.current.anyKey.wasPressedThisFrame)
            SkipFly();
    }

    private void FlyCamera()
    {
        if (currentIndex >= cameraPoints.Length) return;

        Transform target = cameraPoints[currentIndex];
        Transform cam = mainCam.transform;

        cam.position = Vector3.MoveTowards(cam.position, target.position, moveSpeed * Time.deltaTime);

        float dist = Vector3.Distance(cam.position, target.position);
        float dynamicRotateSpeed = rotateSpeed * (1f + dist / rotationSmoothness);

        cam.rotation = Quaternion.Slerp(cam.rotation, target.rotation, Time.deltaTime * dynamicRotateSpeed);

        if (dist < 0.3f)
        {
            currentIndex++;

            // ✅ при достижении 4-й точки
            if (currentIndex == 4)
            {
                isFlying = false;

                // Скрываем текст "нажмите любую кнопку"
                if (skipHint != null)
                    skipHint.SetActive(false);

                // Панель сразу появляется
                ShowBuyPanelSmooth();
                return;
            }
        }
    }

    private void EnableSkip()
    {
        canSkip = true;
        skipHint?.SetActive(true);
    }

    private void SkipFly()
    {
        if (cameraPoints == null || cameraPoints.Length == 0) return;

        Transform last = cameraPoints[Mathf.Min(3, cameraPoints.Length - 1)];
        mainCam.transform.position = last.position;
        mainCam.transform.rotation = last.rotation;

        isFlying = false;
        skipHint?.SetActive(false);

        if (!GameManager.Instance.Data.hasOwnHouse)
            ShowBuyPanelSmooth();
        else
            PlayerPrefs.SetInt("HouseSceneVisited", 1);
    }

    private void ShowBuyPanelSmooth()
    {
        StopAllCoroutines(); // чтобы не пересекалось с fade-in
        StartCoroutine(FadeInPanel());
    }

    private IEnumerator FadeInPanel()
    {
        if (buyPanelGroup == null) yield break;

        buyPanelGroup.gameObject.SetActive(true);
        buyPanelGroup.alpha = 0f;

        float timer = 0f;
        const float duration = 0.45f; // быстро, но плавно

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / duration);
            buyPanelGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        buyPanelGroup.alpha = 1f;
        PlayerPrefs.SetInt("HouseSceneVisited", 1);
    }

    private void OnBuyClicked()
    {
        if (GameManager.Instance.SpendMoney(housePrice))
        {
            GameManager.Instance.Data.hasOwnHouse = true;
            int slot = GameManager.Instance.CurrentSlot >= 0 ? GameManager.Instance.CurrentSlot : 0;
            SaveManager.SaveGame(GameManager.Instance.CurrentGame, slot);

            HUDController.Instance?.ShowToast("🏡 Дом успешно куплен!");
            SceneManager.LoadScene("OfficeScene");
        }
        else
        {
            buyButton.interactable = false;
        }
    }

    private void OnBuyLaterClicked()
    {
        // 🏃‍♂️ Вернуться в офис без покупки
        SceneManager.LoadScene("OfficeScene");
    }

}
