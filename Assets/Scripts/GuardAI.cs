using UnityEngine;
using UnityEngine.AI;

public enum GuardState
{
    Patrol,
    Chase,
    ReturnToPatrol
}

public class GuardAI : MonoBehaviour
{
    [Header("Görsel Ayarlar")]
    public float viewDistance = 10f;
    public float viewAngle = 60f;

    [Header("Referanslar")]
    public Transform player;
    public Transform[] patrolPoints;

    private NavMeshAgent agent;
    private BehaviorTree behaviorTree;

    private int currentPatrolIndex = 0;
    private Vector3 lastPatrolPosition; 

    public GuardState CurrentState { get; private set; }

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        behaviorTree = new BehaviorTree(this);

        CurrentState = GuardState.Patrol;
        GoToNextPatrolPoint();
    }

    private void Update()
    {
        // 1. Kararý BT verir
        GuardState decision = behaviorTree.Evaluate();

        // 2. FSM güncellenir
        if (decision != CurrentState)
        {
            CurrentState = decision;
        }

        // 3. FSM davranýþý yürütür
        switch (CurrentState)
        {
            case GuardState.Patrol:
                Patrol();
                break;

            case GuardState.Chase:
                Chase();
                break;

            case GuardState.ReturnToPatrol:
                ReturnToPatrol();
                break;
        }
    }

    // FSM davranýþlarý
    void Patrol()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            GoToNextPatrolPoint();
        }
    }

    void GoToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;

        agent.destination = patrolPoints[currentPatrolIndex].position;
        lastPatrolPosition = patrolPoints[currentPatrolIndex].position;
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    void Chase()
    {
        agent.destination = player.position;

        if (Vector3.Distance(transform.position, player.position) < 1.5f)
        {
            GameManager.Instance.SetGameState(GameManager.GameState.Fail);
        }
    }

    void ReturnToPatrol()
    {
        agent.destination = lastPatrolPosition;

        if (Vector3.Distance(transform.position, lastPatrolPosition) < 0.5f)
        {
            CurrentState = GuardState.Patrol;
        }
    }

    // Oyuncuyu görme algoritmasý
    public bool CanSeePlayer()
    {
        Vector3 dirToPlayer = player.position - transform.position;
        float distance = dirToPlayer.magnitude;

        if (distance > viewDistance) return false;

        float angle = Vector3.Angle(transform.forward, dirToPlayer.normalized);
        if (angle > viewAngle / 2f) return false;

        Ray ray = new Ray(transform.position + Vector3.up, dirToPlayer.normalized);
        if (Physics.Raycast(ray, out RaycastHit hit, viewDistance))
        {
            if (hit.transform != player) return false;
        }

        return true;
    }
}
