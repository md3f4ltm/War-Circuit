using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using UnityEditor.AI;

public static class ZeldaSetup
{
    [MenuItem("Tools/Setup Zelda Game")]
    public static void SetupGame()
    {
        // 1. Setup Camera
        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam.GetComponent<ThirdPersonCamera>() == null)
        {
            if (mainCam.GetComponent("TopDownCamera") != null)
            {
                Object.DestroyImmediate(mainCam.GetComponent("TopDownCamera"));
            }
            mainCam.gameObject.AddComponent<ThirdPersonCamera>();
        }

        // 2. Find or create Ground
        GameObject ground = GameObject.Find("Ground");
        if (ground == null) ground = GameObject.Find("Terrain");
        if (ground == null)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(10, 1, 10);
        }
        GameObjectUtility.SetStaticEditorFlags(ground, StaticEditorFlags.NavigationStatic);

        // 3. Setup Player
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            player = GameObject.Find("MaleCharacterPolyart");
            if (player == null)
            {
                string[] playerGuids = AssetDatabase.FindAssets("MaleCharacterPolyart t:Prefab");
                if (playerGuids.Length > 0)
                {
                    string playerPath = AssetDatabase.GUIDToAssetPath(playerGuids[0]);
                    GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPath);
                    player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                    player.name = "MaleCharacterPolyart";
                    player.transform.position = new Vector3(0, 0, 0);
                }
            }
        }

        if (player != null)
        {
            player.tag = "Player";
            // Disable any shield objects
            foreach (Transform child in player.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.ToLower().Contains("shield"))
                {
                    child.gameObject.SetActive(false);
                }
            }

            if (player.GetComponent<PlayerController>() == null)
                player.AddComponent<PlayerController>();
                
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc == null) cc = player.AddComponent<CharacterController>();
            if (cc != null)
            {
                cc.center = new Vector3(0, 1f, 0);
                cc.height = 2f;
                cc.radius = 0.5f;
            }
            
            Animator anim = player.GetComponentInChildren<Animator>();
            if (anim != null && anim.runtimeAnimatorController == null)
            {
                string[] guids = AssetDatabase.FindAssets("SwordAndShieldStance t:AnimatorController");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
                }
            }
        }

        // 4. Setup Enemy Prefab
        GameObject enemyPrefab = null;
        string[] enemyGuids = AssetDatabase.FindAssets("FemaleCharacterPolyart t:Prefab");
        if (enemyGuids.Length > 0)
        {
            string enemyPath = AssetDatabase.GUIDToAssetPath(enemyGuids[0]);
            GameObject originalEnemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(enemyPath);
            
            GameObject instEnemy = (GameObject)PrefabUtility.InstantiatePrefab(originalEnemyPrefab);
            instEnemy.name = "Zombie_Enemy";
            
            // Disable any shield objects
            foreach (Transform child in instEnemy.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.ToLower().Contains("shield"))
                {
                    child.gameObject.SetActive(false);
                }
            }

            if (instEnemy.GetComponent<EnemyController>() == null)
                instEnemy.AddComponent<EnemyController>();
                
            NavMeshAgent agent = instEnemy.GetComponent<NavMeshAgent>();
            if (agent == null) agent = instEnemy.AddComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.speed = 3.5f;
            }
            
            CapsuleCollider coll = instEnemy.GetComponent<CapsuleCollider>();
            if (coll == null) coll = instEnemy.AddComponent<CapsuleCollider>();
            coll.center = new Vector3(0, 1f, 0);
            coll.height = 2f;
            
            Animator eAnim = instEnemy.GetComponentInChildren<Animator>();
            if (eAnim != null && eAnim.runtimeAnimatorController == null)
            {
                string[] guids = AssetDatabase.FindAssets("SwordAndShieldStance t:AnimatorController");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    eAnim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
                }
            }

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            enemyPrefab = PrefabUtility.SaveAsPrefabAsset(instEnemy, "Assets/Prefabs/Zombie_Enemy.prefab");
            Object.DestroyImmediate(instEnemy);
        }

        // 5. Setup Wave Spawner
        GameObject spawnerObj = GameObject.Find("WaveSpawner");
        if (spawnerObj == null)
        {
            spawnerObj = new GameObject("WaveSpawner");
            WaveSpawner spawner = spawnerObj.AddComponent<WaveSpawner>();
            if (enemyPrefab != null)
            {
                spawner.enemyPrefab = enemyPrefab;
            }
            if (player != null)
            {
                spawner.player = player.transform;
            }
        }

        // 6. Bake NavMesh
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();

        Debug.Log("Zelda Game Setup Complete! You can now hit Play.");
    }
}
