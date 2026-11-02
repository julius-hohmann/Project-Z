using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RandomPrefabSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public List<GameObject> prefabs = new List<GameObject>();
    public float minSpawnInterval;
    public float maxSpawnInterval;
    public float spawnRadius;

    [Header("Ground Placement")]
    public LayerMask groundLayer = 1 << 10;
    public float groundOffset = 0.5f;
    public float raycastHeight = 1000f;
    public float maxNavMeshDistance = 2f;
    public int maxSpawnAttempts = 10;

    private void Start()
    {
        StartCoroutine(SpawnLoop());
    }
    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);

            SpawnPrefab();
        }
    }
    private void SpawnPrefab()
    {
        if (prefabs.Count == 0)
        {
            Debug.LogWarning(gameObject.name + " has no prefabs assigned to it!");
            return;
        }

        //Get random prefab from list
        GameObject prefabToSpawn = prefabs[Random.Range(0, prefabs.Count)];

        if (TryGetSpawnPosition(out Vector3 spawnPos))
            Instantiate(prefabToSpawn, spawnPos, Quaternion.identity, gameObject.transform);
    }

    //Get random position in spawn radius that lies on the ground and can be reached by agents
    private bool TryGetSpawnPosition(out Vector3 spawnPos)
    {
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector2 randomPos = Random.insideUnitCircle * spawnRadius;
            Vector3 rayOrigin = new Vector3(transform.position.x + randomPos.x, raycastHeight, transform.position.z + randomPos.y);

            if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit groundHit, Mathf.Infinity, groundLayer))
                continue;

            if (NavMesh.SamplePosition(groundHit.point, out NavMeshHit navMeshHit, maxNavMeshDistance, NavMesh.AllAreas))
            {
                spawnPos = navMeshHit.position + Vector3.up * groundOffset;
                return true;
            }
        }
        spawnPos = Vector3.zero;
        return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
