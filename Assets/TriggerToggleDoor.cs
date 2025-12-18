using UnityEngine;
using System.Collections;

public class TriggerToggleDoor : MonoBehaviour
{
    [Header("Interaction")]
    public GameObject uiObject;
    public KeyCode interactKey = KeyCode.F;

    [Header("Door")]
    public DoorScript.Door door;

    [Header("Door Audio Volume")]
    [Range(0f, 1f)]
    public float playerDoorVolume = 1f;

    [Range(0f, 1f)]
    public float enemyDoorVolume = 0.6f;

    [Header("Enemy Auto Open")]
    public LayerMask enemyLayer;
    public float enemyOpenCooldown = 3f;

    private bool playerInside = false;
    private bool enemyCooldownActive = false;

    void Start()
    {
        if (uiObject != null)
            uiObject.SetActive(false);
    }

    void Update()
    {
        // Player manual toggle
        if (playerInside && door != null && Input.GetKeyDown(interactKey))
        {
            SetDoorVolume(playerDoorVolume);
            door.OpenDoor();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Player UI only
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            if (uiObject != null)
                uiObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            if (uiObject != null)
                uiObject.SetActive(false);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Enemy detection
        if (IsEnemy(other))
        {
            TryEnemyOpen();
        }
    }

    void TryEnemyOpen()
    {
        if (door == null)
            return;

        if (!door.open && !enemyCooldownActive)
        {
            SetDoorVolume(enemyDoorVolume);
            door.OpenDoor();
            StartCoroutine(EnemyCooldownRoutine());
        }
    }

    IEnumerator EnemyCooldownRoutine()
    {
        enemyCooldownActive = true;
        yield return new WaitForSeconds(enemyOpenCooldown);
        enemyCooldownActive = false;
    }

    void SetDoorVolume(float volume)
    {
        if (door != null && door.asource != null)
        {
            door.asource.volume = volume;
        }
    }

    bool IsEnemy(Collider other)
    {
        // check collider layer
        if ((enemyLayer.value & (1 << other.gameObject.layer)) != 0)
            return true;

        // check root (important for rigs)
        Transform root = other.transform.root;
        if (root != null && (enemyLayer.value & (1 << root.gameObject.layer)) != 0)
            return true;

        return false;
    }
}