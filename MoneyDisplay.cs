using UnityEngine;
using TMPro;

public class MoneyDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text moneyText;

    private void OnEnable()
    {
        UpdateMoney(); // обновляем сразу при появлении
        HUDController.OnMoneyChanged += UpdateMoney; // подписываемся на изменения
    }

    private void OnDisable()
    {
        HUDController.OnMoneyChanged -= UpdateMoney; // убираем подписку
    }

    // 🔥 Делаем публичным, чтобы можно было вызвать из GaragePanelController
    public void UpdateMoney()
    {
        var data = GameManager.Instance?.CurrentGame;
        if (data != null)
            moneyText.text = $"{data.money:n0}$"; // формат: 1 000 000$
    }
}
