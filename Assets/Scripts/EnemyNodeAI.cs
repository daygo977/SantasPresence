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
    public bool roamOnly=false;     //If true, never patrol
    public bool enablePatrol=true;  //If true, patrol allowed, can go to investigate and hunt, no RoamMap

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
    private int patrolIndex=0;              //Which patrol node we are currentlty on

    private State state;            //Enemy current state (Patrol, RoamMap, Hunt, Investigate)
    private float nextRepathTime;   //Time allowed for repath computation to be done

    //Node roam management
    private AINode currentTarget;                       //Node we are currently moving toward
    private AINode queuedNext;                          //Node we target after current has been reached
    private readonly HashSet<AINode> visited=new();     //Stores recently visited nodes

    private Transform player;
    private Vector3 lastKnownPos;       //Last known position of player
    private float lastSeenTime;         //Time when player was last seen

    private float investigateEndTime;   //Time when investigate mode should end

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

    private float baseDecayRate=1f;         //Original decay rate from FOV script
    private bool baseDecayCaptured=false;   //True, when we save base decay

    [Header("Debug Gizmos")]
    public bool showAIPerceptionGizmos=true;
    
    [Header("Node Gizmos")]
    public bool showNodesGizmos=true;

    [Tooltip("Optional: assign MapNodes parent transform to only draw/count roam nodes")]
    public Transform roamNodesRoot;     //If set, only consider nodes under this

    public float nodeGizmosMaxDrawDistance=60f;

    public Color unvisitedNodeColor=new Color(0.6f,0.6f,0.6f,1f);   //Unvisited node color
        public Color visitedNodeColor=Color.green;                  //Visited node color
    public Color currentTargetColor=Color.white;                    //Color for current target node
    public Color queuedTargetColor=Color.magenta;                   //Color for queued node

    [Header("Visited Reset")]
    public bool resetVisitedWhenAllVisited=true;        //If ture, clear visited list when all nodes are visited

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
        //Pick starting Enemy State
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
                    //Tell enemy it sees player
                    OnSeePlayer(playerTagTf);
                }
        }
        
        //Check nearby nodes as visited
        CheckNodePerception();

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

        //Go to next patrol node, when close enough
        if (HasReached(target.transform.position))
        {
            patrolIndex=(patrolIndex+1)%patrolRoute.Count;
        }
    }

    private void UpdateNodeRoam(Vector3? filterCenter, float filterRadius)
    {
        //Visited reset, only clears visited, does not wipe current plan unless other function does
        TryResetVisitedIfAllVisited();
        
        //Initialize target if we don't have one
        if (!currentTarget)
        {
            PickInitialTargets(filterCenter, filterRadius);
            return;
        }
        
        //If close to current target node, decide where to go
        if (HasReached(currentTarget.transform.position))
        {   
            //Mark node as visited, so we can avoid choosing it again
            visited.Add(currentTarget);
            //After adding node, check list, if all has been visited, reset
            TryResetVisitedIfAllVisited();

            //Use queued node if we have one already, else pick a new one from current
            AINode next = queuedNext? queuedNext : PickNextQueued(fromNode: currentTarget, filterCenter, filterRadius);
            //After using queued node, clear it
            queuedNext=null;

            //Swap queued next node to current, then pick new next
            if (next)
            {
                currentTarget=next;
                queuedNext=PickNextQueued(fromNode: currentTarget, filterCenter, filterRadius);

                //Skip cooldown and update the NavMeshAgent path this frame
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

        //If something cleared target (reset/fail), pick again
        if (!currentTarget)
        {
            PickInitialTargets(filterCenter, filterRadius);
            return;
        }

        //If all is normal, keep going to target (does use repath cooldown)
        SetDestinationIfReady(currentTarget.transform.position);
    }

    /// <summary>
    /// Pick starting roam node (currentTarget) and pre-pick the next node (queuedNext)
    /// </summary>
    private void PickInitialTargets(Vector3? filterCenter, float filterRadius)
    {
        //If node system is not set up, no target can be picked
        if (NodeGraph.Instance == null){
            return;
        }

        //Try to find a valid start node (unvisited, reachable, and inside filter)
        var candidate=PickRandomCandidateNode(filterCenter, filterRadius);

        //Go back, if that fails, grab any random node from graph
        if (!candidate)
        {   
            candidate=NodeGraph.Instance.GetRandomNode();
        }
        //Set first destination node
        currentTarget=candidate;

        //Look ahead for queued node, so we can swap when arriving
        queuedNext=PickNextQueued(fromNode: currentTarget, filterCenter, filterRadius);
        //Move immediately (no repath cooldown) so we start going there
        ForceDestination(currentTarget.transform.position);
    }

    /// <summary>
    /// Tries to pick random node, that is not visited, not null, inside the filter radius (when investigating)
    /// , and reachable in the NavMesh
    /// </summary>
    private AINode PickRandomCandidateNode(Vector3? filterCenter, float filterRadius)
    {
        if (NodeGraph.Instance == null){
            return null;
        }
        //Get all nodes we can choose from NodeGraph
        var all=NodeGraph.Instance.AllNodes;

        if (all.Count == 0)
        {
            return null;
        }

        //Try 15 times to pick valid random node (tweak number for more or less tries)
        for (int i=0; i<15; i++)
        {
            //Pick a random node from the list
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

    /// <summary>
    /// Pick next node to move from starting node.
    /// Prefers closest reachable, unvisited neighbor (if they exist).
    /// Else, search all nodes.
    /// </summary>
    private AINode PickNextQueued(AINode fromNode, Vector3? filterCenter, float filterRadius)
    {
        if (NodeGraph.Instance == null){
            return null;
        }
        //If no starting node, then we can't choose a next node
        if (!fromNode)
        {
            return null;
        }

        AINode best=null;               //Best next node found so far
        float bestDist=float.MaxValue;  //Distance to best node

        //Prefer connected neighbors if the node has some, else go back to searching nodes in graph
        IEnumerable<AINode> options=(fromNode.neighbors != null && fromNode.neighbors.Count > 0)? fromNode.neighbors : NodeGraph.Instance.AllNodes;

        foreach (var n in options)
        {
            //Skip missing nodes and visited ones
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

            //Chose closest reachable option
            float d = (n.transform.position-fromNode.transform.position).sqrMagnitude;
            if (d<bestDist && IsReachable(n.transform.position))
            {
                bestDist=d;
                best=n;
            }
        }
        
        // If no valid best node, go back to random unvisited reachable node
        if (!best)
        {
            best = PickRandomCandidateNode(filterCenter, filterRadius);
        }
        return best;
    }

    /// <summary>
    /// Chase player while they are visible, else move to player last known location.
    /// If player has not been seen for a while, switch to Investigate
    /// </summary>
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

    /// <summary>
    /// Seach around the last known position for a limited time.
    /// When timer ends, return to Patrol (if allowed) or Roam.
    /// </summary>
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

    /// <summary>
    /// Changes Enemy state
    /// </summary>
    /// <param name="newState"></param>
    private void SwitchState(State newState)
    {
        state=newState;
        Debug.Log($"{name} switched to state: {state}");

        //Clear roam targets whenever we change state
        currentTarget = null;
        queuedNext = null;

        //If investigation statem set when it should end
        if (state == State.Investigate)
        {
            investigateEndTime = Time.time+investigateDuration;
        }
        //Update FOV suspicion decay settings for correct state
        ApplySuspicionDecayForState();
    }

    /// <summary>
    /// Adjust how fast suspicion/detection drops based on the current enemy state
    /// </summary>
    private void ApplySuspicionDecayForState()
    {
        if (!driveSuspicionDecay || fov==null || !baseDecayCaptured)
        {
            return;
        }
        
        //Default multiplier based on current state
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
                mult=huntDecayMultiplier;   //0, to stop decay while hunting
                break;
        }
        //Apply final decay rate to FOV component
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
        //Set time for next repath calculation
        nextRepathTime=Time.time+repathCooldown;

        //Only set destination if agent exists and is currently on NavMesh
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(pos);
        }
    }

    /// <summary>
    /// Set NavMesh destination immediately
    /// </summary>
    private void ForceDestination(Vector3 pos)
    {
        //Reset cooldown, so SetDestination can run instantly
        nextRepathTime = 0f;
        //Only set destination if agent exists and is currently on NavMesh
        if (agent != null && agent.isOnNavMesh)
            agent.SetDestination(pos);
    }


    /// <summary>
    /// Return true if agent can find a full NavMesg path to location
    /// </summary>
    private bool IsReachable(Vector3 pos)
    {
        //If we don't have agent or not on NavMesh, no path found
        if (agent==null || !agent.isOnNavMesh)
        {
            return false;
        }
        //Temporary path used to test a route
        NavMeshPath path=new NavMeshPath();

        //Try to calculate path from current position to target position
        if (!NavMesh.CalculatePath(transform.position, pos, NavMesh.AllAreas, path))
        {
            return false;   //Path calculation failed
        }
        //Only count as reachable if path is complete
        return path.status==NavMeshPathStatus.PathComplete;
    }

    /// <summary>
    /// Returns true if node is allowed to be used for roaming
    /// </summary>
    private bool IsRelevantRoamNode(AINode n)
    {
        //Null check
        if (!n)
        {
            return false;
        }
        //If no root filter, all nodes are valid roam nodes
        if (roamNodesRoot == null)
        {
            return true;
        }
        //Only allow nodes that are under specific root objects
        return n.transform.IsChildOf(roamNodesRoot);
    }

    /// <summary>
    /// If we have visited relevant roam node, clear visited set so roaming can loop
    /// </summary>
    private void TryResetVisitedIfAllVisited()
    {
        //If feature is off, do nothing
        if (!resetVisitedWhenAllVisited)
        {
            return;
        }
        if (NodeGraph.Instance == null)
        {
            return;
        }
        //Only reset in roaming state
        if (state != State.RoamMap && state != State.Investigate)
        {
            return;
        }
        //Count how many nodes are considered relevant for roaming
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
        
        //Count how many visited nodes are relevant
        int visitedRelevant=0;
        foreach (var n in visited)
        {
            if (IsRelevantRoamNode(n))
            {
                visitedRelevant++;
            }
        }

        //If we have visited all relevant nodes, clear so we can start again
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

        //Queued next node (the one we plan to go to after currentTarget)
        if (queuedNext!=null)
        {
            Gizmos.color=queuedTargetColor;

            Vector3 from=(currentTarget!=null)? currentTarget.transform.position : transform.position;
            Gizmos.DrawLine(from, queuedNext.transform.position);
            Gizmos.DrawWireSphere(queuedNext.transform.position, 0.3f);
        }
    }
}