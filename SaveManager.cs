using System.IO;
using UnityEngine;

public static class SaveManager
{
    private static string GetSavePath(int slot)
    {
        // 🔒 Никогда не пишем в слот < 0 — фиксим «save-1.json»
        if (slot < 0) slot = 0;
        return Path.Combine(Application.persistentDataPath, $"save{slot}.json");
    }

    /// <summary>
    /// Сохранить игру. Предпочитает data, а если она null — берёт CurrentGame.
    /// Всегда сохраняет в валидный слот (>=0).
    /// </summary>
    public static void SaveGame(GameData data, int slot)
    {
        if (slot < 0) slot = 0;

        // ✅ Явно берём тот объект, который передали; если его нет — берём CurrentGame
        GameData source = data ?? (GameManager.Instance != null ? GameManager.Instance.CurrentGame : null);
        if (source == null)
        {
            Debug.LogError("[SaveManager] Нет данных для сохранения (data == null и CurrentGame == null)");
            return;
        }

        // Страховочные правки
        if (source.level <= 0) source.level = 1;
        if (source.xp < 0) source.xp = 0;

        // Сохраняем таймер/скорость и т.п. в GameData
        if (TimeController.Instance != null)
            TimeController.Instance.SaveToGameData(source);

        source.lastSaveTime = System.DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        string path = GetSavePath(slot);

        try
        {
            string json = JsonUtility.ToJson(source, true);
            File.WriteAllText(path, json);
            Debug.Log($"[SaveManager] Сохранено в слот {slot}: {source.companyName} ({source.lastSaveTime}) → {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Ошибка записи в {path}: {e}");
        }

        // 🧷 На всякий: синхронизируем ссылку в менеджере
        if (GameManager.Instance != null && GameManager.Instance.CurrentGame != source)
            GameManager.Instance.SetCurrentGame(source, slot);
    }

    public static GameData LoadGame(int slot)
    {
        if (slot < 0) slot = 0;

        string path = GetSavePath(slot);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SaveManager] Слот {slot} пустой ({path})");
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            GameData loaded = JsonUtility.FromJson<GameData>(json);

            if (loaded.level <= 0) loaded.level = 1;
            if (loaded.xp < 0) loaded.xp = 0;

            Debug.Log($"[SaveManager] Загружено из слота {slot}: {loaded?.companyName}, Lvl {loaded.level}, XP {loaded.xp}");
            return loaded;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Ошибка чтения {path}: {e}");
            return null;
        }
    }

    public static GameData PeekSave(int slot)
    {
        if (slot < 0) slot = 0;

        string path = GetSavePath(slot);
        if (!File.Exists(path)) return null;

        try
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<GameData>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Ошибка чтения превью {path}: {e}");
            return null;
        }
    }

    public static bool HasSave(int slot)
    {
        if (slot < 0) slot = 0;
        return File.Exists(GetSavePath(slot));
    }

    public static void DeleteSave(int slot)
    {
        if (slot < 0) slot = 0;

        string path = GetSavePath(slot);
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[SaveManager] Удалён слот {slot} ({path})");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Ошибка удаления {path}: {e}");
        }
    }

    /// <summary>
    /// Удобный хоткей: сохранить текущую игру в текущий слот (или в 0, если слот ещё не выбран).
    /// </summary>
    public static void SaveCurrent()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.CurrentGame == null)
        {
            Debug.LogWarning("[SaveManager] Нет текущей игры для быстрого сохранения");
            return;
        }

        int slot = gm.CurrentSlot < 0 ? 0 : gm.CurrentSlot;
        SaveGame(gm.CurrentGame, slot);
    }
}
