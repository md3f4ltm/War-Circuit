using UnityEngine;

public static class RuntimeSceneTemplateLibrary
{
    public static GameObject FindSceneTemplate(string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
        {
            return null;
        }

        GameObject[] loadedObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject candidate in loadedObjects)
        {
            if (candidate == null || candidate.name != templateName)
            {
                continue;
            }

            if (!candidate.scene.IsValid() || !candidate.scene.isLoaded)
            {
                continue;
            }

            return candidate;
        }

        return null;
    }
}
