using System.Linq;
using UnityEngine;

public class BrigadeManager : MonoBehaviour
{
    public static BrigadeManager Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // === АКТИВИРОВАТЬ БРИГАДУ ===
    public void ActivateBrigade(string brigadeId, string orderId)
    {
        var data = GameManager.Instance?.Data;
        if (data == null) return;

        var targetBrigade = data.allBrigades.FirstOrDefault(b => b.id == brigadeId);
        if (targetBrigade == null)
        {
            Debug.LogWarning($"[BrigadeManager] ❌ Бригада не найдена: {brigadeId}");
            return;
        }

        targetBrigade.isWorking = true;
        targetBrigade.currentOrderId = orderId;

        // 🔹 Помечаем всех работников как занятых
        foreach (var w in targetBrigade.workers)
            w.isBusy = true;

        SaveManager.SaveGame(data, GameManager.Instance.CurrentSlot);
        Debug.Log($"🏗️ Активирована бригада {targetBrigade.name} для заказа {orderId}");
    }

    // === ДЕАКТИВИРОВАТЬ БРИГАДУ ===
    public void DeactivateBrigade(string brigadeId)
    {
        var data = GameManager.Instance?.Data;
        if (data == null) return;

        var brigade = data.allBrigades.FirstOrDefault(b => b.id == brigadeId);
        if (brigade == null)
        {
            Debug.LogWarning($"[BrigadeManager] ❌ Бригада не найдена: {brigadeId}");
            return;
        }

        brigade.isWorking = false;
        brigade.currentOrderId = "";

        // 🔹 Освобождаем всех работников
        foreach (var w in brigade.workers)
            w.isBusy = false;

        Debug.Log($"✅ Бригада {brigade.name} завершила заказ и освободила работников.");
    }

    // === ПРОВЕРКА: РАБОЧИЙ ЗАДЕЙСТВОВАН В АКТИВНОЙ БРИГАДЕ ===
    public bool IsWorkerInActiveBrigade(WorkerData worker)
    {
        if (worker == null) return false;
        var data = GameManager.Instance?.Data;
        if (data == null) return false;

        // 🔹 Проверяем по ID, а не по ссылке
        return data.allBrigades.Any(b =>
            b.isWorking &&
            b.workers.Any(w => w.id == worker.id)
        );
    }

    // === ПРОВЕРКА: БРИГАДИР ЗАНЯТ ===
    public bool IsForemanActive(string foremanId)
    {
        var data = GameManager.Instance?.Data;
        if (data == null) return false;

        var foreman = data.foremen.FirstOrDefault(f => f.id == foremanId);
        if (foreman == null) return false;

        return foreman.brigades.Any(b => b.isWorking);
    }
}
