using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WorkerListPanelUI : MonoBehaviour
{
    [Header("Основные элементы")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;

    [Header("Вкладки")]
    [SerializeField] private Button constructionTabButton;
    [SerializeField] private Button officeTabButton;

    [Header("Контенты вкладок")]
    [SerializeField] private GameObject constructionContent;
    [SerializeField] private GameObject officeContent;

    private void Start()
    {
        // Скрываем при старте
        panelRoot.SetActive(false);

        // Подписываем кнопки
        closeButton.onClick.AddListener(ClosePanel);
        constructionTabButton.onClick.AddListener(OpenConstructionTab);
        officeTabButton.onClick.AddListener(OpenOfficeTab);
    }

    // Открытие панели
    public void OpenPanel()
    {
        panelRoot.SetActive(true);
        OpenConstructionTab(); // по умолчанию показываем стройку
    }

    // Закрытие панели
    private void ClosePanel()
    {
        panelRoot.SetActive(false);
        Time.timeScale = 1f; // продолжаем игру
    }

    // Вкладки
    private void OpenConstructionTab()
    {
        constructionContent.SetActive(true);
        officeContent.SetActive(false);

        // Визуальное выделение активной кнопки (если хочешь)
        HighlightButton(constructionTabButton, true);
        HighlightButton(officeTabButton, false);
    }

    private void OpenOfficeTab()
    {
        constructionContent.SetActive(false);
        officeContent.SetActive(true);

        HighlightButton(constructionTabButton, false);
        HighlightButton(officeTabButton, true);
    }

    private void HighlightButton(Button button, bool active)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = active ? new Color(0.8f, 0.8f, 1f) : Color.white;
        button.colors = colors;
    }
}
