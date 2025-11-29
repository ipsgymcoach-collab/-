using UnityEngine;

public class LaborMarketUIController : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject laborMarketPanel;  // панель с таблицей работников

    public static LaborMarketUIController Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Открыть панель Биржи труда.
    /// </summary>
    public void OpenLaborMarket()
    {
        if (laborMarketPanel == null)
        {
            Debug.LogError("[LaborMarketUIController] Панель LaborMarketPanel не назначена!");
            return;
        }

        laborMarketPanel.SetActive(true);
        Debug.Log("[LaborMarketUIController] Панель Биржи труда открыта.");
    }

    /// <summary>
    /// Закрыть панель.
    /// </summary>
    public void CloseLaborMarket()
    {
        if (laborMarketPanel == null) return;

        laborMarketPanel.SetActive(false);
        Debug.Log("[LaborMarketUIController] Панель Биржи труда закрыта.");
    }
}
