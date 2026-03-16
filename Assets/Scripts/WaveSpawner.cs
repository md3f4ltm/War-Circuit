using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WaveSpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform player;

    public int currentWave = 1;
    public int baseEnemyCount = 3;
    public float spawnRadius = 15f;

    private int enemiesAlive = 0;
    private Coroutine nextWaveRoutine;

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
        if (enemyPrefab == null || player == null)
        {
            Debug.LogWarning("WaveSpawner is missing required references.");
            return;
        }

        int enemiesToSpawn = baseEnemyCount + (currentWave * 2);

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            SpawnEnemy();
        }
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
                GameObject enemyObj = Instantiate(enemyPrefab, hit.position, Quaternion.identity);
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

    public void EnemyDefeated()
    {
        enemiesAlive--;
        if (enemiesAlive <= 0)
        {
            enemiesAlive = 0;
            currentWave++;

            if (nextWaveRoutine != null)
            {
                StopCoroutine(nextWaveRoutine);
            }

            nextWaveRoutine = StartCoroutine(StartNextWaveAfterDelay(3f));
        }
    }

    IEnumerator StartNextWaveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        nextWaveRoutine = null;
        StartWave();
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.white;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.UpperRight;

        GUI.Label(new Rect(Screen.width - 220, 25, 200, 30), $"WAVE: {currentWave}  |  Enemies: {enemiesAlive}", style);
    }
}
