using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitDoor : MonoBehaviour
{
    /*[Header("Exit Settings")]
    public float holdTimeRequired = 3f;
    public string nextSceneName;

    [Header("State")]
    public bool missionComplete = false;

    private float holdTimer = 0f;
    private bool playerInRange = false;

    void Update()
    {
        if (!missionComplete || !playerInRange)
        {
            holdTimer = 0f;
            return;
        }

        if (Input.GetKey(KeyCode.E))
        {
            holdTimer += Time.deltaTime;

            if (holdTimer >= holdTimeRequired)
            {
                ExitLevel();
            }
        }
        else
        {
            holdTimer = 0f;
        }
    }

    void ExitLevel()
    {
        Debug.Log("Exiting Level...");
        SceneManager.LoadScene(nextSceneName);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            holdTimer = 0f;
        }
    }*/
}
