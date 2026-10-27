using System.Collections;
using UnityEngine;

// Put on the "yes" button of the quit panel (or any object).
// Button > On Click () -> drag this object -> QuitGame.Quit
//
// In the Unity Editor it stops Play mode; in the built game (Quest/PC) it closes the app.
public class QuitGame : MonoBehaviour
{
    [Header("Optional")]
    [Tooltip("Plays before quitting (e.g. the shutdown sound)")]
    public AudioSource audioSource;
    public AudioClip quitSound;
    [Tooltip("Seconds to wait before closing (lets the sound play)")]
    public float delay = 0.5f;

    private bool quitting = false;

    public void Quit()
    {
        if (quitting) return;
        quitting = true;

        if (audioSource != null && quitSound != null)
            audioSource.PlayOneShot(quitSound);

        // Runs on a helper object that never gets hidden, so it still works
        // even if the Yes button / quit panel is turned off by its own On Click.
        var runner = new GameObject("QuitRunner").AddComponent<QuitRunner>();
        DontDestroyOnLoad(runner.gameObject);
        runner.Begin(delay);
    }

    private class QuitRunner : MonoBehaviour
    {
        public void Begin(float delay) { StartCoroutine(Run(delay)); }

        private IEnumerator Run(float delay)
        {
            // Realtime, so it still works if the game is paused (timeScale = 0)
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            DoQuit();
        }
    }

    private static void DoQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}