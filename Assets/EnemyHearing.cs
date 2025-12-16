using UnityEngine;

public class EnemyHearing : MonoBehaviour
{
    public void HearNoise(Vector3 sourcePosition, float radius)
    {
        float distance = Vector3.Distance(transform.position, sourcePosition);

        if (distance <= radius)
        {
            Debug.Log($"{name} heard a noise at distance {distance:F2}");
        }
    }
}
