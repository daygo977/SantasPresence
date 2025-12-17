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
    public float nodeArriveDistance=1.1f; //How close the enemy must get to a node before being reached
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

    [Header("Sensing")]
    public FieldOfView fov;
    public string playerTag="Player";
    private Transform playerTagTf;

    [Header("Suspicion/Detection Decay (by State)")]
    [Tooltip("If true, EnemyNodeAI will adjust FOV.detectDecayRate based on State")]
    public bool driveSuspicionDecay=true;

    [Tooltip("Multiplier applied to FOV's starting detectionDecayRate while Patroling")]
    public float patrolDecayMultiplier=2.0f;

    [Tooltip("Multiplier applied to FOV's starting detectionDecayRate while Roaming")]
    public float roamDecayMultiplier=2.0f;

    [Tooltip("Multiplier applied to FOV's starting detectionDecayRate while Investigating")]
    public float investigateDecayMultiplier=0.35f;

    [Tooltip("Multiplier applied to FOV's starting detectionDecayRate while Hunting. Set 0 = no decay")]
    public float huntDecayMultiplier=0f;

    private float baseDecayRate=1f;
    private bool baseDecayCaptured=false;

    [Header("Debug Gizmos")]
    public bool showAIPerceptionGizmos=true;
    
    [Header("Node Gizmos")]
    public bool showNodesGizmos=true;

    [Tooltip("Optional: assign MapNodes parent transform to only draw/count roam nodes")]
    public Transform roamNodesRoot;

    public float nodeGizmosMaxDrawDistance=60f;

    public Color unvisitedNodeColor=new Color(0.6f,0.6f,0.6f,1f);
    public Color visitedNodeColor=Color.green;
    public Color currentTargetColor=Color.white;
    public Color queuedTargetColor=Color.magenta;

    [Header("Visited Reset")]
    public bool resetVisitedWhenAllVisited=true;
    public bool resetTargetWhenAllVisited=true;

    /// <summary>
    /// Called in editor when script is added or reset.
    /// Auto set NavMeshAgent component
    /// </summary>
    private void Reset()
    {
        agent=GetComponent<NavMeshAgent>();
        fov=GetComponentInChildren<FieldOfView>();
    }

    private void Start()
    {
        if (!agent)
        {
            agent=GetComponent<NavMeshAgent>();
        }

        if (!fov)
        {
            fov=GetComponentInChildren<FieldOfView>();
        }

        //Cache player transform
        var go=GameObject.FindGameObjectWithTag(playerTag);
        if (go)
        {
            playerTagTf=go.transform;
        }
        //Cache base suspicion decay
        if (fov != null)
        {
            baseDecayRate=fov.detectionDecayRate;
            baseDecayCaptured=true;
        }
        EnterInitialState();
    }

    /// <summary>
    /// Starting state based on what is checked (roamOnly, enablePatrol)
    /// </summary>
    private void EnterInitialState()
    {
        if (roamOnly || !enablePatrol || patrolRoute.Count == 0)
        {
            SwitchState(State.RoamMap);
        }
        else
        {
            SwitchState(State.Patrol);
        }
    }

    private void Update()
    {
        //If fov says it sees the player, refresh OnSeePlayer each frame
        if (fov!=null && fov.canSeePlayer)
        {
            if (!playerTagTf)
            {
                var go=GameObject.FindGameObjectWithTag(playerTag);
                if (go)
                {
                    playerTagTf=go.transform;
                }
            }
            if (playerTagTf)
                {
                    OnSeePlayer(playerTagTf);
                }
        }

        CheckNodePerception(); //Check nearby nodes

        switch (state)
        {
            case State.Patrol:
                UpdatePatrol();  //Logic for following patrol route
                break;
            
            case State.RoamMap:
                //Logic for roaming
                UpdateNodeRoam(filterCenter: null, filterRadius: 0f);
                break;

            case State.Hunt:
                //Logic for chasing player's last location
                UpdateHunt();
                break;
            
            case State.Investigate:
                //Logic for search specific area
                UpdateInvestigate();
                break;
        }
    }

    public void OnSeePlayer(Transform player_spot)
    {
        //Update player reference and the last known postion
        player=player_spot;
        lastKnownPos=player_spot.position;
        lastSeenTime=Time.time;

        //If not in hunt, then switch to hunt state
        if (state!=State.Hunt)
        {
            SwitchState(State.Hunt);
        }
    }

    public void OnHearNoise(Vector3 pos)
    {
        //Ignore noise if enemy is already hunting
        if (state==State.Hunt)
        {
            return;
        }
        //Set disturbance location and switch to investigate state
        lastKnownPos=pos;
        SwitchState(State.Investigate);
    }

    private void UpdatePatrol()
    {
        //If route is empty, switch to roam
        if (patrolRoute.Count==0)
        {
            SwitchState(State.RoamMap);
            return;
        }

        //Get current target node form list
        var target=patrolRoute[patrolIndex];
        if (!target)
        {
            //Skip null node and move to next index
            patrolIndex=(patrolIndex+1)%patrolRoute.Count;
            return;
        }

        SetDestinationIfReady(target.transform.position);

        //Increase index when close enough
        if (HasReached(target.transform.position))
        {
            patrolIndex=(patrolIndex+1)%patrolRoute.Count;
        }
    }

    private void UpdateNodeRoam(Vector3? filterCenter, float filterRadius)
    {
        //Visited reset, only clears visited, does not wipe current plan
        TryResetVisitedIfAllVisited();
        
        //Initialize target if we don't have one
        if (!currentTarget)
        {
            PickInitialTargets(filterCenter, filterRadius);
            return;
        }
        
        //If close to current target node
        if (HasReached(currentTarget.transform.position))
        {   
            visited.Add(currentTarget);
            TryResetVisitedIfAllVisited();

            //Prefer queued node, if we got one, else pick fresh next node near one we reached
            AINode next = queuedNext? queuedNext : PickNextQueued(fromNode: currentTarget, filterCenter, filterRadius);
            queuedNext=null;

            //Swap queued next node to current, then pick new next
            if (next)
            {
                currentTarget=next;
                queuedNext=PickNextQueued(fromNode: currentTarget, filterCenter, filterRadius);

                //Force immediate repath so movement matches new target this frame
                ForceDestination(currentTarget.transform.position);
            }
            else
            {
                //No valid next node, reinitialize next frame
                currentTarget=null;
                queuedNext=null;
                return;
            }
        }

        if (!currentTarget)
        {
            PickInitialTargets(filterCenter, filterRadius);
            return;
        }

        SetDestinationIfReady(currentTarget.transform.position);
    }

    private void PickInitialTargets(Vector3? filterCenter, float filterRadius)
    {
        if (NodeGraph.Instance == null){
            return;
        }

        //Try to find a valid start node
        var candidate=PickRandomCandidateNode(filterCenter, filterRadius);
        if (!candidate)
        {   
            //Pick node from node graph instance
            candidate=NodeGraph.Instance.GetRandomNode();
        }
        currentTarget=candidate;

        //Look ahead for queued node
        queuedNext=PickNextQueued(fromNode: currentTarget, filterCenter, filterRadius);

        ForceDestination(currentTarget.transform.position);
    }

    private AINode PickRandomCandidateNode(Vector3? filterCenter, float filterRadius)
    {
        if (NodeGraph.Instance == null){
            return null;
        }

        var all=NodeGraph.Instance.AllNodes;
        if (all.Count == 0)
        {
            return null;
        }

        //Try 12 times to pick valid random node (tweak number for more or less tries)
        for (int i=0; i<12; i++)
        {
            var n=all[Random.Range(0, all.Count)];

            //Skip invalid or already visited nodes
            if (!n || visited.Contains(n))
            {
                continue;
            }

            //Use distance filter (for investigate state)
            if (filterCenter.HasValue)
            {
                if ((n.transform.position-filterCenter.Value).sqrMagnitude > filterRadius*filterRadius)
                {
                    continue;
                }
            }

            //Checks to see if NavMesh path exists
            if (IsReachable(n.transform.position))
            {
                return n;
            }
        }

        return null;
    }

    private AINode PickNextQueued(AINode fromNode, Vector3? filterCenter, float filterRadius)
    {
        if (NodeGraph.Instance == null){
            return null;
        }

        if (!fromNode)
        {
            return null;
        }

        AINode best=null;
        float bestDist=float.MaxValue;

        //Use neighbors if possible, else check all nodes
        IEnumerable<AINode> options=(fromNode.neighbors != null && fromNode.neighbors.Count > 0)? fromNode.neighbors : NodeGraph.Instance.AllNodes;

        foreach (var n in options)
        {
            if (!n || visited.Contains(n))
            {
                continue;
            }

            //Use investigation radius filter
            if (filterCenter.HasValue)
            {
                if ((n.transform.position-filterCenter.Value).sqrMagnitude > filterRadius*filterRadius)
                {
                    continue;
                }
            }

            float d = (n.transform.position-fromNode.transform.position).sqrMagnitude;
            if (d<bestDist && IsReachable(n.transform.position))
            {
                bestDist=d;
                best=n;
            }
        }
        
        // If no valid next node, go back to random unvisited reachable node
        if (!best)
        {
            best = PickRandomCandidateNode(filterCenter, filterRadius);
        }
        return best;
    }

    private void UpdateHunt()
    {
        // If player reference is missing, investigate last known position
        if (!player)
        {
            SwitchState(State.Investigate);
            return;
        }

        //Only update lastKnownPos while actually visible (prevent seing through wall tracking)
        bool seesNow=(fov!=null && fov.canSeePlayer);
        if (seesNow)
        {
            lastKnownPos=player.position;
            lastSeenTime=Time.time;
        }
        //Move to last known position (seen)
        SetDestinationIfReady(lastKnownPos);
        
        // If we haven't been seeing player and we don't see them in the moment, go to investigate
        if (!seesNow && (Time.time-lastSeenTime > loseSightDelay))
        {
            SwitchState(State.Investigate);
        }
    }

    private void UpdateInvestigate()
    {
        // Finished investigating, return to patrol or roammap
        if (Time.time >= investigateEndTime)
        {
            if (!roamOnly && enablePatrol && patrolRoute.Count > 0)
            {
                SwitchState(State.Patrol);
            }
            else
            {
                SwitchState(State.RoamMap);
            }
            return;
        }

        //Roam only within the investigate circle around last known pos
        UpdateNodeRoam(filterCenter: lastKnownPos, filterRadius: investigateRadius);
    }

    private void SwitchState(State newState)
    {
        state=newState;
        Debug.Log($"{name} switched to state: {state}");

        //Clear roam targets whenever we change state
        currentTarget = null;
        queuedNext = null;

        if (state == State.Investigate)
        {
            investigateEndTime = Time.time+investigateDuration;
        }

        ApplySuspicionDecayForState();
    }

    private void ApplySuspicionDecayForState()
    {
        if (!driveSuspicionDecay || fov==null || !baseDecayCaptured)
        {
            return;
        }

        float mult = 1f;
        switch (state)
        {
            case State.Patrol:
                mult=patrolDecayMultiplier;
                break;
        
            case State.RoamMap:
                mult=roamDecayMultiplier;
                break;

            case State.Investigate:
                mult=investigateDecayMultiplier;
                break;

            case State.Hunt:
                mult=huntDecayMultiplier;
                break;
        }

        fov.detectionDecayRate=baseDecayRate*mult;
    }

    /// <summary>
    /// Mark nodes within perception radius as visited
    /// </summary>
    private void CheckNodePerception()
    {
        if (NodeGraph.Instance == null){
            return;
        }

        float r2 = perceptionRadius*perceptionRadius;
        Vector3 p = transform.position;

        //perception, distance check against all nodes, bug fix: prevents nodes from flashing (when using gizmos)
        //from showing all green out of nowhere
        foreach (var n in NodeGraph.Instance.AllNodes)
        {
            if (!n) continue;

            Vector3 d = n.transform.position-p;
            d.y = 0f;

            if (d.sqrMagnitude <= r2)
            {
                visited.Add(n);
            }
        }
    }

    /// <summary>
    /// "arrived" check using NavMeshAgent remainingDistance and fallback distance
    /// </summary>
    private bool HasReached(Vector3 pos)
    {
        if (!agent || !agent.enabled)
        {
            return (pos-transform.position).sqrMagnitude <= nodeArriveDistance*nodeArriveDistance;
        }

        if (!agent.pathPending && agent.hasPath && agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, nodeArriveDistance))
        {
            return true;
        }

        return (pos-transform.position).sqrMagnitude <= nodeArriveDistance*nodeArriveDistance;
    }
    
    /// <summary>
    /// controlled set destination to avoid multiple repaths every frame
    /// </summary>
    private void SetDestinationIfReady(Vector3 pos)
    {
        if (Time.time < nextRepathTime)
        {
            return;
        }
        nextRepathTime=Time.time+repathCooldown;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(pos);
        }
    }

    private void ForceDestination(Vector3 pos)
    {
        nextRepathTime = 0f;
        if (agent != null && agent.isOnNavMesh)
            agent.SetDestination(pos);
    }


    // Checks if a complete NavMesh path exists to the position
    private bool IsReachable(Vector3 pos)
    {
        if (agent==null || !agent.isOnNavMesh)
        {
            return false;
        }
        NavMeshPath path=new NavMeshPath();

        if (!NavMesh.CalculatePath(transform.position, pos, NavMesh.AllAreas, path))
        {
            return false;
        }

        return path.status==NavMeshPathStatus.PathComplete;
    }

    private bool IsRelevantRoamNode(AINode n)
    {
        if (!n)
        {
            return false;
        }
        if (roamNodesRoot == null)
        {
            return true;
        }
        return n.transform.IsChildOf(roamNodesRoot);
    }

    private void TryResetVisitedIfAllVisited()
    {
        if (!resetVisitedWhenAllVisited)
        {
            return;
        }
        if (NodeGraph.Instance == null)
        {
            return;
        }
        if (state != State.RoamMap && state != State.Investigate)
        {
            return;
        }

        int total=0;
        foreach (var n in NodeGraph.Instance.AllNodes)
        {
            if (IsRelevantRoamNode(n))
            {
                total++;
            }
        }

        if (total <= 0)
        {
            return;
        }

        int visitedRelevant=0;
        foreach (var n in visited)
        {
            if (IsRelevantRoamNode(n))
            {
                visitedRelevant++;
            }
        }

        if (visitedRelevant >= total)
        {
            visited.Clear();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showAIPerceptionGizmos)
        {
            return;
        }

        //Perception radius (node checker)
        Gizmos.color=Color.yellow;
        Gizmos.DrawWireSphere(transform.position, perceptionRadius);

        if (!Application.isPlaying)
        {
            return;
        }

        //Investigate Circle
        if (state == State.Investigate)
        {
            Gizmos.color=Color.cyan;
            Gizmos.DrawWireSphere(lastKnownPos, investigateRadius);   
        }

        //Draw Nodes (visited/unvisited)
        if (showNodesGizmos && NodeGraph.Instance != null)
        {
            float maxDistSqr=nodeGizmosMaxDrawDistance*nodeGizmosMaxDrawDistance;

            foreach (var n in NodeGraph.Instance.AllNodes)
            {
                if (!IsRelevantRoamNode(n)){
                    continue;
                }   
                if ((n.transform.position-transform.position).sqrMagnitude > maxDistSqr)
                {
                    continue;
                }

                Gizmos.color=visited.Contains(n)? visitedNodeColor : unvisitedNodeColor;
                Gizmos.DrawSphere(n.transform.position, 0.3f);
            }
        }

        //Current target
        if (currentTarget!=null)
        {
            Gizmos.color=Color.white;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
            Gizmos.DrawWireSphere(currentTarget.transform.position, 0.25f);
        }

        if (queuedNext!=null)
        {
            Gizmos.color=queuedTargetColor;

            Vector3 from=(currentTarget!=null)? currentTarget.transform.position : transform.position;
            Gizmos.DrawLine(from, queuedNext.transform.position);
            Gizmos.DrawWireSphere(queuedNext.transform.position, 0.3f);
        }
    }
}