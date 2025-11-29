using UnityEngine;

public class UIResetter : MonoBehaviour
{
    void LateUpdate()
    {
        UIManager.ResetPanels(); // правильно
    }
}
