using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;

    [Header("Camera")]
    public MonoBehaviour cameraController;  // Camera to lock

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
        // If already locked, return
        if (IsLocked)
            return;

        // Lock game
        IsLocked = true;
        Time.timeScale = 0f;

        if (cameraController != null)
            cameraController.enabled = false; // Cannot move

        // Show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // When out of menu, unlock
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
