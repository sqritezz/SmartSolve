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
        // Undo the pause first (pause panel sets time to 0 and mutes sounds)
        Time.timeScale = 1f;
        AudioListener.pause = false;

        string level = backToSameLevel ? TeleportToPoint.CurrentLevel : "";
        // Going back to a level: don't ask "How well do you know computer?" again
        if (!string.IsNullOrEmpty(level) && ExperienceChoice.HasChosen) ExperienceChoice.SkipQuestionOnce = true;

        // A helper that survives the reload, so it can teleport the player afterwards
        var runner = new GameObject("RestartRunner").AddComponent<RestartRunner>();
        DontDestroyOnLoad(runner.gameObject);
        runner.Go(SceneManager.GetActiveScene().buildIndex, level);
    }

    private class RestartRunner : MonoBehaviour
    {
        public void Go(int sceneIndex, string level) { StartCoroutine(Run(sceneIndex, level)); }

        private IEnumerator Run(int sceneIndex, string level)
        {
            yield return SceneManager.LoadSceneAsync(sceneIndex);
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