using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Every scene a client has loaded (MainMenu, Lobby, Game) carries its own
/// Camera and AudioListener. Because lobby and game scenes are loaded additively, all of them
/// are enabled at the same time: several cameras render over each other and Unity warns about
/// multiple AudioListeners.
///
/// Exactly one of those scenes is the one the local player is in. Activate() enables that
/// scene's cameras and listeners and disables the ones in every other loaded scene.
/// </summary>
public static class SceneViewControl
{
    public static void ActivateOnly(Scene active)
    {
        if (!active.IsValid() || !active.isLoaded)
        {
            Debug.LogError("SceneViewControl.ActivateOnly called with a scene that is not loaded");
            return;
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid() || !scene.isLoaded) continue;

            SetEnabled(scene, scene == active);
        }

        // Also make it the active scene. Unity puts every object created without an explicit
        // scene into the active one, Mirror's client side spawns included, so without this the
        // client keeps creating objects in the MainMenuScene and moving them over afterwards.
        // Server side this must never happen: there the active scene would arbitrarily become
        // one lobby's, which is why only ActivateOnly does it and Disable does not.
        SceneManager.SetActiveScene(active);
    }

    /// <summary>
    /// Silences a scene's camera and listener outright. Used by the server, where every lobby
    /// adds another game scene and with it another camera that would render and another
    /// AudioListener that would warn.
    /// </summary>
    public static void Disable(Scene scene) => SetEnabled(scene, false);

    private static void SetEnabled(Scene scene, bool enabled)
    {
        foreach (Camera camera in HierarchyUtility.FindAllInScene<Camera>(scene))
        {
            camera.enabled = enabled;
        }

        foreach (AudioListener listener in HierarchyUtility.FindAllInScene<AudioListener>(scene))
        {
            listener.enabled = enabled;
        }
    }
}
