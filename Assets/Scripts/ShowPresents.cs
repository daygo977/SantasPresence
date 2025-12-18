using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script make the presents in the christmas tree prefab not visible in-game. After the player interacts with the tree then presents are placed.

public class ShowPresents : MonoBehaviour
{
    [Header("Present Model To Show")]
    [SerializeField] private GameObject presentModel;  // assign in Inspector

    private bool hasPresent = false;

    private void Start()
    {
        // Hide the present at the start
        if (presentModel != null)
        {
            presentModel.SetActive(false);
        }
    }

    // Call this when the present has been planted
    public void ShowPresent()
    {
        if (hasPresent) return;

        hasPresent = true;

        if (presentModel != null)
        {
            presentModel.SetActive(true);
        }
    }
}
