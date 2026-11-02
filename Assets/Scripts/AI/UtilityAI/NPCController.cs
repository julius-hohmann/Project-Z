using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPCController : MonoBehaviour
{
    public MovementController movementController;
    public AIBrain aiBrain;
    public List<Action> availableActions;
    public Inventory inventory;
    public WeaponHandler weaponHandler;
    public StatsHandler stats;
    public EventHandler eventHandler;
    public EquipmentHandler equipmentHandler;
    public SensingHandler sensingHandler;
    public POIHandler poiHandler;
    public PlayerTypeHandler playerTypeHandler;
    public ThreatHandler threatHandler;
    public GroupHandler groupHandler;

    public Transform eyesPoint;
    public LayerMask visionLayers;

    public float maxActionDuration = 30f;
    public float ignoreItemDuration = 30f;
    public float fleeDistance = 10f;
    public float minFleeDistanceGain = 2f;
    public float maxFleeDuration = 5f;
    private float actionStartTime;

    public bool HasItemToCollect
    {
        get { return sensingHandler.KnownItems.Count > 0 && inventory.slots.Count < inventory.maxSlots; }
    }

    private void Awake()
    {
        movementController = GetComponent<MovementController>();
        aiBrain = GetComponent<AIBrain>();
        inventory = GetComponent<Inventory>();
        weaponHandler = GetComponent<WeaponHandler>();
        stats = GetComponent<StatsHandler>();
        eventHandler = GetComponent<EventHandler>();
        equipmentHandler = GetComponent<EquipmentHandler>();
        sensingHandler = GetComponent<SensingHandler>();
        poiHandler = GetComponent<POIHandler>();
        playerTypeHandler = GetComponent<PlayerTypeHandler>();
        threatHandler = GetComponent<ThreatHandler>();
        groupHandler = GetComponent<GroupHandler>();
    }
    private void Update()
    {
        if (aiBrain.finishedDeciding)
        {
            aiBrain.finishedDeciding = false;
            actionStartTime = Time.time;
            aiBrain.bestAction.Execute(this);
        }
        else if (aiBrain.bestAction != null && Time.time - actionStartTime > maxActionDuration)
        {
            //Stuck actions must not block the agent forever
            actionStartTime = Time.time;
            StopAllCoroutines();
            OnFinishedAction();
        }
    }

    public void OnFinishedAction()
    {
        aiBrain.DecideBestAction(availableActions);
    }

    private void StartAction(IEnumerator action)
    {
        StartCoroutine(RunAction(action));
    }

    //Exceptions end a coroutine without finishing the action, so the agent has to decide again here
    private IEnumerator RunAction(IEnumerator action)
    {
        while (true)
        {
            try
            {
                if (!action.MoveNext())
                    yield break;
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, this);
                OnFinishedAction();
                yield break;
            }
            yield return action.Current;
        }
    }

    #region Actions
    internal void Drink(int time)
    {
        StartAction(DrinkCoroutine(time));
    }

    internal void Heal(int time)
    {
        StartAction(HealCoroutine(time));
    }

    public void Eat(int time)
    {
        StartAction(EatCoroutine(time));
    }
    internal void SearchForItems()
    {
        StartAction(SearchItemsCoroutine(0.5f));
    }
    public void EquipArmor(EquipmentType type)
    {
        StartAction(EquipArmorCoroutine(1, type));
    }
    public void EquipWeapon(WeaponType type)
    {
        StartAction(EquipWeaponCoroutine(1, type));
    }
    public void ReloadWeapon()
    {
        if (weaponHandler.currentWeaponData != null)
            StartAction(ReloadWeaponCoroutine(weaponHandler.currentWeaponData.reloadTime));
        else
            Debug.Log(stats.Name + " tries to reload, but has no weapon equipped");
    }
    public void ExploreArea(PointOfInterest poi)
    {
        StartAction(ExploreAreaCoroutine(poi, 2f));
    }
    public void MoveAroundArea()
    {
        StartAction(MoveAroundAreaCoroutine(1f));
    }
    public void Flee()
    {
        StartAction(FleeCoroutine(0.01f));
    }
    public void Shoot(string tag)
    {
        StartAction(ShootCoroutine(tag));
    }
    public void InviteToGroup()
    {
        StartAction(InviteToGroupCoroutine(1f));
    }
    public void AcceptInvitation()
    {
        StartAction(AcceptInvitationCoroutine(1f));
    }
    public void DeclineInvitation()
    {
        StartAction(DeclineInvitationCoroutine(1f));
    }
    public void FollowLeader()
    {
        StartAction(FollowLeaderCoroutine(0.5f));
    }
    public void BetrayGroup()
    {
        StartAction(BetrayGroupCoroutine());
    }
    public void ChaseTarget()
    {
        StartAction(ChaseTargetCoroutine(1f));
    }

    #endregion

    #region Coroutines
    private IEnumerator EatCoroutine(float time)
    {
        if (!inventory.HasConsumable(ConsumableType.Food))
            Debug.Log(stats.Name + " wants to eat, but has no food");
        else
        {
            yield return new WaitForSeconds(time);

            inventory.UseItem(ConsumableType.Food);

            Debug.Log("I just ate some food");
        }
        OnFinishedAction();
    }
    private IEnumerator DrinkCoroutine(float time)
    {
        if (!inventory.HasConsumable(ConsumableType.Water))
            Debug.Log(stats.Name + " wants to drink, but has no water");
        else
        {
            yield return new WaitForSeconds(time);

            inventory.UseItem(ConsumableType.Water);

            Debug.Log("I just drank some water");
        }
        OnFinishedAction();
    }
    private IEnumerator HealCoroutine(float time)
    {
        if (!inventory.HasConsumable(ConsumableType.Healing))
            Debug.Log(stats.Name + " wants to heal, but has no healing items");
        else
        {
            yield return new WaitForSeconds(time);

            inventory.UseItem(ConsumableType.Healing);

            Debug.Log("I just healed my injuries");
        }
        OnFinishedAction();
    }
    private IEnumerator SearchItemsCoroutine(float time)
    {
        //Debug.Log("I am searching for Items");

        float distance = Mathf.Infinity;
        GameObject closestItem = null;

        foreach (GameObject item in sensingHandler.KnownItems)
        {
            float tempDistance = Vector3.Distance(transform.position, item.transform.position);
            if (tempDistance < distance)
            {
                closestItem = item;
                distance = tempDistance;
            }
        }

        if (closestItem != null)
        {
            movementController.MoveTo(closestItem.transform.position);

            //Keep walking to the item for a while, even if it leaves the vision cone
            float elapsedTime = 0f;
            while (closestItem != null && elapsedTime < time)
            {
                yield return null;
                elapsedTime += Time.deltaTime;

                //Items get collected on contact, so reaching one that still exists means it can't be picked up
                if (closestItem != null && !movementController.agent.pathPending && movementController.agent.remainingDistance <= 0.5f)
                {
                    sensingHandler.IgnoreItem(closestItem, ignoreItemDuration);
                    break;
                }
            }
        }
        else
            yield return null;

        OnFinishedAction();
    }
    private IEnumerator EquipArmorCoroutine(int time, EquipmentType type)
    {
        if (!inventory.HasEquipment(type))
            Debug.Log(stats.Name + " wants to equip " + type.ToString() + " but doesn't have it in it's inventory");
        else
        {
            yield return new WaitForSeconds(time);

            equipmentHandler.EquipItem(type);

            Debug.Log(stats.Name + " equipped a " + type.ToString());
        }
        OnFinishedAction();
    }
    private IEnumerator EquipWeaponCoroutine(int time, WeaponType type)
    {
        Weapon weaponToEquip = inventory.HasWeapon(type);
        if (weaponToEquip == null)
            Debug.Log(stats.Name + " wants to equip " + type.ToString() + " but doesn't have it in it's inventory");
        else
        {
            yield return new WaitForSeconds(time);

            equipmentHandler.EquipItem(type);
        }
        OnFinishedAction();
    }
    private IEnumerator ReloadWeaponCoroutine(float time)
    {
        yield return new WaitForSeconds(time);

        weaponHandler.ReloadWeapon();

        OnFinishedAction();
    }
    private IEnumerator MoveAroundAreaCoroutine(float time)
    {
        float waitTime = time;
        Vector3 randomPosition = GetRandomPoint();
        if (randomPosition != Vector3.zero)
        {
            movementController.MoveTo(randomPosition);
            while (movementController.agent.pathPending || movementController.agent.remainingDistance > 0.5f)
            {
                yield return null;
                if (HasItemToCollect || threatHandler.VisibleEnemies.Count > 0)
                {
                    waitTime = 0f;
                    break;
                }
            }
            yield return new WaitForSeconds(waitTime);
        }
        OnFinishedAction();
    }
    private Vector3 GetRandomPoint()
    {
        PointOfInterest poi = poiHandler.currentArea;
        for(int i = 0; i < 10;  i++)
        {
            float randomX = Random.Range(poi.transform.position.x + poi.areaCenter.x - poi.areaSize.x / 2, poi.transform.position.x + poi.areaCenter.x + poi.areaSize.x / 2);
            float randomZ = Random.Range(poi.transform.position.z + poi.areaCenter.z - poi.areaSize.z / 2, poi.transform.position.z + poi.areaCenter.z + poi.areaSize.z / 2);
            float randomY = Random.Range(poi.transform.position.y + poi.areaCenter.y - poi.areaSize.y / 2, poi.transform.position.y + poi.areaCenter.y + poi.areaSize.y / 2);

            Vector3 randomPoint = new Vector3(randomX, randomY, randomZ);
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, poi.areaSize.y / 2, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        return Vector3.zero;
    }

    private IEnumerator ExploreAreaCoroutine(PointOfInterest poi, float time)
    {
        eventHandler.UpdateDestination(poi);

        if (poi != null)
        {
            movementController.MoveTo(poi.transform.position);

            bool interrupted = false;

            while (movementController.agent.pathPending || movementController.agent.remainingDistance > 0.5f)
            {
                yield return null;

                if (poiHandler.currentArea == poi || HasItemToCollect || threatHandler.VisibleEnemies.Count > 0)
                {
                    time = 0f;
                    interrupted = true;
                    break;
                }
            }

            //The path ended without entering the area, so it can't be reached from here
            if (!interrupted && poiHandler.currentArea != poi)
                poiHandler.MarkAreaAsVisited(poi);
        }
        yield return new WaitForSeconds(time);

        OnFinishedAction();
    }

    private IEnumerator FleeCoroutine(float time)
    {
        GameObject enemy = GetEnemyToFleeFrom();

        if(enemy != null)
        {
            if (TryGetFleePosition(enemy.transform.position, out Vector3 fleePosition))
            {
                movementController.MoveTo(fleePosition);

                float elapsedTime = 0f;
                while (elapsedTime < maxFleeDuration && (movementController.agent.pathPending || movementController.agent.remainingDistance > 0.5f))
                {
                    yield return null;
                    elapsedTime += Time.deltaTime;
                }
            }
            else if (weaponHandler.currentWeapon != null && weaponHandler.CurrentAmmo > 0)
            {
                //Cornered, so the only option left is to fight back
                StartAction(ShootCoroutine(enemy.tag));
                yield break;
            }
            else
                time = 1f;
        }

        yield return new WaitForSeconds(time);

        OnFinishedAction();
    }

    private GameObject GetEnemyToFleeFrom()
    {
        GameObject attacker = stats.recentlyAttackedBy;
        if (attacker != null && threatHandler.DetectedEnemies.Contains(attacker))
            return attacker;

        GameObject closestVisible = threatHandler.GetClosestInVision();
        if (closestVisible != null)
            return closestVisible;

        return threatHandler.GetClosestDetected();
    }

    //Prefers running straight away from the enemy, other directions are used if that way is blocked
    private bool TryGetFleePosition(Vector3 enemyPosition, out Vector3 fleePosition)
    {
        float[] angles = { 0f, 45f, -45f, 90f, -90f, 135f, -135f };

        Vector3 awayFromEnemy = Vector3.ProjectOnPlane(transform.position - enemyPosition, Vector3.up).normalized;
        float currentDistance = Vector3.Distance(transform.position, enemyPosition);
        NavMeshPath path = new NavMeshPath();

        foreach (float angle in angles)
        {
            Vector3 candidate = transform.position + Quaternion.Euler(0, angle, 0) * awayFromEnemy * fleeDistance;

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                continue;

            if (Vector3.Distance(hit.position, enemyPosition) < currentDistance + minFleeDistanceGain)
                continue;

            if (movementController.agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                fleePosition = hit.position;
                return true;
            }
        }

        fleePosition = Vector3.zero;
        return false;
    }

    private IEnumerator ShootCoroutine(string tag)
    {
        eventHandler.ChangeMovementState(false);

        GameObject newTarget = threatHandler.GetClosestInVision(tag);

        if(newTarget != null)
        {
            FaceTarget(newTarget.transform.position);

            threatHandler.currentTarget = newTarget;
        }

        if (HasLineOfSight(newTarget))
            weaponHandler.Shoot();

        yield return null;

        OnFinishedAction();
    }

    private IEnumerator BetrayGroupCoroutine()
    {
        eventHandler.ChangeMovementState(false);

        GameObject newTarget = threatHandler.GetClosestGroupMemberInVision();

        if (newTarget != null)
        {
            FaceTarget(newTarget.transform.position);

            //After the first shot the former group members are regular targets
            if (HasLineOfSight(newTarget))
            {
                threatHandler.currentTarget = newTarget;
                threatHandler.enemyMemory[newTarget] = newTarget.transform.position;

                groupHandler.BetrayGroup();
                weaponHandler.Shoot();
            }
        }

        yield return null;

        OnFinishedAction();
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 30f);
    }

    private bool HasLineOfSight(GameObject target)
    {
        if (Physics.Raycast(eyesPoint.position, transform.forward, out RaycastHit hit, threatHandler.detectionRadius, visionLayers))
            return hit.collider.gameObject == target;
        return false;
    }
    
    private IEnumerator InviteToGroupCoroutine(float time)
    {
        yield return new WaitForSeconds(time);

        GameObject go = threatHandler.GetClosestInVision("Human");

        NPCController target = null;

        if (go != null)
            target = go.GetComponent<NPCController>();

        bool canInvite = groupHandler.currentGroup == null || groupHandler.currentGroup.groupLeader == this;

        if(target != null && canInvite)
        {
            //Remembered even if the target can't be invited, otherwise the agent would keep trying
            groupHandler.RecentlyInvited.Add(go);

            if (target.groupHandler.CanJoinGroup())
            {
                if(groupHandler.currentGroup == null)
                    new Group(this);

                target.groupHandler.InviteToGroup(groupHandler.currentGroup);
            }
        }

        OnFinishedAction();
    }

    private IEnumerator AcceptInvitationCoroutine(float time)
    {
        yield return new WaitForSeconds(time);

        if (groupHandler.isInvited)
        {
            groupHandler.AcceptGroupInvitation();
        }

        OnFinishedAction();
    }
    private IEnumerator DeclineInvitationCoroutine(float time)
    {
        yield return new WaitForSeconds(time);

        if (groupHandler.isInvited)
        {
            groupHandler.DeclineGroupInvitation();
        }

        OnFinishedAction();
    }

    private IEnumerator FollowLeaderCoroutine(float time)
    {
        Group group = groupHandler.currentGroup;

        if (group != null && group.groupLeader != null)
        {
            Vector3 position = group.GetFormationPosition(this);

            if (!NavMesh.SamplePosition(position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                hit.position = group.groupLeader.transform.position;

            movementController.MoveTo(hit.position);
        }

        //The action only runs shortly, so the path gets updated and the agent can react to new situations
        yield return new WaitForSeconds(time);

        OnFinishedAction();
    }

    private IEnumerator ChaseTargetCoroutine(float time)
    {
        float waitTime = time;
        Vector3? lastKnownPosition = null;

        if (threatHandler.currentTarget != null)
            lastKnownPosition = threatHandler.GetLastKnownPosition(threatHandler.currentTarget);

        if (lastKnownPosition == null)
            threatHandler.currentTarget = null;
        else
        {
            movementController.MoveTo(lastKnownPosition.Value);
            while (movementController.agent.pathPending || movementController.agent.remainingDistance > 0.5f)
            {
                yield return null;
                if (HasItemToCollect || threatHandler.VisibleEnemies.Count > 0)
                {
                    waitTime = 0f;
                    break;
                }
            }
            threatHandler.currentTarget = null;
            yield return new WaitForSeconds(waitTime);
        }
        OnFinishedAction();
    }
    #endregion
}
