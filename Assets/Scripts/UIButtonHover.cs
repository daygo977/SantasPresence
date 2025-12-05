using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Transform textTransform;   // Text to scale
    private Vector3 originalScale;
    public float hoverScale = 1.15f;  // How big the text grows when hovering
    public float moveSpeed = 10f;     // Scale speed
    public AudioSource audioSource;
    public AudioClip hoverClip;
    public AudioClip clickClip;
    public bool canClick;
    private bool hovering;

    void Start()
    {
        if (textTransform == null)
            textTransform = transform.GetChild(0);

        originalScale = textTransform.localScale;
    }

    void Update()
    {
        // Target scale based on hover state
        Vector3 targetScale = hovering ? originalScale * hoverScale : originalScale;

        // Smoothly scale toward target
        textTransform.localScale = Vector3.Lerp(
            textTransform.localScale,
            targetScale,
            Time.unscaledDeltaTime * moveSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        Debug.Log("Hovering");
        if (audioSource && hoverClip)
            audioSource.PlayOneShot(hoverClip, 0.5f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (audioSource && clickClip)
            audioSource.PlayOneShot(clickClip, 0.5f);
    }

    public void OnDisable()
    {
        hovering = false;

        // Force reset scale so it doesn't stay big past panel reset/disable
        if (textTransform != null)
            textTransform.localScale = originalScale;
    }
}
