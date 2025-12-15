using System.Collections.Generic;
using UnityEngine;

public class AINode : MonoBehaviour
{
    [Tooltip("Automatic Connection Setting: Leave this empty for automatic connections to be generated.")]
    public List<AINode> neighbors = new();

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, 0.25f);
        Gizmos.color = Color.gray;
        foreach (var i in neighbors)
        {
            if (i)
            {
                Gizmos.DrawLine(transform.position, i.transform.position);
            }
        }
    }
}
