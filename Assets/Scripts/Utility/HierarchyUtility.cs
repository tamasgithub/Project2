using UnityEngine;
using UnityEngine.SceneManagement;

public static class HierarchyUtility
{
    public static Transform GetOrCreatePath(string path, Scene scene)
    {
        string[] parts = path.Split('/');

        Transform current = null;

        foreach (string part in parts)
        {
            Transform next = null;

            if (current == null)
            {
                // Look for a root object
                GameObject root = GameObject.Find(part);

                if (root == null)
                {
                    root = new GameObject(part);
                    SceneManager.MoveGameObjectToScene(root, scene);
                }

                next = root.transform;
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
}