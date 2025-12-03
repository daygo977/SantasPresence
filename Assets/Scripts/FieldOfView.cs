using System.Collections;
using TMPro;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
    [Header("FOV Settings")]
    public float radius;
    [Range(0,360)]
    public float angle;

    public GameObject playerRef;

    public LayerMask targetMask;
    public LayerMask obstructionMask;

    [Header("Debug")]
    public bool canSeePlayer;
    public bool showVisionConeGiz = false;

    private void Start()
    {
        playerRef = GameObject.FindGameObjectWithTag("Player");
        StartCoroutine(FOVRoutine());
    }

    private IEnumerator FOVRoutine()
    {
        float delay = 0.2f;
        WaitForSeconds wait = new WaitForSeconds(delay);

        while (true)
        {
            yield return wait;
            FieldOfViewCheck();
        }
    }

    private void FieldOfViewCheck()
    {
        Collider[] rangeChecks = Physics.OverlapSphere(transform.position, radius, targetMask);

        if (rangeChecks.Length != 0)
        {
            Transform target = rangeChecks[0].transform;
            Vector3 directionToTarget = (target.position - transform.position).normalized;

            if (Vector3.Angle(transform.forward, directionToTarget) < angle / 2)
            {
                float distanceToTarget = Vector3.Distance(transform.position, target.position);

                if (!Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstructionMask))
                {
                    canSeePlayer = true;
                    Debug.Log("I see you!");
                }
                else
                {
                    canSeePlayer = false;
                    Debug.Log("You are now hidden");
                }
            }
            else
            {
                canSeePlayer = false;
                Debug.Log("You are now hidden");
            }
        }
        else if (canSeePlayer)
        {
            canSeePlayer = false;
            Debug.Log("You are now hidden");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showVisionConeGiz)
            return;

        //Draw radius
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);

        //Draw FOV cone edges (Needs DirectionFromAngle helper function)
        Vector3 leftBound = DirectionFromAngle(-angle / 2f, false);
        Vector3 rightBound = DirectionFromAngle(angle / 2f, false);

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.position, transform.position + leftBound * radius);
        Gizmos.DrawLine(transform.position, transform.position + rightBound * radius);
        
        //Draw line to player if visible
        if (canSeePlayer && playerRef != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, playerRef.transform.position);
        }
    }

    //Help function for Gizmos
    private Vector3 DirectionFromAngle(float angle, bool angleGlobal)
    {
        //Make angle relative to object if local
        if (!angleGlobal)
        {
            angle += transform.eulerAngles.y;
        }

        float rad = angle * Mathf.Deg2Rad;
        //0 degrees is foward, 90 degrees is right 
        return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
    }
}
