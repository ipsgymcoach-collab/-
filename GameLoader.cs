using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameLoader : MonoBehaviour
{
    [Header("UI / Office References")]
    [SerializeField] private Image officeLogoImage;         // если лого это UI
    [SerializeField] private MeshRenderer officeLogoMesh;   // если лого на стене как 3D материал
    [SerializeField] private TMP_Text companyNameText;      // название компании в UI
    [SerializeField] private Transform heroSpawnPoint;      // точка спавна героя

    [Header("Assets")]
    [SerializeField] private Sprite[] logoSprites;
    [SerializeField] private Material[] logoMaterials;
    [SerializeField] private GameObject[] heroPrefabs;

    private void Start()
    {
        GameData data = GameManager.Instance.CurrentGame;
        if (data == null)
        {
            Debug.LogError("[GameLoader] Нет данных о текущей игре!");
            return;
        }

        // --- Название компании ---
        if (companyNameText != null)
            companyNameText.text = data.companyName;

        // --- Логотип ---
        if (officeLogoImage != null && data.selectedLogoId >= 0 && data.selectedLogoId < logoSprites.Length)
            officeLogoImage.sprite = logoSprites[data.selectedLogoId];

        if (officeLogoMesh != null && data.selectedLogoId >= 0 && data.selectedLogoId < logoMaterials.Length)
            officeLogoMesh.material = logoMaterials[data.selectedLogoId];

        // --- Герой ---
        if (heroSpawnPoint != null && data.selectedHeroId >= 0 && data.selectedHeroId < heroPrefabs.Length)
        {
            Instantiate(heroPrefabs[data.selectedHeroId], heroSpawnPoint.position, heroSpawnPoint.rotation);
        }

        // --- 🔧 Обновляем HUD ---
        if (HUDController.Instance != null)
        {
            HUDController.Instance.UpdateHUD(data);
        }
    }
}
