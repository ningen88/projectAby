using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ObstacleAgent : MonoBehaviour
{
    private NavMeshAgent agent;
    private NavMeshObstacle obstacle;
    private Vector3 lastPosition;
    private float lastMoveTime;
    [SerializeField] float CarvingTime = 0.5f;
    [SerializeField] float CarvingMoveTresh = 0.1f;

    private void Awake()
    {
        agent = gameObject.GetComponent<NavMeshAgent>();
        obstacle = gameObject.GetComponent<NavMeshObstacle>();
        obstacle.enabled = false;
        obstacle.carveOnlyStationary = false;
        lastPosition = gameObject.transform.position;
    }

    void Update()
    {
        float dist = Vector3.Distance(lastPosition, gameObject.transform.position);
        if (dist > CarvingMoveTresh)
        {
            lastMoveTime = Time.time;
            lastPosition = gameObject.transform.position;
        }
        if (lastMoveTime + CarvingTime < Time.time)
        {
            agent.enabled = false;
            obstacle.enabled = true;
        }
    }

    public void SetDestination(Vector3 position)
    {
        obstacle.enabled = false;
        lastMoveTime = Time.time;
        lastPosition = gameObject.transform.position;

        StartCoroutine(MoveAgent(position));
    }

    private IEnumerator MoveAgent(Vector3 position)
    {
        yield return null;

        agent.enabled = true;
        agent.SetDestination(position);
    }

    public float GetRemainingDistance()
    {
        return agent.remainingDistance;
    }

    public float ObjectiveDistance(Vector3 objectivePosition)
    {
        return Vector3.Distance(agent.transform.position, objectivePosition);
    }

    public bool ArrivedAtDestination(Vector3 objectivePosition)
    {
        if (ObjectiveDistance(objectivePosition) == 0) return true;
        return false;
    }
}
