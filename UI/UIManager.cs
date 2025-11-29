using System.Collections.Generic;
using UnityEngine;

public static class UIManager
{
    private static readonly HashSet<GameObject> activePanels = new HashSet<GameObject>();

    public static void RegisterPanel(GameObject panel)
    {
        if (panel != null)
            activePanels.Add(panel);
    }

    public static void UnregisterPanel(GameObject panel)
    {
        if (panel != null)
            activePanels.Remove(panel);
    }

    public static bool IsAnyPanelOpen()
    {
        // 🟢 чистим null-ссылки на случай, если сцена сменилась
        activePanels.RemoveWhere(panel => panel == null);
        return activePanels.Count > 0;
    }

    // 🟢 новый метод — полный сброс всех панелей (вызывается при выходе из игры)
    public static void ResetPanels()
    {
        activePanels.Clear();
    }
}
