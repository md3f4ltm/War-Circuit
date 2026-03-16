using UnityEngine;
using UnityEditor;

public class EnvironmentSetupTool
{
    [MenuItem("Tools/BuildMap")]
    public static void UpgradeAndBuild()
    {
        UpgradeMaterialsToURP();
        BuildMap();
    }

    private static void UpgradeMaterialsToURP()
    {
        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/RPG Tiny Hero Duo/Material", "Assets/Polytope Studio" });
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        Shader urpSimpleLit = Shader.Find("Universal Render Pipeline/Simple Lit");

        if (urpLit == null)
        {
            Debug.LogError("URP Lit shader not found!");
            return;
        }

        foreach (string guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null && mat.shader != null && mat.shader.name != urpLit.name && mat.shader.name != urpSimpleLit.name)
            {
                // Try to upgrade Polyart materials to simple lit for better performance/look
                if (path.Contains("Polyart"))
                {
                    mat.shader = urpSimpleLit;
                }
                else
                {
                    mat.shader = urpLit;
                }
                EditorUtility.SetDirty(mat);
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Upgraded materials to URP.");
    }

    private static void BuildMap()
    {
        GameObject env = GameObject.Find("Zelda_Environment");
        if (env != null) UnityEngine.Object.DestroyImmediate(env);

        env = new GameObject("Zelda_Environment");

        // Create Floor
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Ground";
        floor.transform.parent = env.transform;
        floor.transform.localScale = new Vector3(10f, 1f, 10f); // 100x100
        
        Material floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        floorMat.color = new Color(0.2f, 0.5f, 0.2f); // Dark green
        floor.GetComponent<MeshRenderer>().material = floorMat;

        // Try to load tree
        string[] treeGuids = AssetDatabase.FindAssets("PT_Pine_Tree_03_green t:Prefab");
        GameObject treePrefab = null;
        if (treeGuids.Length > 0) treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(treeGuids[0]));

        // Try to load rock
        string[] rockGuids = AssetDatabase.FindAssets("PT_Generic_Rock_01 t:Prefab");
        GameObject rockPrefab = null;
        if (rockGuids.Length > 0) rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(rockGuids[0]));

        // Spawn some obstacles/decorations
        Random.InitState(42);
        for (int i = 0; i < 40; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-45f, 45f), 0, Random.Range(-45f, 45f));
            
            // Keep center clear for player and spawning
            if (pos.magnitude < 10f) continue;

            if (treePrefab != null && Random.value > 0.3f)
            {
                GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab);
                tree.transform.position = pos;
                tree.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
                tree.transform.localScale = Vector3.one * Random.Range(0.8f, 1.5f);
                tree.transform.parent = env.transform;
            }
            else if (rockPrefab != null)
            {
                GameObject rock = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab);
                rock.transform.position = pos;
                rock.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
                rock.transform.localScale = Vector3.one * Random.Range(0.8f, 2.0f);
                rock.transform.parent = env.transform;
            }
        }
        
        // Bake NavMesh
        GameObjectUtility.SetStaticEditorFlags(floor, StaticEditorFlags.NavigationStatic);
        foreach (Transform child in env.transform)
        {
            GameObjectUtility.SetStaticEditorFlags(child.gameObject, StaticEditorFlags.NavigationStatic);
        }
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
        
        Debug.Log("Map built and NavMesh baked.");
    }
}
