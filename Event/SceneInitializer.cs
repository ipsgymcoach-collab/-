using UnityEngine;

public class SceneInitializer : MonoBehaviour
{
    private void Start()
    {
        if (GameManager.Instance?.CurrentGame != null && HUDController.Instance != null)
        {
            Debug.Log("[SceneInitializer] Обновляем HUD из CurrentGame");
            HUDController.Instance.UpdateHUD(GameManager.Instance.CurrentGame);
        }
    }
}
