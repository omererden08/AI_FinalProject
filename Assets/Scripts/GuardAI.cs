using UnityEngine;
using UnityEngine.AI;
using BehaviorTreeSystem;

public enum GuardState
{
    Patrol,
    Chase,
    Search,
    ReturnToPatrol
}

[RequireComponent(typeof(NavMeshAgent))]
public class GuardAI : MonoBehaviour
{
    [Header("Vision Settings")]
    [SerializeField] private float viewDistance = 10f;
    [SerializeField] private float viewAngle = 60f;
    
    [Header("Chase Settings")]
    [SerializeField] private float catchDistance = 1.5f;
    [SerializeField] private float waypointReachDistance = 0.5f;
    [SerializeField] private float chasePersistenceTime = 3f;
    [SerializeField] private float chaseSpeedMultiplier = 1.5f;
    [SerializeField] private float searchDuration = 3f;
    [SerializeField] private float lookAroundSpeed = 90f;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] patrolPoints;

    [Header("Debug")]
    [SerializeField] private bool enableDebug;

    [Header("In-Game Visuals")]
    [SerializeField] private bool showVisionCone = true;
    [SerializeField] private Material visionConeMaterial;
    [SerializeField] private Color normalColor = new Color(1f, 1f, 0f, 0.3f);
    [SerializeField] private Color alertColor = new Color(1f, 0f, 0f, 0.5f);

    private NavMeshAgent agent;
    private GuardBehaviorTree behaviorTree;
    private int currentPatrolIndex;
    private Vector3 lastPatrolPosition;
    private Vector3 lastKnownPlayerPosition;
    private float chaseTimer;
    private float normalSpeed;
    private float searchTimer;
    private bool reachedSearchPoint;
    private float lookDirection = 1f;
    private float totalRotation;

    private MeshFilter visionMeshFilter;
    private MeshRenderer visionMeshRenderer;
    private Mesh visionMesh;

    public GuardState CurrentState { get; private set; }
    public float ViewDistance => viewDistance;
    public float ViewAngle => viewAngle;
    public float ChasePersistenceTime => chasePersistenceTime;
    public float ChaseTimer => chaseTimer;
    public float SearchTimer => searchTimer;
    public Vector3 LastKnownPlayerPosition => lastKnownPlayerPosition;
    public bool HasLastKnownPosition => lastKnownPlayerPosition != Vector3.zero;

    private void Start()
    {
        InitializeComponents();
        InitializeBehaviorTree();
        InitializeVisionCone();
        StartPatrol();
    }

    private void Update()
    {
        ProcessBehaviorTree();
        ExecuteCurrentState();
        
    }
    private void LateUpdate()
    {
        UpdateVisionCone();
    }

    private void InitializeComponents()
    {
        agent = GetComponent<NavMeshAgent>();
        normalSpeed = agent.speed;
    }

    private void InitializeBehaviorTree()
    {
        behaviorTree = new GuardBehaviorTree(this);
        behaviorTree.SetDebugEnabled(enableDebug);
    }

    private void StartPatrol()
    {
        CurrentState = GuardState.Patrol;
        GoToNextPatrolPoint();
    }

    private void ProcessBehaviorTree()
    {
        GuardState decision = behaviorTree.Evaluate();
        
        if (decision != CurrentState)
        {
            TransitionToState(decision);
        }
    }

    private void TransitionToState(GuardState newState)
    {
        if (enableDebug)
        {
            Debug.Log($"[GuardAI] State: {CurrentState} -> {newState}");
        }
        
        CurrentState = newState;
    }

    private void ExecuteCurrentState()
    {
        switch (CurrentState)
        {
            case GuardState.Patrol:
                ExecutePatrol();
                break;
            case GuardState.Chase:
                ExecuteChase();
                break;
            case GuardState.Search:
                ExecuteSearch();
                break;
            case GuardState.ReturnToPatrol:
                ExecuteReturnToPatrol();
                break;
        }
    }

    private void ExecutePatrol()
    {
        if (HasReachedDestination())
        {
            GoToNextPatrolPoint();
        }
    }

    private void ExecuteChase()
    {
        agent.speed = normalSpeed * chaseSpeedMultiplier;
        
        if (CanSeePlayer())
        {
            lastKnownPlayerPosition = player.position;
            chaseTimer = chasePersistenceTime;
            agent.SetDestination(player.position);
        }
        else
        {
            chaseTimer -= Time.deltaTime;
            agent.SetDestination(lastKnownPlayerPosition);
        }

        if (IsPlayerInCatchRange())
        {
            CatchPlayer();
        }
    }

    private void ExecuteSearch()
    {
        agent.speed = normalSpeed;

        if (!reachedSearchPoint)
        {
            agent.SetDestination(lastKnownPlayerPosition);
            
            if (Vector3.Distance(transform.position, lastKnownPlayerPosition) < waypointReachDistance)
            {
                reachedSearchPoint = true;
                agent.ResetPath();
                totalRotation = 0f;
            }
            return;
        }

        float rotationAmount = lookAroundSpeed * Time.deltaTime * lookDirection;
        transform.Rotate(0, rotationAmount, 0);
        totalRotation += Mathf.Abs(rotationAmount);

        if (totalRotation >= 180f)
        {
            lookDirection *= -1f;
            totalRotation = 0f;
        }

        searchTimer -= Time.deltaTime;
    }

    public void StartSearch()
    {
        searchTimer = searchDuration;
        reachedSearchPoint = false;
        totalRotation = 0f;
        lookDirection = 1f;
    }

    public bool IsSearchComplete()
    {
        return searchTimer <= 0;
    }

    private void ExecuteReturnToPatrol()
    {
        agent.speed = normalSpeed;
        agent.destination = lastPatrolPosition;

        if (HasReachedLastPatrolPoint())
        {
            CurrentState = GuardState.Patrol;
        }
    }

    private void GoToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) 
            return;

        lastPatrolPosition = patrolPoints[currentPatrolIndex].position;
        agent.destination = lastPatrolPosition;
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    public bool CanSeePlayer()
    {
        if (player == null) 
            return false;

        Vector3 directionToPlayer = player.position - transform.position;
        float distance = directionToPlayer.magnitude;

        if (distance > viewDistance) 
            return false;

        float angle = Vector3.Angle(transform.forward, directionToPlayer.normalized);
        if (angle > viewAngle * 0.5f) 
            return false;

        return HasLineOfSightToPlayer(directionToPlayer.normalized);
    }

    private bool HasLineOfSightToPlayer(Vector3 direction)
    {
        Ray ray = new Ray(new Vector3(transform.position.x, transform.position.y / 2, transform.position.z) + Vector3.up, direction);
        
        if (Physics.Raycast(ray, out RaycastHit hit, viewDistance))
        {
            return hit.transform == player;
        }
        
        return true;
    }

    private bool HasReachedDestination()
    {
        if (agent.pathPending)
            return false;
            
        if (!agent.hasPath)
            return false;
            
        return agent.remainingDistance < waypointReachDistance;
    }

    private bool HasReachedLastPatrolPoint()
    {
        return Vector3.Distance(transform.position, lastPatrolPosition) < waypointReachDistance;
    }

    private bool IsPlayerInCatchRange()
    {
        return Vector3.Distance(transform.position, player.position) < catchDistance;
    }

    private void CatchPlayer()
    {
        GameManager.Instance.SetGameState(GameManager.GameState.Fail);
    }

    private void InitializeVisionCone()
    {
        if (!showVisionCone) return;

        GameObject visionConeObj = new GameObject("VisionCone");
        visionConeObj.transform.SetParent(null);
        visionConeObj.transform.position = transform.position + Vector3.up * 0.1f;
        visionConeObj.transform.rotation = transform.rotation;

        visionMeshFilter = visionConeObj.AddComponent<MeshFilter>();
        visionMeshRenderer = visionConeObj.AddComponent<MeshRenderer>();

        visionMesh = new Mesh();
        visionMesh.name = "VisionConeMesh";
        visionMeshFilter.mesh = visionMesh;

        Material coneMat = new Material(Shader.Find("Sprites/Default"));
        coneMat.color = normalColor;
        coneMat.renderQueue = 3000;
        
        visionMeshRenderer.material = coneMat;
        visionMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        visionMeshRenderer.receiveShadows = false;
    }

    private void UpdateVisionCone()
    {
        if (!showVisionCone || visionMesh == null || visionMeshFilter == null) return;

        visionMeshFilter.transform.position = transform.position + Vector3.up * 0.1f;
        visionMeshFilter.transform.rotation = transform.rotation;

        int segments = 30;
        int vertexCount = segments + 2;
        
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[segments * 3];

        bool canSee = CanSeePlayer();
        Color currentColor = canSee ? alertColor : normalColor;

        vertices[0] = Vector3.zero;

        float angleStep = viewAngle / segments;
        float startAngle = -viewAngle * 0.5f;

        int layerMask = ~LayerMask.GetMask("Guard", "Ignore Raycast");

        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector3 localDir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            
            float distance = viewDistance;
            Vector3 worldDir = transform.TransformDirection(localDir);
            Ray ray = new Ray(transform.position + Vector3.up * 0.5f, worldDir);
            
            if (Physics.Raycast(ray, out RaycastHit hit, viewDistance, layerMask))
            {
                if (hit.transform != player && hit.transform != transform)
                {
                    distance = hit.distance;
                }
            }

            vertices[i + 1] = localDir * distance;
        }

        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        visionMesh.Clear();
        visionMesh.vertices = vertices;
        visionMesh.triangles = triangles;
        visionMesh.RecalculateNormals();
        visionMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);

        if (visionMeshRenderer.material != null)
        {
            visionMeshRenderer.material.color = currentColor;
        }
    }

    private void OnDestroy()
    {
        if (visionMesh != null)
        {
            Destroy(visionMesh);
        }
        if (visionMeshFilter != null && visionMeshFilter.gameObject != null)
        {
            Destroy(visionMeshFilter.gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        DrawVisionCone();
        DrawPatrolPath();
    }

    private void OnDrawGizmosSelected()
    {
        DrawDetailedVision();
    }

    private void DrawVisionCone()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        
        Vector3 leftBoundary = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, viewAngle * 0.5f, 0) * transform.forward;

        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawRay(origin, leftBoundary * viewDistance);
        Gizmos.DrawRay(origin, rightBoundary * viewDistance);
        Gizmos.DrawRay(origin, transform.forward * viewDistance);

        int segments = 20;
        float angleStep = viewAngle / segments;
        Vector3 previousPoint = origin + leftBoundary * viewDistance;

        for (int i = 1; i <= segments; i++)
        {
            float angle = -viewAngle * 0.5f + angleStep * i;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
            Vector3 currentPoint = origin + direction * viewDistance;
            
            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }

        Gizmos.DrawLine(origin, origin + leftBoundary * viewDistance);
        Gizmos.DrawLine(origin, origin + rightBoundary * viewDistance);
    }

    private void DrawDetailedVision()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
        
        int rings = 5;
        int segments = 30;
        
        for (int r = 1; r <= rings; r++)
        {
            float radius = (viewDistance / rings) * r;
            float angleStep = viewAngle / segments;
            
            Vector3 previousPoint = origin + Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward * radius;
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = -viewAngle * 0.5f + angleStep * i;
                Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
                Vector3 currentPoint = origin + direction * radius;
                
                Gizmos.DrawLine(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }
        }

        if (player != null)
        {
            bool canSee = CanSeePlayer();
            Gizmos.color = canSee ? Color.red : Color.gray;
            Gizmos.DrawLine(origin, player.position + Vector3.up);
            Gizmos.DrawWireSphere(player.position + Vector3.up, 0.3f);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, catchDistance);
    }

    private void DrawPatrolPath()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        Gizmos.color = Color.cyan;
        
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null) continue;
            
            Vector3 point = patrolPoints[i].position;
            Gizmos.DrawWireSphere(point, 0.3f);
            
            int nextIndex = (i + 1) % patrolPoints.Length;
            if (patrolPoints[nextIndex] != null)
            {
                Gizmos.DrawLine(point, patrolPoints[nextIndex].position);
            }
        }

        if (Application.isPlaying)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, lastPatrolPosition);
            Gizmos.DrawWireSphere(lastPatrolPosition, 0.5f);
        }
    }
}
