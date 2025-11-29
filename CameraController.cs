using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CameraController : MonoBehaviour
{
    [Header("Скорость движения")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float shiftMultiplier = 2f;
    [SerializeField] private float moveSmoothTime = 0.2f;

    [Header("Скорость поворота (Q/E)")]
    [SerializeField] private float rotationSpeed = 100f;

    [Header("Настройки приближения (по высоте)")]
    [SerializeField] private float zoomStep = 2f;
    [SerializeField] private float zoomSmooth = 10f;
    [SerializeField] private float minZoomHeight = 5f;
    [SerializeField] private float maxZoomHeight = 30f;

    [Header("Ограничения карты")]
    [SerializeField] private float minX = -29f;
    [SerializeField] private float maxX = 24f;
    [SerializeField] private float minZ = -20f;
    [SerializeField] private float maxZ = 30f;

    private Vector3 targetPosition;
    private Vector3 velocity = Vector3.zero;

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == "OfficeScene" &&
            GameManager.Instance != null &&
            GameManager.Instance.CurrentGame != null &&
            GameManager.Instance.CurrentGame.hasSavedCamera)
        {
            GameData data = GameManager.Instance.CurrentGame;

            targetPosition = new Vector3(data.cameraPosX, data.cameraPosY, data.cameraPosZ);
            transform.position = targetPosition;

            transform.eulerAngles = new Vector3(
                data.cameraRotX,
                data.cameraRotY,
                data.cameraRotZ
            );
        }
        else
        {
            targetPosition = transform.position;
        }
    }

    private void Update()
    {
        // 🚫 Если UI открыт — полностью блокируем управление
        if (GameManager.Instance != null && GameManager.Instance.IsUIOpen)
            return;

        HandleKeyboardMovement();
        HandleMouseDrag();
        HandleRotation();
        HandleZoom();

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, moveSmoothTime);
    }

    private void OnDisable()
    {
        SaveCameraState();
    }

    private void OnDestroy()
    {
        SaveCameraState();
    }

    private void SaveCameraState()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentGame != null)
        {
            GameData data = GameManager.Instance.CurrentGame;

            data.cameraPosX = transform.position.x;
            data.cameraPosY = transform.position.y;
            data.cameraPosZ = transform.position.z;

            data.cameraRotX = transform.eulerAngles.x;
            data.cameraRotY = transform.eulerAngles.y;
            data.cameraRotZ = transform.eulerAngles.z;

            data.hasSavedCamera = true;
        }
    }

    private void HandleKeyboardMovement()
    {
        Vector3 inputDir = Vector3.zero;

        if (Keyboard.current.wKey.isPressed) inputDir += Vector3.forward;
        if (Keyboard.current.sKey.isPressed) inputDir += Vector3.back;
        if (Keyboard.current.aKey.isPressed) inputDir += Vector3.left;
        if (Keyboard.current.dKey.isPressed) inputDir += Vector3.right;

        float multiplier = Keyboard.current.leftShiftKey.isPressed ? shiftMultiplier : 1f;
        targetPosition += Quaternion.Euler(0, transform.eulerAngles.y, 0) * inputDir * (moveSpeed * multiplier * Time.unscaledDeltaTime);

        ClampPosition();
    }

    private void HandleMouseDrag()
    {
        if (Mouse.current.middleButton.isPressed)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            Vector3 drag = new Vector3(-delta.x, 0, -delta.y) * (moveSpeed * 0.1f) * Time.unscaledDeltaTime;

            targetPosition += Quaternion.Euler(0, transform.eulerAngles.y, 0) * drag;

            ClampPosition();
        }
    }

    private void HandleRotation()
    {
        if (Keyboard.current.qKey.isPressed)
            transform.Rotate(Vector3.up, -rotationSpeed * Time.unscaledDeltaTime, Space.World);
        if (Keyboard.current.eKey.isPressed)
            transform.Rotate(Vector3.up, rotationSpeed * Time.unscaledDeltaTime, Space.World);
    }

    private void HandleZoom()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float targetHeight = Mathf.Clamp(targetPosition.y - scroll * zoomStep, minZoomHeight, maxZoomHeight);
            targetPosition.y = Mathf.Lerp(targetPosition.y, targetHeight, Time.unscaledDeltaTime * zoomSmooth);
        }
    }

    private void ClampPosition()
    {
        targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        targetPosition.z = Mathf.Clamp(targetPosition.z, minZ, maxZ);
    }
}
