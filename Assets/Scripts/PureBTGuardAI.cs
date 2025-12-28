using UnityEngine;
using UnityEngine.AI;
using BehaviorTreeSystem;

[RequireComponent(typeof(NavMeshAgent))]
public class PureBTGuardAI : MonoBehaviour
{
    [Header("Vision Settings")]
    [SerializeField] private float viewDistance = 10f;
    [SerializeField] private float viewAngle = 60f;

    [Header("Movement Settings")]
    [SerializeField] private float catchDistance = 1.5f;
    [SerializeField] private float waypointReachDistance = 0.5f;
    [SerializeField] private float searchDuration = 3f;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] patrolPoints;

    [Header("Debug")]
    [SerializeField] private bool enableDebug;

    [Header("In-Game Visuals")]
    [SerializeField] private bool showVisionCone = true;
    [SerializeField] private Material visionConeMaterial;
    [SerializeField] private Color normalColor = new Color(1f, 0.5f, 0f, 0.3f);
    [SerializeField] private Color alertColor = new Color(1f, 0f, 0f, 0.5f);
    [SerializeField] private Color searchColor = new Color(1f, 1f, 0f, 0.4f);

    private NavMeshAgent agent;
    private BehaviorTreeRunner treeRunner;
    
    private int currentPatrolIndex;
    private Vector3 lastKnownPlayerPosition;
    private float searchTimer;
    private bool hasLastKnownPosition;
    private bool alertSoundPlayed;

    private MeshFilter visionMeshFilter;
    private MeshRenderer visionMeshRenderer;
    private Mesh visionMesh;

    public float ViewDistance => viewDistance;
    public float ViewAngle => viewAngle;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        treeRunner = new BehaviorTreeRunner(BuildTree(), enableDebug);
        InitializeVisionCone();
    }

    private void Update()
    {
        treeRunner.Tick();
        UpdateVisionCone();
    }

    private BTNode BuildTree()
    {
        return new BehaviorTreeBuilder()
            .Selector("Root")
            
                .Sequence("CatchPlayer")
                    .Condition(IsPlayerInCatchRange, "InCatchRange")
                    .Action(CatchPlayer, "Catch")
                .End()
                
                .Sequence("ChaseBehavior")
                    .Condition(CanSeePlayer, "CanSee")
                    .Do(UpdateLastKnownPosition, "UpdatePosition")
                    .Do(ResetSearchTimer, "ResetTimer")
                    .Parallel(2, "ChaseActions")
                        .Action(MoveToPlayer, "MoveToPlayer")
                        .Action(PlayAlertSound, "AlertSound")
                    .End()
                .End()
                
                .Sequence("SearchBehavior")
                    .Condition(HasLastKnownPosition, "HasLastPos")
                    .Condition(IsSearching, "IsSearching")
                    .Parallel(2, "SearchActions")
                        .Action(MoveToLastKnownPosition, "MoveToLastPos")
                        .Action(UpdateSearchTimer, "UpdateTimer")
                    .End()
                .End()
                
                .Sequence("PatrolBehavior")
                    .Do(ResetAlertSound, "ResetAlert")
                    .Do(ClearLastKnownPosition, "ClearLastPos")
                    .Action(Patrol, "Patrol")
                .End()
                
            .End()
            .Build();
    }

    #region Conditions

    private bool CanSeePlayer()
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

        Ray ray = new Ray(transform.position + Vector3.up, directionToPlayer.normalized);
        if (Physics.Raycast(ray, out RaycastHit hit, viewDistance))
        {
            return hit.transform == player;
        }

        return true;
    }

    private bool IsPlayerInCatchRange()
    {
        if (player == null)
            return false;
        return Vector3.Distance(transform.position, player.position) < catchDistance;
    }

    private bool HasLastKnownPosition()
    {
        return hasLastKnownPosition;
    }

    private bool IsSearching()
    {
        return searchTimer > 0f;
    }

    #endregion

    #region Actions

    private NodeStatus MoveToPlayer()
    {
        if (player == null)
            return NodeStatus.Failure;

        agent.destination = player.position;

        if (enableDebug)
            Debug.Log("[PureBT] Moving to player");

        return NodeStatus.Success;
    }

    private NodeStatus MoveToLastKnownPosition()
    {
        agent.destination = lastKnownPlayerPosition;

        float distance = Vector3.Distance(transform.position, lastKnownPlayerPosition);
        
        if (enableDebug)
            Debug.Log($"[PureBT] Searching at last known position, distance: {distance:F1}");

        if (distance < waypointReachDistance)
            return NodeStatus.Success;

        return NodeStatus.Running;
    }

    private NodeStatus Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return NodeStatus.Failure;

        if (!agent.pathPending && agent.remainingDistance < waypointReachDistance)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            agent.destination = patrolPoints[currentPatrolIndex].position;

            if (enableDebug)
                Debug.Log($"[PureBT] Patrolling to point {currentPatrolIndex}");
        }

        return NodeStatus.Running;
    }

    private NodeStatus PlayAlertSound()
    {
        if (!alertSoundPlayed)
        {
            alertSoundPlayed = true;
            
            if (enableDebug)
                Debug.Log("[PureBT] ALERT! Player spotted!");
        }

        return NodeStatus.Success;
    }

    private NodeStatus UpdateSearchTimer()
    {
        searchTimer -= Time.deltaTime;

        if (enableDebug)
            Debug.Log($"[PureBT] Search time remaining: {searchTimer:F1}s");

        if (searchTimer <= 0f)
        {
            hasLastKnownPosition = false;
            return NodeStatus.Failure;
        }

        return NodeStatus.Success;
    }

    private NodeStatus CatchPlayer()
    {
        if (enableDebug)
            Debug.Log("[PureBT] Player caught!");

        GameManager.Instance.SetGameState(GameManager.GameState.Fail);
        return NodeStatus.Success;
    }

    #endregion

    #region Helper Actions

    private void UpdateLastKnownPosition()
    {
        lastKnownPlayerPosition = player.position;
        hasLastKnownPosition = true;
    }

    private void ResetSearchTimer()
    {
        searchTimer = searchDuration;
    }

    private void ResetAlertSound()
    {
        alertSoundPlayed = false;
    }

    private void ClearLastKnownPosition()
    {
        hasLastKnownPosition = false;
    }

    #endregion

    #region In-Game Vision Cone

    private void InitializeVisionCone()
    {
        if (!showVisionCone) return;

        GameObject visionConeObj = new GameObject("VisionCone");
        visionConeObj.transform.SetParent(transform, false);
        visionConeObj.transform.localPosition = Vector3.up * 0.2f;
        visionConeObj.transform.localRotation = Quaternion.identity;
        visionConeObj.transform.localScale = Vector3.one;

        visionMeshFilter = visionConeObj.AddComponent<MeshFilter>();
        visionMeshRenderer = visionConeObj.AddComponent<MeshRenderer>();

        visionMesh = new Mesh();
        visionMesh.name = "VisionConeMesh";
        visionMeshFilter.mesh = visionMesh;

        Material coneMat = null;
        
        Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (urpUnlit != null)
        {
            coneMat = new Material(urpUnlit);
            coneMat.SetFloat("_Surface", 1);
            coneMat.SetFloat("_Blend", 0);
            coneMat.SetFloat("_Cull", 0);
            coneMat.SetFloat("_ZWrite", 0);
            coneMat.SetFloat("_AlphaClip", 0);
            coneMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            coneMat.SetOverrideTag("RenderType", "Transparent");
            coneMat.renderQueue = 3000;
        }
        else
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader != null)
            {
                coneMat = new Material(litShader);
                coneMat.SetFloat("_Surface", 1);
                coneMat.SetFloat("_Cull", 0);
                coneMat.renderQueue = 3000;
            }
            else
            {
                coneMat = new Material(Shader.Find("Sprites/Default"));
            }
        }
        
        coneMat.color = normalColor;
        visionMeshRenderer.material = coneMat;
        visionMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        visionMeshRenderer.receiveShadows = false;
    }

    private void UpdateVisionCone()
    {
        if (!showVisionCone || visionMesh == null) return;

        int segments = 30;
        int vertexCount = segments + 2;
        
        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        int[] triangles = new int[segments * 3];

        bool canSee = CanSeePlayer();
        Color currentColor;
        
        if (canSee)
            currentColor = alertColor;
        else if (hasLastKnownPosition && searchTimer > 0)
            currentColor = searchColor;
        else
            currentColor = normalColor;

        vertices[0] = Vector3.zero;
        normals[0] = Vector3.up;

        float angleStep = viewAngle / segments;
        float startAngle = -viewAngle * 0.5f;

        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            
            float distance = viewDistance;
            Ray ray = new Ray(transform.position + Vector3.up * 0.5f, transform.TransformDirection(direction));
            if (Physics.Raycast(ray, out RaycastHit hit, viewDistance))
            {
                if (hit.transform != player)
                {
                    distance = hit.distance;
                }
            }

            vertices[i + 1] = direction * distance;
            normals[i + 1] = Vector3.up;
        }

        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        visionMesh.Clear();
        visionMesh.vertices = vertices;
        visionMesh.normals = normals;
        visionMesh.triangles = triangles;
        visionMesh.RecalculateBounds();

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
    }

    #endregion

    #region Gizmos

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

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
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

        if (hasLastKnownPosition)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(lastKnownPlayerPosition, 0.5f);
            Gizmos.DrawLine(transform.position, lastKnownPlayerPosition);
        }
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

        if (Application.isPlaying && agent != null && agent.hasPath)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, agent.destination);
            Gizmos.DrawWireSphere(agent.destination, 0.4f);
        }
    }

    #endregion
}
