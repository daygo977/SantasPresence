using System.Collections.Generic;
using UnityEngine;

public class NodeGraph : MonoBehaviour
{
    public static NodeGraph Instance { get; private set; }

    [SerializeField] private List<AINode> nodes = new();

    private void Awake()
    {
        Instance = this;
        if (nodes.Count == 0)
        {
            nodes.AddRange(FindObjectsOfType<AINode>());
        }
    }

    public IReadOnlyList<AINode> AllNodes => nodes;

    public List<AINode> GetNodeInRadius(Vector3 center, float radius)
    {
        float radiusSqr = radius*radius;
        var result = new List<AINode>();
        foreach (var i in nodes)
        {
            if (!i)
            {
                continue;
            }
            if ((i.transform.position-center).sqrMagnitude <= radiusSqr)
            {
                result.Add(i);
            }
        }
        return result;
    }

    public AINode GetRandomNode()
    {
        if (nodes.Count == 0)
        {
            return null;
        }
        return nodes[Random.Range(0, nodes.Count)];
    }
}
