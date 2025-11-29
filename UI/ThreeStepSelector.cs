using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// Трёхпозиционный слайдер (лево — центр — право)
/// ✅ Работает с замком как прозрачным оверлеем поверх смайла
/// ✅ Не требует ручных дочерних объектов в иерархии
/// ✅ Добавлен метод ResetToMiddle() для сброса в центр
/// </summary>
public class ThreeStepSelector : MonoBehaviour
{
    public event Action<int> OnValueChanged;

    [Header("Позиции выбора (лево, центр, право)")]
    [SerializeField] private RectTransform[] positions;
    [SerializeField] private Image[] optionImages;   // 0 = левый, 1 = центр, 2 = правый
    [SerializeField] private Image handle;
    [SerializeField] private Image fillLine;

    [Header("Анимация")]
    [SerializeField, Range(0.1f, 2f)] private float moveDuration = 0.35f;
    [SerializeField] private AnimationCurve moveEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Цвета (Tint)")]
    [SerializeField] private Color inactiveColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color negativeColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private Color neutralColor = new Color(1f, 1f, 1f);
    [SerializeField] private Color positiveColor = new Color(0.3f, 1f, 0.3f);

    [Header("🔒 Настройки замка")]
    [SerializeField] private Sprite lockedSprite;
    [SerializeField, Range(0f, 1f)] private float lockAlpha = 0.6f;

    private Image rightLockOverlay;
    private Image leftLockOverlay;

    private bool rightLocked = false;
    private bool leftLocked = false;

    private int currentIndex = 1;
    private Coroutine moveRoutine;
    private RectTransform handleRect;
    private RectTransform fillRect;

    private void Awake()
    {
        if (handle) handleRect = handle.rectTransform;
        if (fillLine) fillRect = fillLine.rectTransform;

        // создаём оверлеи при старте
        CreateLockOverlay(ref rightLockOverlay, 2);
        CreateLockOverlay(ref leftLockOverlay, 0);
    }

    private void Start()
    {
        AddButtonEvent(0, () => SetIndex(0));
        AddButtonEvent(1, () => SetIndex(1));
        AddButtonEvent(2, () => SetIndex(2));

        SetIndexInstant(1);
        UpdateVisuals();

        // ✅ Проверяем через кадр, если замок уже активен логически — отрисовываем его визуально
        StartCoroutine(DelayedLockVisualSync());
    }

    private IEnumerator DelayedLockVisualSync()
    {
        yield return null; // ждём 1 кадр

        if (rightLocked && rightLockOverlay != null)
            rightLockOverlay.gameObject.SetActive(true);

        if (leftLocked && leftLockOverlay != null)
            leftLockOverlay.gameObject.SetActive(true);
    }

    private void CreateLockOverlay(ref Image overlay, int optionIndex)
    {
        if (optionImages == null || optionImages.Length <= optionIndex || optionImages[optionIndex] == null)
            return;

        var target = optionImages[optionIndex];

        GameObject lockObj = new GameObject($"LockOverlay_{optionIndex}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lockObj.transform.SetParent(target.transform, false);

        overlay = lockObj.GetComponent<Image>();
        overlay.sprite = lockedSprite;
        overlay.color = new Color(1f, 1f, 1f, lockAlpha);
        overlay.preserveAspect = true;
        overlay.raycastTarget = false;
        overlay.gameObject.SetActive(false);

        var rect = overlay.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(45f, 45f);
        rect.anchoredPosition = Vector2.zero;
    }

    private void AddButtonEvent(int index, Action callback)
    {
        if (optionImages == null || optionImages.Length <= index || optionImages[index] == null) return;

        var btn = optionImages[index].GetComponent<Button>();
        if (btn == null) btn = optionImages[index].gameObject.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => callback());
    }

    // === Основная логика ===
    public void SetIndex(int index)
    {
        if (index < 0 || index > 2 || index == currentIndex) return;

        if (rightLocked && index == 2)
        {
            Debug.Log("🔒 Правая сторона заблокирована");
            return;
        }
        if (leftLocked && index == 0)
        {
            Debug.Log("🔒 Левая сторона заблокирована");
            return;
        }

        currentIndex = index;
        UpdateVisuals();

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveHandle(positions[index]));

        OnValueChanged?.Invoke(currentIndex);
    }

    public void SetIndexInstant(int index)
    {
        if (index < 0 || index > 2) return;

        currentIndex = index;
        if (positions != null && positions.Length > index && handle != null)
            handle.rectTransform.position = positions[index].position;

        UpdateVisuals();
    }

    private IEnumerator MoveHandle(RectTransform target)
    {
        if (handleRect == null || target == null) yield break;

        Vector3 start = handleRect.position;
        Vector3 end = target.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / moveDuration;
            handleRect.position = Vector3.Lerp(start, end, moveEase.Evaluate(t));
            yield return null;
        }

        handleRect.position = end;
        moveRoutine = null;
    }

    private void UpdateVisuals()
    {
        if (optionImages == null) return;

        for (int i = 0; i < optionImages.Length; i++)
        {
            if (optionImages[i] != null)
                optionImages[i].color = (i == currentIndex) ? activeColor : inactiveColor;
        }

        if (fillLine != null)
        {
            Color c = neutralColor;
            if (currentIndex == 0) c = negativeColor;
            else if (currentIndex == 2) c = positiveColor;
            fillLine.color = c;
        }
    }

    // === Блокировки ===
    public void SetRightLocked(bool locked)
    {
        rightLocked = locked;
        if (rightLockOverlay != null)
            rightLockOverlay.gameObject.SetActive(locked);

        if (optionImages != null && optionImages.Length > 2 && optionImages[2] != null)
        {
            var btn = optionImages[2].GetComponent<Button>() ?? optionImages[2].gameObject.AddComponent<Button>();
            btn.interactable = !locked;
        }

        if (locked && currentIndex == 2)
            SetIndexInstant(1);
    }

    public void SetLeftLocked(bool locked)
    {
        leftLocked = locked;
        if (leftLockOverlay != null)
            leftLockOverlay.gameObject.SetActive(locked);

        if (optionImages != null && optionImages.Length > 0 && optionImages[0] != null)
        {
            var btn = optionImages[0].GetComponent<Button>() ?? optionImages[0].gameObject.AddComponent<Button>();
            btn.interactable = !locked;
        }

        if (locked && currentIndex == 0)
            SetIndexInstant(1);
    }

    // ✅ Новый метод для удобного сброса
    public void ResetToMiddle()
    {
        SetIndexInstant(1);
    }

    public int CurrentIndex => currentIndex;
}
