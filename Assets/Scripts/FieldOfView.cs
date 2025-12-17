using System.Collections;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
    [Header("FOV Settings")]
    public float radius;
    [Range(0, 360)] public float angle;

    public LayerMask targetMask;
    public LayerMask obstructionMask;

    [Header("Detection")]
    public float timeToLose = 3f;
    public float detectionDecayRate = 1f;

    [Header("Debug")]
    public bool canSeePlayer;
    public bool showVisionConeGiz = false;

    private GameObject playerRef;
    private float detectionTimer = 0f;

    private void Start()
    {
        playerRef = GameObject.FindGameObjectWithTag("Player");
        DetectionManager.Instance.RegisterEnemy(this);
        StartCoroutine(FOVRoutine());
    }

    public float GetCurrentDetection()
    {
        return detectionTimer;
    }

    public float GetTimeToLose()
    {
        return timeToLose;
    }

    private void OnDestroy()
    {
        if (DetectionManager.Instance != null)
            DetectionManager.Instance.UnregisterEnemy(this);
    }

    private IEnumerator FOVRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.2f);
        while (true)
        {
            yield return wait;
            FieldOfViewCheck();
        }
    }

    private void Update()
    {
        UpdateDetectionTimer();
    }

    private void FieldOfViewCheck()
    {
        Collider[] rangeChecks = Physics.OverlapSphere(transform.position, radius, targetMask);

        if (rangeChecks.Length == 0)
        {
            canSeePlayer = false;
            return;
        }

        Transform target = rangeChecks[0].transform;
        Vector3 dirToTarget = (target.position - transform.position).normalized;

        if (Vector3.Angle(transform.forward, dirToTarget) > angle / 2)
        {
            canSeePlayer = false;
            return;
        }

        float dist = Vector3.Distance(transform.position, target.position);

        if (!Physics.Raycast(transform.position, dirToTarget, dist, obstructionMask))
            canSeePlayer = true;
        else
            canSeePlayer = false;
    }

    private void UpdateDetectionTimer()
    {
        if (canSeePlayer)
        {
            float distance = Vector3.Distance(transform.position, playerRef.transform.position);
            float closeness = 1f - (distance / radius);
            closeness = Mathf.Clamp01(closeness);

            float multiplier = Mathf.Lerp(0.4f, 8f, closeness);
            detectionTimer += Time.deltaTime * multiplier;
        }
        else
        {
            detectionTimer -= Time.deltaTime * detectionDecayRate;
        }

        detectionTimer = Mathf.Clamp(detectionTimer, 0f, timeToLose);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showVisionConeGiz) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);

        Vector3 left = DirectionFromAngle(-angle / 2f);
        Vector3 right = DirectionFromAngle(angle / 2f);

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.position, transform.position + left * radius);
        Gizmos.DrawLine(transform.position, transform.position + right * radius);

        if (canSeePlayer && playerRef != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, playerRef.transform.position);
        }
    }

    private Vector3 DirectionFromAngle(float angle)
    {
        angle += transform.eulerAngles.y;
        float rad = angle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
    }
}
