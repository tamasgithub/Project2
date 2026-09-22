using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HierarchyUtility
{
    public static Transform GetOrCreatePath(string path, Scene scene)
    {
        if (!IsUsable(scene))
        {
            Debug.LogError($"GetOrCreatePath({path}) called with an unusable scene");
            return null;
        }

        string[] parts = path.Split('/');

        Transform current = null;

        foreach (string part in parts)
        {
            Transform next;

            if (current == null)
            {
                // Must not use GameObject.Find here: it searches every loaded scene and
                // would happily return the root object of another lobby's scene instance.
                next = FindRootInScene(scene, part);

                if (next != null && !next.gameObject.activeInHierarchy)
                {
                    Debug.LogWarning($"GetOrCreatePath({path}): the existing root '{part}' is inactive, everything parented under it stays invisible");
                }

                if (next == null)
                {
                    GameObject root = new GameObject(part);
                    SceneManager.MoveGameObjectToScene(root, scene);
                    next = root.transform;
                }
            }
            else
            {
                next = current.Find(part);

                if (next == null)
                {
                    GameObject go = new GameObject(part);
                    go.transform.SetParent(current, false);
                    next = go.transform;
                }
            }

            current = next;
        }

        return current;
    }

    private static Transform FindRootInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name)
                return root.transform;
        }

        return null;
    }

    public static T FindInScene<T>(Scene scene) where T : Component
    {
        if (!IsUsable(scene)) return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        return null;
    }

    public static List<T> FindAllInScene<T>(Scene scene) where T : Component
    {
        List<T> results = new List<T>();
        if (!IsUsable(scene)) return results;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            results.AddRange(root.GetComponentsInChildren<T>(true));
        }

        return results;
    }

    private static bool IsUsable(Scene scene) => scene.IsValid() && scene.isLoaded;
}
