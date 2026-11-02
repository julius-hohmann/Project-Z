using AYellowpaper.SerializedCollections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThreatHandler : MonoBehaviour
{
    public float detectionRadius;
    public LayerMask hittablesLayer;
    public LayerMask obstacleLayer;

    public float scanFrequency;
    private float scanInterval;
    private float scanTimer;

    public float memoryDuration;
    public GameObject currentTarget;

    [SerializeField] public SerializedDictionary<GameObject, Vector3> enemyMemory { get; private set; }
    private List<GameObject> visibleEnemies = new List<GameObject>();
    private List<GameObject> detectedEnemies = new List<GameObject>();
    private List<GameObject> visibleGroupMembers = new List<GameObject>();
    private List<(Vector3 point, VisionResult result)> lastScanResults = new List<(Vector3 point, VisionResult result)>();

    private Transform eyesPoint;

    public List<GameObject> VisibleEnemies
    {
        get
        {
            visibleEnemies.RemoveAll(obj => !obj);
            return visibleEnemies;
        }
    }
    public List<GameObject> DetectedEnemies
    {
        get
        {
            detectedEnemies.RemoveAll(obj => !obj);
            return detectedEnemies;
        }
    }

    private void Awake()
    {
        scanInterval = 1f / scanFrequency;

        enemyMemory = new SerializedDictionary<GameObject, Vector3>();

        NPCController npc = GetComponent<NPCController>();
        if (npc != null)
            eyesPoint = npc.eyesPoint;
    }

    private void FixedUpdate()
    {
        scanTimer -= Time.deltaTime;
        if (scanTimer < 0)
        {
            scanTimer += scanInterval;
            ScanEnvironment();
        }
    }

    private void ScanEnvironment()
    {
        Collider[] enemyColliders = Physics.OverlapSphere(transform.position, detectionRadius, hittablesLayer);
        GetTargetsInView(enemyColliders);
    }

    private void GetTargetsInView(Collider[] enemyColliders)
    {
        List<GameObject> currentlyVisible = new List<GameObject>();
        HashSet<GameObject> currentlyDetected = new HashSet<GameObject>();
        List<GameObject> currentlyVisibleGroupMembers = new List<GameObject>();
        lastScanResults.Clear();

        foreach (Collider collider in enemyColliders)
        {
            GameObject enemy = collider.gameObject;
            if (enemy.transform.IsChildOf(transform)) continue;

            NPCController thisNPC = GetComponent<NPCController>();
            NPCController otherNPC = enemy.GetComponent<NPCController>();

            if (thisNPC != null && otherNPC != null)
            {
                Group myGroup = thisNPC.groupHandler?.currentGroup;
                if (myGroup != null && myGroup.GroupMembers.Contains(otherNPC))
                {
                    if (VisionCheck.Evaluate(GetEyesPosition(), transform.forward, collider, 360f, obstacleLayer) == VisionResult.Visible)
                        currentlyVisibleGroupMembers.Add(enemy);

                    continue;
                }
            }

            currentlyDetected.Add(enemy);

            VisionResult result = VisionCheck.Evaluate(GetEyesPosition(), transform.forward, collider, 360f, obstacleLayer);
            lastScanResults.Add((collider.bounds.center, result));

            if (result == VisionResult.Visible)
            {
                currentlyVisible.Add(enemy);
                if (!visibleEnemies.Contains(enemy))
                {
                    visibleEnemies.Add(enemy);
                }

                enemyMemory[enemy] = enemy.transform.position;
            }
        }

        foreach (GameObject enemy in visibleEnemies)
        {
            if (!currentlyVisible.Contains(enemy))
            {
                StartCoroutine(RememberEnemyPosition(enemy));
            }
        }

        visibleEnemies = currentlyVisible;
        visibleGroupMembers = currentlyVisibleGroupMembers;

        List<GameObject> outOfRangeEnemies = new List<GameObject>();
        foreach (GameObject enemy in detectedEnemies)
        {
            if (!currentlyDetected.Contains(enemy))
            {
                outOfRangeEnemies.Add(enemy);
            }
        }

        foreach (GameObject enemy in outOfRangeEnemies)
        {
            StartCoroutine(RememberEnemyPosition(enemy));
        }

        detectedEnemies = new List<GameObject>(currentlyDetected);
    }

    private Vector3 GetEyesPosition()
    {
        return eyesPoint != null ? eyesPoint.position : transform.position + Vector3.up;
    }

    private IEnumerator RememberEnemyPosition(GameObject enemy)
    {
        if (!enemyMemory.ContainsKey(enemy))
        {
            enemyMemory[enemy] = enemy.transform.position;
        }

        yield return new WaitForSeconds(memoryDuration);

        if (!visibleEnemies.Contains(enemy) && !detectedEnemies.Contains(enemy))
        {
            currentTarget = null;
            enemyMemory.Remove(enemy);
        }
    }

    public Vector3? GetLastKnownPosition(GameObject enemy)
    {
        if (enemyMemory.ContainsKey(enemy))
        {
            return enemyMemory[enemy];
        }
        return null;
    }
    public GameObject GetClosestInVision()
    {
        float distance = Mathf.Infinity;
        GameObject closest = null;
        foreach (GameObject go in VisibleEnemies)
        {
            float d = Vector3.Distance(transform.position, go.transform.position);
            if (d < distance)
            {
                closest = go;
                distance = d;
            }
        }
        return closest;
    }

    public GameObject GetClosestDetected()
    {
        float distance = Mathf.Infinity;
        GameObject closest = null;
        foreach (GameObject go in DetectedEnemies)
        {
            float d = Vector3.Distance(transform.position, go.transform.position);
            if (d < distance)
            {
                closest = go;
                distance = d;
            }
        }
        return closest;
    }

    public GameObject GetClosestGroupMemberInVision()
    {
        visibleGroupMembers.RemoveAll(obj => !obj);

        float distance = Mathf.Infinity;
        GameObject closest = null;
        foreach (GameObject go in visibleGroupMembers)
        {
            float d = Vector3.Distance(transform.position, go.transform.position);
            if (d < distance)
            {
                closest = go;
                distance = d;
            }
        }
        return closest;
    }

    public GameObject GetClosestInVision(string tag)
    {
        float distance = Mathf.Infinity;
        GameObject closest = null;
        foreach (GameObject go in VisibleEnemies)
        {
            if(!go.CompareTag(tag))
                continue;

            float d = Vector3.Distance(transform.position, go.transform.position);
            if (d < distance)
            {
                closest = go;
                distance = d;
            }
        }
        return closest;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    //Green = visible, red = blocked by an obstacle
    void OnDrawGizmosSelected()
    {
        foreach ((Vector3 point, VisionResult result) in lastScanResults)
        {
            VisionCheck.DrawGizmo(GetEyesPosition(), point, result);
        }
    }
}
