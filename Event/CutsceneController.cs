using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class CutsceneController : MonoBehaviour
{
    [Header("UI элементы катсцены")]
    [SerializeField] private GameObject cutscenePanel;
    [SerializeField] private Image cutsceneImage;
    [SerializeField] private TMP_Text cutsceneText;

    [Header("Настройки")]
    [SerializeField] private float zoomSpeed = 0.02f; // скорость приближения
    [SerializeField] private float cutsceneTime = 5f; // длительность

    private bool playing = false;
    private float timer = 0f;
    private Action onCutsceneEnd;

    private void Update()
    {
        if (!playing) return;

        // 🔍 плавный зум картинки
        cutsceneImage.rectTransform.localScale += Vector3.one * zoomSpeed * Time.deltaTime;

        timer += Time.deltaTime;
        if (timer >= cutsceneTime)
        {
            EndCutscene();
        }
    }

    public void PlayCutscene(Sprite image, string text, Action onEnd = null)
    {
        cutscenePanel.SetActive(true);
        cutsceneImage.sprite = image;
        cutsceneText.text = text;

        cutsceneImage.rectTransform.localScale = Vector3.one;
        timer = 0f;
        playing = true;
        onCutsceneEnd = onEnd;
    }

    private void EndCutscene()
    {
        playing = false;
        cutscenePanel.SetActive(false);

        // выполняем переданное действие
        onCutsceneEnd?.Invoke();
        onCutsceneEnd = null;
    }
}
