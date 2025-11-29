using UnityEngine;

public class PreviewManager : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform heroSpawnPoint;   // точка появления героя
    [SerializeField] private Transform logoSpawnPoint;   // точка появления лого

    [Header("Prefabs")]
    [SerializeField] private GameObject logoPrefabBase;  // один Quad-префаб
    [SerializeField] private GameObject[] heroPrefabs;   // массив префабов героев

    [Header("Materials (для логотипов)")]
    [SerializeField] private Material[] logoMaterials;   // сюда закидываешь 6 материалов с PNG

    private GameObject currentHero;
    private GameObject currentLogo;


    public void ShowHero(int id)
    {
        if (currentHero != null) Destroy(currentHero);

        if (id >= 0 && id < heroPrefabs.Length)
        {
            currentHero = Instantiate(heroPrefabs[id], heroSpawnPoint.position, heroSpawnPoint.rotation);
            SetLayerRecursive(currentHero, LayerMask.NameToLayer("Preview"));
            Debug.Log($"[Preview] Герой {id} заспавнен");
        }
        else
        {
            Debug.LogWarning($"[Preview] Неверный id героя: {id}");
        }
    }


    public void ShowLogo(int id)
    {
        if (currentLogo != null) Destroy(currentLogo);

        if (logoPrefabBase == null)
        {
            Debug.LogError("[Preview] logoPrefabBase не назначен!");
            return;
        }

        currentLogo = Instantiate(logoPrefabBase, logoSpawnPoint.position, logoSpawnPoint.rotation);

        var renderer = currentLogo.GetComponent<MeshRenderer>();
        if (renderer != null && id >= 0 && id < logoMaterials.Length)
        {
            renderer.material = logoMaterials[id]; // назначаем нужный материал
            Debug.Log($"[Preview] Лого {id} заспавнено, назначен материал {logoMaterials[id].name}");
        }
        else
        {
            Debug.LogWarning($"[Preview] Материал для логотипа {id} не найден");
        }

        SetLayerRecursive(currentLogo, LayerMask.NameToLayer("Preview"));
    }

    // ----- Служебное -----
    private void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }
}
