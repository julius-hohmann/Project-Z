using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum VisionResult { Visible, OutOfCone, Blocked }

public static class VisionCheck
{
    //Checks against the center of the target's collider, the cone only considers the horizontal direction
    public static VisionResult Evaluate(Vector3 origin, Vector3 forward, Collider target, float viewAngle, LayerMask obstacles)
    {
        Vector3 targetPoint = target.bounds.center;
        Vector3 flatDirection = Vector3.ProjectOnPlane(targetPoint - origin, Vector3.up);

        if (flatDirection.sqrMagnitude > 0.001f && Vector3.Angle(Vector3.ProjectOnPlane(forward, Vector3.up), flatDirection) > viewAngle / 2)
            return VisionResult.OutOfCone;

        return Physics.Linecast(origin, targetPoint, obstacles) ? VisionResult.Blocked : VisionResult.Visible;
    }

    public static void DrawGizmo(Vector3 origin, Vector3 targetPoint, VisionResult result)
    {
        switch (result)
        {
            case VisionResult.Visible:
                Gizmos.color = Color.green;
                break;
            case VisionResult.Blocked:
                Gizmos.color = Color.red;
                break;
            default:
                Gizmos.color = Color.gray;
                break;
        }
        Gizmos.DrawLine(origin, targetPoint);
    }
}

public class SensingHandler : MonoBehaviour
{
    public float visionRange;
    public float visionAngle;

    public float scanFrequency;
    private float scanInterval;
    private float scanTimer;

    public float itemMemoryDuration = 5f;

    public LayerMask itemLayer;
    public LayerMask obstacleLayer;

    private Transform eyesPoint;

    public List<GameObject> ItemsInView
    {
        get
        {
            itemsInView.RemoveAll(obj => !obj);
            return itemsInView;
        }
    }
    private List<GameObject> itemsInView;

    //Items seen within the last itemMemoryDuration seconds
    public List<GameObject> KnownItems
    {
        get
        {
            knownItems.Clear();
            foreach (KeyValuePair<GameObject, float> item in itemMemory)
            {
                if (item.Key && Time.time - item.Value <= itemMemoryDuration)
                    knownItems.Add(item.Key);
            }
            return knownItems;
        }
    }
    private List<GameObject> knownItems;
    private Dictionary<GameObject, float> itemMemory;
    private Dictionary<GameObject, float> ignoredItems;

    private List<(Vector3 point, VisionResult result)> lastScanResults;


    private void Awake()
    {
        itemsInView = new List<GameObject>();
        knownItems = new List<GameObject>();
        itemMemory = new Dictionary<GameObject, float>();
        ignoredItems = new Dictionary<GameObject, float>();
        lastScanResults = new List<(Vector3 point, VisionResult result)>();
        scanInterval = 1f / scanFrequency;

        NPCController npc = GetComponent<NPCController>();
        if (npc != null)
            eyesPoint = npc.eyesPoint;
    }

    private void FixedUpdate()
    {
        scanTimer -= Time.deltaTime;
        if(scanTimer < 0)
        {
            scanTimer += scanInterval;
            ScanEnvironment();
        }
    }
    public void ScanEnvironment()
    {
        itemsInView.Clear();
        lastScanResults.Clear();

        Collider[] itemColliders = Physics.OverlapSphere(transform.position, visionRange, itemLayer);

        GetTargetsInView(itemColliders, itemsInView);

        ForgetOldItems();
    }

    //Stops an item from being sensed for a while, e.g. because the agent reached it but couldn't pick it up
    public void IgnoreItem(GameObject item, float duration)
    {
        ignoredItems[item] = Time.time + duration;
        itemMemory.Remove(item);
        itemsInView.Remove(item);
    }

    private void GetTargetsInView(Collider[] colliders, List<GameObject> targetList)
    {
        foreach (Collider collider in colliders)
        {
            if (collider.gameObject.transform.IsChildOf(transform))
                continue;

            if (ignoredItems.TryGetValue(collider.gameObject, out float ignoredUntil) && Time.time < ignoredUntil)
                continue;

            VisionResult result = VisionCheck.Evaluate(GetEyesPosition(), transform.forward, collider, visionAngle, obstacleLayer);
            lastScanResults.Add((collider.bounds.center, result));

            if (result == VisionResult.Visible && !targetList.Contains(collider.gameObject))
            {
                targetList.Add(collider.gameObject);
                itemMemory[collider.gameObject] = Time.time;
            }
        }
    }

    private void ForgetOldItems()
    {
        List<GameObject> oldItems = new List<GameObject>();
        foreach (KeyValuePair<GameObject, float> item in itemMemory)
        {
            if (!item.Key || Time.time - item.Value > itemMemoryDuration)
                oldItems.Add(item.Key);
        }
        foreach (GameObject item in oldItems)
        {
            itemMemory.Remove(item);
        }

        oldItems.Clear();
        foreach (KeyValuePair<GameObject, float> item in ignoredItems)
        {
            if (!item.Key || Time.time >= item.Value)
                oldItems.Add(item.Key);
        }
        foreach (GameObject item in oldItems)
        {
            ignoredItems.Remove(item);
        }
    }

    private Vector3 GetEyesPosition()
    {
        return eyesPoint != null ? eyesPoint.position : transform.position + Vector3.up;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }

    //Green = visible, red = blocked by an obstacle, gray = outside of the vision cone
    void OnDrawGizmosSelected()
    {
        if (lastScanResults == null)
            return;

        foreach ((Vector3 point, VisionResult result) in lastScanResults)
        {
            VisionCheck.DrawGizmo(GetEyesPosition(), point, result);
        }
    }
}
