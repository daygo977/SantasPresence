using UnityEngine;

[RequireComponent(typeof(Collider))]
public class escapeTrigger : MonoBehaviour
{
    private bool isActive = false;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        gameObject.SetActive(false);
    }

    public void EnableEscape()
    {
        isActive = true;
        gameObject.SetActive(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive)
            return;

        if (other.CompareTag("Player"))
        {
            GameManager.Instance.WinGame(); // If the escape trigger reads a player inside of the collider, instantiate a win condition through the gamemanager
        }
    }
}
