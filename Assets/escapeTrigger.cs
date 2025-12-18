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
            GameManager.Instance.WinGame();
        }
    }
}
