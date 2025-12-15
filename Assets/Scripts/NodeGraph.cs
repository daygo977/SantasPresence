using System.Collections.Generic;
using UnityEngine;

public class NodeGraph : MonoBehaviour
{
    // Static property for singleton access. Lets other script get the graph manager easily
    public static NodeGraph Instance { get; private set; }


    [SerializeField] private List<AINode> nodes = new();

    private void Awake()
    {
        //Sets singleton access to Instance
        Instance = this;

        //Find other nodes and establish connections if empty
        if (nodes.Count == 0)
        {
            nodes.AddRange(FindObjectsOfType<AINode>());
        }
    }

    /// <summary>
    /// Safely access the list of nodes, prevents any modification to list
    /// </summary>
    public IReadOnlyList<AINode> AllNodes => nodes;

    /// <summary>
    /// Finds all nodes within a specified radius of center point
    /// </summary>
    public List<AINode> GetNodeInRadius(Vector3 center, float radius)
    {
        float radiusSqr = radius*radius;
        var result = new List<AINode>();
        // Check every node in the list
        foreach (var i in nodes)
        {
            //Skip if node is null
            if (!i)
            {
                continue;
            }
            //Compute squared distance from node to center
            //If distance is <= radiusSqr, node is within the circle
            if ((i.transform.position-center).sqrMagnitude <= radiusSqr)
            {
                result.Add(i);
            }
        }
        //Return list of nodes found within radius
        return result;
    }

    /// <summary>
    /// Returns randomly selected node from graph
    /// </summary>
    public AINode GetRandomNode()
    {
        // Safety check, return null if no nodes in graph
        if (nodes.Count == 0)
        {
            return null;
        }
        // Returns random node, from range 0 to nodes.Count
        return nodes[Random.Range(0, nodes.Count)];
    }
}
