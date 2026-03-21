using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WaveSpawner : MonoBehaviour
{
    public GameObject GoblinEnemy;
    public Transform player;

    public int currentWave = 1;
    public int baseEnemyCount = 3;
    public float spawnRadius = 15f;

    public event System.Action<int> WaveCleared;
    public event System.Action<Vector3> EnemyKilled;

    private int enemiesAlive = 0;
    private bool waitingForNextWave;

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
        int enemiesToSpawn = baseEnemyCount + (currentWave * 2);

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
        const int maxSpawnAttempts = 10;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;
            Vector3 spawnPos = player.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                GameObject enemyObj = Instantiate(GoblinEnemy, hit.position, Quaternion.identity);
                EnemyController ec = enemyObj.GetComponent<EnemyController>();
                if (ec != null)
                {
                    ec.InitializeStats((currentWave - 1) * 15f, (currentWave - 1) * 3f);
                }

                enemiesAlive++;
                return;
            }
        }

        Debug.LogWarning("WaveSpawner could not find a valid NavMesh position for an enemy.");
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
