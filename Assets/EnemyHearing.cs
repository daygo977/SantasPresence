using UnityEngine;

public class EnemyHearing : MonoBehaviour
{
    private EnemyNodeAI ai;

    private void Awake()
    {
        ai=GetComponentInParent<EnemyNodeAI>();
    }

    public void HearNoise(Vector3 sourcePosition, float radius)
    {
        float distance = Vector3.Distance(transform.position, sourcePosition);

        if (distance <= radius)
        {
            Debug.Log($"{name} heard a noise at distance {distance:F2}");

            if (ai != null)
            {
                ai.OnHearNoise(sourcePosition);
            }
        }
    }
}
