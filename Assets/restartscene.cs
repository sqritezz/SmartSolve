using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Put this on the pause panel (Ypanelbtn).
// Restart button  On Click -> RestartScene.Restart
//
// Reloads the scene fresh (every object back to how it starts) and un-pauses the game.
// "Back To Same Level" = after reloading, the player is teleported straight back to
// the level they were playing (fresh start), instead of the beginning of the game.
public class RestartScene : MonoBehaviour
{
    [Tooltip("Ticked = return to the same level after restarting. Unticked = start the whole game over.")]
    public bool backToSameLevel = true;

    public void Restart()
    {
        Debug.Log("[RestartScene] Restart pressed - reloading the scene...");
        // Undo the pause first (pause panel sets time to 0 and mutes sounds)
        Time.timeScale = 1f;
        AudioListener.pause = false;

        string level = backToSameLevel ? TeleportToPoint.CurrentLevel : "";
        TeleportToPoint.RestartIntoLevel = level;   // TeleportToPoint takes the player there after the reload
        // Going back to a level: don't ask "How well do you know computer?" again
        if (!string.IsNullOrEmpty(level) && ExperienceChoice.HasChosen) ExperienceChoice.SkipQuestionOnce = true;

        // A helper that survives the reload, so it can teleport the player afterwards
        var runner = new GameObject("RestartRunner").AddComponent<RestartRunner>();
        DontDestroyOnLoad(runner.gameObject);
        runner.Go(SceneManager.GetActiveScene(), "");
    }

    private class RestartRunner : MonoBehaviour
    {
        public void Go(Scene scene, string level) { StartCoroutine(Run(scene, level)); }

        private IEnumerator Run(Scene scene, string level)
        {
            AsyncOperation op;
            if (scene.buildIndex >= 0)
                op = SceneManager.LoadSceneAsync(scene.buildIndex);
            else
            {
#if UNITY_EDITOR
                // Scene not in the Build list: still works while testing in the Editor
                op = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                        scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
                op = SceneManager.LoadSceneAsync(scene.name);
#endif
                Debug.LogWarning("[RestartScene] '" + scene.name + "' is not in File > Build Profiles > Scene List. " +
                                 "Add it, or Restart will NOT work on the Quest.");
            }
            if (op == null) { Debug.LogError("[RestartScene] Couldn't reload the scene."); Destroy(gameObject); yield break; }
            yield return op;
            // Let every object finish its Start() first
            yield return null;
            yield return null;

            if (!string.IsNullOrEmpty(level))
            {
                var tp = FindFirstObjectByType<TeleportToPoint>();
                string method = "Teleport" + level.Replace(" ", "");   // "RAM Easy" -> TeleportRAMEasy
                if (tp != null) tp.SendMessage(method, SendMessageOptions.DontRequireReceiver);
                else Debug.LogWarning("[RestartScene] No TeleportToPoint found to return to " + level);
            }
            Destroy(gameObject);
        }
    }
}