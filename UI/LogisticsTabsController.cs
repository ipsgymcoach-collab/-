using UnityEngine;
using UnityEngine.UI;

public class LogisticsTabsController : MonoBehaviour
{
    [Header("Tab Buttons")]
    public Button logisticsTab;
    public Button birgaTab;
    public Button brigadesTab;
    public Button workersTab;

    [Header("Views")]
    public GameObject logisticsView;
    public GameObject birgaView;
    public GameObject brigadesView;
    public GameObject workersView;

    [Header("Close Button")]
    public Button closeButton;

    private OfficeUIController office;

    private void Start()
    {
        office = FindFirstObjectByType<OfficeUIController>();

        logisticsTab.onClick.AddListener(() => OpenTab("logistics"));
        birgaTab.onClick.AddListener(() => OpenTab("birga"));
        brigadesTab.onClick.AddListener(() => OpenTab("brigades"));
        workersTab.onClick.AddListener(() => OpenTab("workers"));

        if (closeButton != null)
            closeButton.onClick.AddListener(OnClose);
    }

    private void OnEnable()
    {
        // гарантированный сброс
        ForceReset();
    }

    public void ForceReset()
    {
        // полностью выключаем всё
        logisticsView.SetActive(false);
        birgaView.SetActive(false);
        brigadesView.SetActive(false);
        workersView.SetActive(false);

        // включаем логистику
        logisticsView.SetActive(true);
    }

    private void OpenTab(string tab)
    {
        // выключаем все вкладки
        logisticsView.SetActive(false);
        birgaView.SetActive(false);
        brigadesView.SetActive(false);
        workersView.SetActive(false);

        // включаем выбранную
        switch (tab)
        {
            case "logistics":
                logisticsView.SetActive(true);
                break;

            case "birga":
                birgaView.SetActive(true);
                break;

            case "brigades":
                brigadesView.SetActive(true);
                break;

            case "workers":
                workersView.SetActive(true);
                break;
        }
    }

    private void OnClose()
    {
        if (office != null)
            office.CloseLogistics();
    }
}
