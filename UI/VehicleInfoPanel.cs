using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class VehicleInfoPanel : MonoBehaviour
{
    [Header("Основные UI элементы")]
    [SerializeField] private TMP_Text vehicleNameText;
    [SerializeField] private TMP_Text vehicleDescriptionText;
    [SerializeField] private Image vehicleImage;
    [SerializeField] private TMP_Text priceText;

    [Header("Кнопки")]
    [SerializeField] private Button buyButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;

    [Header("Уведомление (Toast)")]
    [SerializeField] private CanvasGroup toastPanel;
    [SerializeField] private TMP_Text toastText;
    [SerializeField] private float toastDuration = 2.5f;
    [SerializeField] private float toastFadeSpeed = 4f;

    private VehicleData currentVehicle;
    private ShopUIController shop;
    private List<Sprite> gallery = new List<Sprite>();
    private int currentImageIndex = 0;
    private Coroutine toastRoutine;

    // 🚀 Показ окна информации
    public void Show(VehicleData vehicle, ShopUIController shopController)
    {
        currentVehicle = vehicle;
        shop = shopController;

        transform.SetAsLastSibling();
        gameObject.SetActive(true);

        if (vehicleNameText)
            vehicleNameText.text = vehicle.name;

        if (vehicleDescriptionText)
            vehicleDescriptionText.text = string.IsNullOrEmpty(vehicle.description)
                ? "Описание отсутствует."
                : vehicle.description;

        if (priceText)
            priceText.text = "$" + vehicle.price.ToString("N0");

        // 🖼️ Загружаем фото из VehicleGallery/[id]
        LoadGallery(vehicle.id);

        // 🟡 Показываем первое фото
        UpdateImage();

        // Настройка кнопок
        SetupButtons();
    }

    private void SetupButtons()
    {
        if (buyButton)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyClicked);
        }

        if (closeButton)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }

        if (leftButton)
        {
            leftButton.onClick.RemoveAllListeners();
            leftButton.onClick.AddListener(ShowPrevImage);
        }

        if (rightButton)
        {
            rightButton.onClick.RemoveAllListeners();
            rightButton.onClick.AddListener(ShowNextImage);
        }
    }

    // 🔹 Загружаем все изображения из папки Resources/VehicleGallery/[id]
    private void LoadGallery(string vehicleId)
    {
        gallery.Clear();
        Sprite[] loadedImages = Resources.LoadAll<Sprite>($"VehicleGallery/{vehicleId}");

        if (loadedImages.Length > 0)
        {
            gallery.AddRange(loadedImages);
            currentImageIndex = 0;
        }
        else
        {
            // Если нет фото — загрузим стандартную
            Sprite defaultImg = Resources.Load<Sprite>("UI/NoImage");
            if (defaultImg != null)
                gallery.Add(defaultImg);
        }
    }

    private void UpdateImage()
    {
        if (gallery.Count == 0) return;
        vehicleImage.sprite = gallery[currentImageIndex];
    }

    private void ShowNextImage()
    {
        if (gallery.Count <= 1) return;
        currentImageIndex = (currentImageIndex + 1) % gallery.Count;
        UpdateImage();
    }

    private void ShowPrevImage()
    {
        if (gallery.Count <= 1) return;
        currentImageIndex = (currentImageIndex - 1 + gallery.Count) % gallery.Count;
        UpdateImage();
    }

    private void OnBuyClicked()
    {
        if (currentVehicle == null || shop == null)
        {
            Debug.LogWarning("[VehicleInfoPanel] Нет данных для покупки!");
            return;
        }

        GameData data = GameManager.Instance.CurrentGame;
        if (data.money < currentVehicle.price)
        {
            ShowToast("Недостаточно средств!");
            return;
        }
        if (data.level < currentVehicle.unlockLevel)
        {
            ShowToast($"Нужна лицензия {currentVehicle.unlockLevel} уровня!");
            return;
        }

        data.money -= currentVehicle.price;
        data.AddVehicleById(currentVehicle.id);
        SaveManager.SaveGame(data, GameManager.Instance.CurrentSlot);
        shop.UpdatePlayerInfo();

        ShowToast("ТС доставлено по вашему адресу");
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    // === Toast ===
    private void ShowToast(string message)
    {
        if (toastPanel == null || toastText == null) return;

        if (toastRoutine != null)
            StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(ToastRoutine(message));
    }

    private IEnumerator ToastRoutine(string message)
    {
        toastPanel.gameObject.SetActive(true);
        toastPanel.alpha = 0f;
        toastText.text = message;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * toastFadeSpeed;
            toastPanel.alpha = Mathf.Lerp(0, 1, t);
            yield return null;
        }

        yield return new WaitForSeconds(toastDuration);

        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * toastFadeSpeed;
            toastPanel.alpha = Mathf.Lerp(1, 0, t);
            yield return null;
        }

        toastPanel.gameObject.SetActive(false);
    }
}
