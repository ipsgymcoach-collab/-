using UnityEngine;
using System.Collections;

public class ForbesUpdater : MonoBehaviour
{
    [SerializeField] private float updateInterval = 30f; // каждые 30 секунд

    private void Start()
    {
        StartCoroutine(AutoUpdate());
    }

    private IEnumerator AutoUpdate()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);

            // 🔁 Обновляем данные рейтинга Forbes
            ForbesManager.Instance.UpdateCompanyValues();

            // 🧮 Обновляем позицию игрока
            var data = GameManager.Instance.Data;
            bool debtCleared = data.currentDebt <= 0;

            ForbesManager.Instance.UpdatePlayerPosition(
                data.money,
                data.GetOwnedVehiclesCount(),  // 🚗 количество техники
                data.GetWorkerCount(),         // 👷 количество работников
                data.homeLevel,                // 🏠 уровень дома
                data.playerLevel,              // 🎯 уровень игрока
                debtCleared
            );
        }
    }
}
