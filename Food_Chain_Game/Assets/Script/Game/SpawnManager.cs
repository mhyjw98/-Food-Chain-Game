using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [Header("Zone Spawn Points")]
    public Transform forestSpawnPoint;
    public Transform fieldSpawnPoint;
    public Transform riverSpawnPoint;
    public Transform skySpawnPoint;

    [SerializeField] private Transform[] spawnPoints;
    private List<int> usedIndices = new();
    private Dictionary<ZoneType, Vector3> zonePositions;

    private void Awake()
    {
        Instance = this;

        if(forestSpawnPoint != null && fieldSpawnPoint != null && riverSpawnPoint != null && skySpawnPoint != null)
        zonePositions = new Dictionary<ZoneType, Vector3>
        {
            { ZoneType.Forest, forestSpawnPoint.position },
            { ZoneType.Field, fieldSpawnPoint.position },
            { ZoneType.River, riverSpawnPoint.position },
            { ZoneType.Sky, skySpawnPoint.position }
        };
    }

    public Vector3 GetAvailableSpawnPosition()
    {
        List<int> availableIndices = new();
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (!usedIndices.Contains(i))
                availableIndices.Add(i);
        }

        if (availableIndices.Count == 0)
        {
            Debug.LogWarning("모든 스폰 위치가 사용 중입니다.");
            return spawnPoints[0].position;
        }

        int index = availableIndices[Random.Range(0, availableIndices.Count)];
        usedIndices.Add(index);
        return spawnPoints[index].position;
    }

    public Vector3 GetRandomPositionInZone(ZoneType zone)
    {       
        float x = Random.Range(-4, 4);
        float y = Random.Range(-3.5f, 3.5f);
        return zonePositions[zone] + new Vector3(x, y, 0f);
    }

    public void ResetUsedSpawns()
    {
        usedIndices.Clear();
    }

    public Vector3 GetZonePosition(ZoneType zone)
    {
        if (zonePositions.TryGetValue(zone, out Vector3 pos))
        {
            float offsetX = Random.Range(-5f, 5f);
            float offsetY = Random.Range(-5f, 5f);

            Vector3 randomResponse = new Vector3(pos.x + offsetX, pos.y + offsetY, pos.z);
            return randomResponse;
        }

        return Vector3.zero;
    }

}
