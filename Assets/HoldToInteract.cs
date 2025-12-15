using UnityEngine;
using UnityEngine.UI;

public class HoldToInteract : MonoBehaviour
{
    public float holdTime = 6f; // Amount of time needed for the player to hold E to plant the presents by the tree
    public GameObject uiObject; // UI Panel "PlantUI" to pop up as a prompt on screen
    public Slider progressSlider; // Progress bar to indicate how long we've held E for

    private bool playerInside = false; // Initially false
    private float holdProgress = 0f; // Initially 0
    private bool canPlant = true; // Initially possible to plant a present under the tree

    void Start()
    {
        uiObject.SetActive(false); // Initially hidden
        progressSlider.value = 0f; // Initially 0 for slider

        GameManager.Instance.RegisterTree();
    }

    void Update()
    {
        // If the tree's sphere collider detects a player inside
        if (playerInside && canPlant)
        {
            // E is the input needed for progress to be made
            if (Input.GetKey(KeyCode.F))
            {
                // holdProgress holds the point in which we started holding F
                holdProgress += Time.deltaTime;
                // Slider reflects the amount of time we've been holding E since we first held it
                progressSlider.value = holdProgress / holdTime;

                // If we've held it as long as needed, plant successful
                if (holdProgress >= holdTime)
                {
                    CompleteInteraction();
                }
            }
            else
            {
                // If let go, progress is interrupted and is decreasing over time
                holdProgress -= Time.deltaTime * 2f;
                holdProgress = Mathf.Max(0f, holdProgress);
                progressSlider.value = holdProgress / holdTime;
            }
        }
    }

    private void CompleteInteraction()
    {
        canPlant = false; // We can no longer plant more presents under this tree
        uiObject.SetActive(false); // Hide UI panel now
        playerInside = false;

        GameManager.Instance.PlantGift();
        Debug.Log("Tree planted!");
    }

    private void OnTriggerEnter(Collider other)
    {
        // Look for object with tag "Player", should only be player object that has this
        if (other.CompareTag("Player") && canPlant)
        {
            playerInside = true; // If player found, set this to true
            uiObject.SetActive(true); // Show UI panel
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // When the player leaves the trigger collider, then hide the UI and player is no longer inside
        if (other.CompareTag("Player") && canPlant)
        {
            playerInside = false;
            holdProgress = 0f;
            progressSlider.value = 0f;
            uiObject.SetActive(false);
        }
    }
}
