using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyNodeAI : MonoBehaviour
{
    // States for enemy AI behavior
    // - Patrol: following defined path
    // - Roam (Level 1): randomly between nodes
    // - Hunt: Actively chase/pursue player, from last known location
    // - Investigate (Roam Level 2): search area where disturbence and/or player last seen
    public enum State { Patrol, RoamMap, Hunt, Investigate}

    [Header("Toggles")]
    public bool roamOnly=false;
    public bool enablePatrol=true;

    [Header("Nav")]
    public NavMeshAgent agent;  //Unity component for movement and pathfinding

    [Header("Nodes / Roam")]
    public float perceptionRadius=6f; //Distance used to check nearby nodes
    public float nodeArriveDistnce=1.1f; //How close the enemy must get to a node before being reached
    public float repathCooldown=0.2f;   //Min time between recomputing path (for optimization)

    [Header("Investigate (Roam Level 2)")]
    public float investigateRadius=18f;     //Radius around disturbance to search nodes
    public float investigateDuration=8f;    //How long the enemy will stay in investigate state

    [Header("Hunt")]
    public float loseSightDelay=2f;     //Time delay before enemy loses hunt state

    [Header("Patrol (optional)")]
    public List<AINode> patrolRoute=new();  //List for patrol route
    private int patrolIndex=0;

    private State state;            //Enemy current state (Patrol, RoamMap, Hunt, Investigate)
    private float nextRepathTime;   //Time allowed for repath computation to be done

    //Node roam management
    private AINode currentTarget;
    private AINode queuedNext;
    private readonly HashSet<AINode> visited=new();     //Stores recently visited nodes

    private Transform player;
    private Vector3 lastKnownPos;
    private float lastSeenTime;

    private float investigateEndTime;
}
