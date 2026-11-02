using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum ZombieState { Inactive, Active}
public class ZombieController : MonoBehaviour
{
    public float activationRange;
    public float interestLossDistance;
    public float attackRange;
    public float attackSpeed;
    public float attackDamage;
    public float maxHealth;

    public LayerMask hittableLayer;
    public LayerMask obstacleLayer;

    private float health;

    private GameObject currentTarget;
    private ZombieState currentState;
    private NavMeshAgent agent;
    private List<GameObject> targetsInView;
    private Collider ownCollider;

    private float hitDelay;

    private void Awake()
    {
        currentState = ZombieState.Inactive;
        agent = GetComponent<NavMeshAgent>();
        targetsInView = new List<GameObject>();
        ownCollider = GetComponent<Collider>();
        health = maxHealth;
    }

    public void ScanEnvironment()
    {
        targetsInView.Clear();

        Collider[] itemColliders = Physics.OverlapSphere(transform.position, activationRange, hittableLayer);

        GetTargetsInView(itemColliders, targetsInView);
    }

    private void GetTargetsInView(Collider[] colliders, List<GameObject> targetList)
    {
        foreach (Collider collider in colliders)
        {
            if (collider.gameObject.transform.IsChildOf(transform))
                continue;

            if (!collider.gameObject.CompareTag("Human"))
                continue;

            if (IsInView(collider) && !targetList.Contains(collider.gameObject))
            {
                targetList.Add(collider.gameObject);
            }
        }
    }
    private bool IsInView(Collider target)
    {
        return VisionCheck.Evaluate(ownCollider.bounds.center, transform.forward, target, 360f, obstacleLayer) == VisionResult.Visible;
    }

    private void FixedUpdate()
    {
        hitDelay -= Time.deltaTime;

        if (currentState == ZombieState.Inactive)
        {
            ScanEnvironment();
            
            if(targetsInView.Count == 0)
                return;

            if(currentTarget == null)
                currentTarget = targetsInView[0];
        }
        if (currentTarget != null)
            agent.SetDestination(currentTarget.transform.position);
        else
        {
            LoseInterest();
            return;
        }

        if (Vector3.Distance(transform.position, currentTarget.transform.position) <= attackRange)
        {
            agent.isStopped = true;
            AttackTarget();
        }
        else if(agent.isStopped)
            agent.isStopped = false;

        if (Vector3.Distance(transform.position, currentTarget.transform.position) >= interestLossDistance)
            LoseInterest();

    }

    private void LoseInterest()
    {
        agent.ResetPath();
        currentState = ZombieState.Inactive;
        currentTarget = null;
    }

    private void AttackTarget()
    {
        if(hitDelay <= 0)
        {
            hitDelay = attackSpeed;
            currentTarget.GetComponent<StatsHandler>().DealDamage(attackDamage, gameObject);
        }
    }

    public void DealDamage(float incomingDamage)
    {
        health -= incomingDamage;

        if (health <= 0)
            Destroy(gameObject);
    }
}
