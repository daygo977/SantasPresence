using UnityEngine;
using UnityEngine.EventSystems;

public class UIBlocker : MonoBehaviour, IPointerClickHandler
{
    public GameObject quitPanel;

    // UI Blocker just serves as a raycastable panel that blocks a certain part of the menu
    // Specifically where you press "QUIT" and another prompt "Are you sure you want to quit?" pops up, preventing the player from being able to hover and click the buttons in the Main Menu Panel.

    public void OnPointerClick(PointerEventData eventData)
    {
        quitPanel.SetActive(false);
        gameObject.SetActive(false);
    }
}
