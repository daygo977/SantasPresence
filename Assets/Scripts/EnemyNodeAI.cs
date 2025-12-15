using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyNodeAI : MonoBehaviour
{
    public enum State { Patrol, RoamMap, Hunt, Investigate}

    [Header("Toggles")]
    public bool roamOnly=false;
    public bool enablePatrol=true;

    [Header("Nav")]
    public NavMeshAgent agent;

    [Header("Nodes / Roam")]
    public float perceptionRadius=6f; //nodes get checked in this radius
    public float nodeArriveDistnce=1.1f; //tighter arrival (optional)
    public float repathCooldown=0.2f;

    [Header("Investigate (Roam Level 2)")]
    public float investigateRadius=18f;
    public float investigateDuration=8f;

    [Header("Hunt")]
    public float loseSightDelay=2f;

    [Header("Patrol (optional)")]
    public List<AINode> patrolRoute=new();
    private int patrolIndex=0;

    private State state;
    private float nextRepathTime;

    //Node roam management
    private AINode currentTarget;
    private AINode queuedNext;
    private readonly HashSet<AINode> visited=new();

    private Transform player;
    private Vector3 lastKnownPos;
    private float lastSeenTime;

    private float investigateEndTime;
}
