using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WorkerItemUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text profText;
    [SerializeField] private TMP_Text categoryText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text salaryText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text upgradeText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyButtonText;

    private WorkerData currentWorker;
    private LaborMarketUI marketUI;

    public void Setup(WorkerData data, LaborMarketUI parent)
    {
        currentWorker = data;
        marketUI = parent;

        nameText.text = $"{data.firstName} {data.lastName}";
        profText.text = data.profession;
        categoryText.text = data.category;

        // ⭐ уровень работника = skillLevel
        levelText.text = data.skillLevel.ToString();

        salaryText.text = $"{data.salary}$/мес";
        priceText.text = $"{data.hireCost}$";
        upgradeText.text = $"{data.upgradeCost}$";

        buyButton.onClick.RemoveAllListeners();

        RefreshAvailability();

        if (buyButton.interactable)
            buyButton.onClick.AddListener(OnBuy);
    }

    public void RefreshAvailability()
    {
        if (currentWorker.isHired)
        {
            buyButton.interactable = false;
            buyButtonText.text = "Нанят";
            buyButton.image.color = Color.gray;
            return;
        }

        bool professionLimit = WorkersDatabase.Instance?.IsProfessionLimited(currentWorker.profession) ?? false;
        bool categoryLimit = marketUI?.IsCategoryAtLimit(currentWorker.category) ?? false;

        if (professionLimit)
        {
            buyButton.interactable = false;
            buyButtonText.text = "Лимит профессии";
            buyButton.image.color = new Color(0.6f, 0.6f, 0.6f);
        }
        else if (categoryLimit)
        {
            buyButton.interactable = false;
            buyButtonText.text = "Лимит мест";
            buyButton.image.color = new Color(0.6f, 0.6f, 0.6f);
        }
        else
        {
            buyButton.interactable = true;
            buyButtonText.text = "Нанять";
            buyButton.image.color = Color.white;
        }
    }

    private void OnBuy()
    {
        if (currentWorker.isHired) return;

        LaborMarketManager.Instance?.HireWorker(currentWorker);
    }
}
