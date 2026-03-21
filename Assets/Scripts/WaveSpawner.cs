using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WaveSpawner : MonoBehaviour
{
    public GameObject GoblinEnemy;
    public Transform player;

    public int currentWave = 1;
    public int baseEnemyCount = 2;
    public int enemiesAddedPerWave = 1;
    public float minSpawnRadius = 12f;
    public float maxSpawnRadius = 18f;
    public float minSpawnSeparation = 4.5f;
    public float waveHealthBonus = 24f;
    public float waveDamageBonus = 4f;

    public event System.Action<int> WaveCleared;
    public event System.Action<Vector3> EnemyKilled;

    private int enemiesAlive = 0;
    private bool waitingForNextWave;
    private readonly List<Vector3> waveSpawnPositions = new List<Vector3>();

    void Start()
    {
        if (player == null)
        {
            GameObject pObj = GameObject.FindWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }

        StartWave();
    }

    void StartWave()
    {
        if (GoblinEnemy == null || player == null)
        {
            Debug.LogWarning("WaveSpawner is missing required references.");
            return;
        }

        waitingForNextWave = false;
        waveSpawnPositions.Clear();
        int enemiesToSpawn = baseEnemyCount + Mathf.Max(0, currentWave - 1) * enemiesAddedPerWave;

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            SpawnEnemy();
        }
    }

    public void StartNextWave()
    {
        if (!waitingForNextWave)
        {
            return;
        }

        currentWave++;
        StartWave();
    }

    void SpawnEnemy()
    {
        const int maxSpawnAttempts = 18;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            if (randomDirection.sqrMagnitude < 0.001f)
            {
                randomDirection = Vector2.up;
            }

            float spawnDistance = Random.Range(minSpawnRadius, maxSpawnRadius);
            Vector3 spawnPos = player.position + new Vector3(randomDirection.x, 0f, randomDirection.y) * spawnDistance;

            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                if (!IsFarEnoughFromOtherSpawns(hit.position))
                {
                    continue;
                }

                GameObject enemyObj = Instantiate(GoblinEnemy, hit.position, Quaternion.identity);
                EnemyController ec = enemyObj.GetComponent<EnemyController>();
                if (ec != null)
                {
                    ec.InitializeStats((currentWave - 1) * waveHealthBonus, (currentWave - 1) * waveDamageBonus);
                }

                waveSpawnPositions.Add(hit.position);
                enemiesAlive++;
                return;
            }
        }

        Debug.LogWarning("WaveSpawner could not find a valid NavMesh position for an enemy.");
    }

    bool IsFarEnoughFromOtherSpawns(Vector3 position)
    {
        for (int i = 0; i < waveSpawnPositions.Count; i++)
        {
            Vector3 existing = waveSpawnPositions[i];
            existing.y = 0f;

            Vector3 candidate = position;
            candidate.y = 0f;

            if (Vector3.Distance(existing, candidate) < minSpawnSeparation)
            {
                return false;
            }
        }

        return true;
    }

    public void EnemyDefeated(Vector3 deathPosition)
    {
        enemiesAlive--;
        EnemyKilled?.Invoke(deathPosition);
        if (enemiesAlive <= 0)
        {
            enemiesAlive = 0;
            waitingForNextWave = true;
            WaveCleared?.Invoke(currentWave);
        }
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.white;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.UpperRight;

        string statusText = waitingForNextWave ? "SHOP" : enemiesAlive.ToString();
        GUI.Label(new Rect(Screen.width - 260, 25, 240, 30), $"WAVE: {currentWave}  |  Enemies: {statusText}", style);
    }
}
