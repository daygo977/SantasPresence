using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;

    [Header("Camera")]
    public MonoBehaviour cameraController;

    public bool IsLocked { get; private set; }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        IsLocked = false;
    }

    public void LockGame()
    {
        if (IsLocked) return;

        IsLocked = true;
        Time.timeScale = 0f;

        if (cameraController != null)
            cameraController.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void UnlockGame()
    {
        IsLocked = false;
        Time.timeScale = 1f;

        if (cameraController != null)
            cameraController.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
