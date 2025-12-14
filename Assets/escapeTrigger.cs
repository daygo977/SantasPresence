using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EscapeTrigger : MonoBehaviour
{
    private bool isActive = false;

    private void Awake()
    {
        // ensure trigger collider
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        gameObject.SetActive(false); // start disabled
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
            GameManager.Instance.SendMessage("WinGame", SendMessageOptions.DontRequireReceiver);
        }
    }
}
