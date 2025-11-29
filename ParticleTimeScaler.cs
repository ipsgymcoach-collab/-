using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleTimeScaler : MonoBehaviour
{
    private ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        if (ps == null || TimeController.Instance == null) return;

        var main = ps.main;
        main.simulationSpeed = TimeController.Instance.GameSpeed;
    }
}
