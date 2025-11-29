using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "VehicleDatabase", menuName = "Construction/Vehicle Database")]
public class VehicleDatabase : ScriptableObject
{
    public static VehicleDatabase Instance { get; private set; }

    public List<VehicleCatalogEntry> allVehicles = new List<VehicleCatalogEntry>();

    private void OnEnable()
    {
        Instance = this;
    }

    public VehicleCatalogEntry GetByModelId(string id)
    {
        return allVehicles.Find(v => v.modelId == id);
    }

    public string GetVehicleNameById(string id)
    {
        var vehicle = allVehicles.Find(v => v.modelId == id);
        return vehicle != null ? vehicle.name : id;
    }
}


