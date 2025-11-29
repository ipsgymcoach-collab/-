using System;
using UnityEngine;

[Serializable]
public class VehicleCatalogEntry
{
    public string modelId;          // "v001"
    public string name;
    public VehicleType type;
    public VehicleGroup group;
    public int basePrice;
    public int maintenanceCost;
    public string iconId;
}
