using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

public class MonthlyPopupUI : MonoBehaviour
{
    public static MonthlyPopupUI Instance;

    [Header("UI ссылки")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform panelTransform;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Image background;
    [SerializeField] private Button closeButton;

    private bool isVisible;
    private Coroutine animationRoutine;

    private void Awake()
    {
        Instance = this;
        if (closeButton != null)
            closeButton.onClick.AddListener(HidePopup);

        canvasGroup.alpha = 0f;
        panelTransform.anchoredPosition = new Vector2(0, -200);
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Показывает уведомление с заданным цветом и временем жизни.
    /// </summary>
    public void ShowMessage(string message, Color bgColor, float showSeconds)
    {
        // 🟡 Проверка — если игрок отключил уведомления, ничего не показываем
        if (GameManager.Instance != null &&
            GameManager.Instance.Data != null &&
            !GameManager.Instance.Data.notificationsEnabled)
        {
            Debug.Log($"🔕 Уведомления выключены, пропущено сообщение: {message}");
            return;
        }

        if (messageText == null || background == null || canvasGroup == null || panelTransform == null)
        {
            Debug.LogError("[MonthlyPopupUI] Не все ссылки назначены в инспекторе!");
            return;
        }
        messageText.text = message;
        background.color = bgColor;

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(ShowAnimation(showSeconds));
    }

    private IEnumerator ShowAnimation(float showSeconds)
    {
        isVisible = true;
        float duration = 0.6f;
        float t = 0f;
        Vector2 start = new Vector2(0, -200);
        Vector2 end = new Vector2(0, 80);

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0, 1, t / duration);
            panelTransform.anchoredPosition = Vector2.Lerp(start, end, p);
            canvasGroup.alpha = p;
            yield return null;
        }

        canvasGroup.alpha = 1f;
        panelTransform.anchoredPosition = end;

        // 🕒 ждём 3 игровых часа
        float realSeconds = 5f;
        float timer = realSeconds;

        while (timer > 0 && isVisible)
        {
            timer -= Time.unscaledDeltaTime;
            yield return null;
        }


        if (isVisible)
            HidePopup();
    }



    public void HidePopup()
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);
        animationRoutine = StartCoroutine(HideAnimation());
    }

    private IEnumerator HideAnimation()
    {
        isVisible = false;
        float duration = 0.5f;
        float t = 0f;

        Vector2 start = panelTransform.anchoredPosition;
        Vector2 end = new Vector2(0, -200);

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0, 1, t / duration);
            panelTransform.anchoredPosition = Vector2.Lerp(start, end, p);
            canvasGroup.alpha = 1f - p;
            yield return null;
        }

        canvasGroup.alpha = 0f;
        panelTransform.anchoredPosition = end;
    }

    private void Update()
    {
        if (!isVisible) return;
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            HidePopup();
    }
}
