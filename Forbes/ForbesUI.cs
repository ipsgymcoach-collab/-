using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class ForbesUI : MonoBehaviour
{
    [Header("UI элементы")]
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private TMP_Text playerPositionText;
    [SerializeField] private Button closeButton;

    private List<GameObject> spawnedEntries = new List<GameObject>();

    private void OnEnable()
    {
        RefreshList();
    }

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    public void RefreshList()
    {
        if (ForbesManager.Instance == null)
        {
            Debug.LogError("❌ ForbesManager не найден в сцене!");
            return;
        }

        // === 🔹 Обновляем рейтинг игрока перед обновлением UI ===
        var data = GameManager.Instance.Data;
        bool debtCleared = data.currentDebt <= 0;

        ForbesManager.Instance.UpdatePlayerPosition(
            data.money,
            data.GetOwnedVehiclesCount(),
            data.GetWorkerCount(),
            data.homeLevel,
            data.playerLevel,
            debtCleared
        );

        var companies = ForbesManager.Instance.companies;

        // === Очистка старых записей ===
        foreach (var obj in spawnedEntries)
            Destroy(obj);
        spawnedEntries.Clear();

        // === Пересортировка и генерация UI ===
        ForbesManager.Instance.SortCompanies();

        foreach (var company in companies)
        {
            GameObject entry = Instantiate(entryPrefab, contentParent);
            var ui = entry.GetComponent<ForbesEntryUI>();
            if (ui != null)
                ui.Setup(company);
            spawnedEntries.Add(entry);
        }

        UpdatePlayerPosition();
    }

    private void UpdatePlayerPosition()
    {
        var player = ForbesManager.Instance.playerCompany;

        if (player == null)
        {
            playerPositionText.text = "❌ Ваша компания пока не входит в рейтинг Forbes-100";
        }
        else
        {
            playerPositionText.text =
                $"🏗 <b>{player.name}</b>\n" +
                $"Место: #{player.rank}\n" +
                $"Состояние: {player.netWorth:F1} млн $";
        }
    }
}
