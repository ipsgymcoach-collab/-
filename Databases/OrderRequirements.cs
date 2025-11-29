using System;
using System.Collections.Generic;

[Serializable]
public class RequiredWorker
{
    public string workerId;   // 🧱 ID профессии (например "p01_carpenter")
    public int count;         // 👷 Сколько работников нужно
}

[Serializable]
public class RequiredVehicle
{
    public string vehicleId;  // 🚜 ID техники
    public int count;         // Количество
}

[Serializable]
public class RequiredMaterial
{
    public string materialId; // 🧱 ID ресурса/материала
    public int count;         // Количество
}