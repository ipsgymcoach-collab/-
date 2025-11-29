using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OrderInfo
{
    [Header("📋 Основная информация")]
    public string id;
    public string address;
    [TextArea(2, 10)] public string description;

    [Header("💰 Характеристики заказа")]
    public int payment;
    public int duration;
    [Range(1, 6)] public int difficulty = 1;

    [Header("👷 Требуемые рабочие")]
    [Tooltip("workerId должен совпадать с ID из базы работников, например: p06_laborer, p03_carpenter, p02_electrician")]
    public List<RequiredWorker> requiredWorkers = new List<RequiredWorker>();

    [Header("🚚 Требуемая техника")]
    [Tooltip("vehicleId должен совпадать с ID техники в VehicleDatabase")]
    public List<RequiredVehicle> requiredVehicles = new List<RequiredVehicle>();

    [Header("🧱 Требуемые материалы")]
    [Tooltip("materialId должен совпадать с ID ресурса в ResourceDatabase")]
    public List<RequiredMaterial> requiredMaterials = new List<RequiredMaterial>();
}
