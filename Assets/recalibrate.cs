using System.Collections;
using UnityEngine;
using TMPro;

// Options menu: "Reset my height" button.
// Button On Click -> RecalibrateHeightButton.Press
//
// Counts down 3..2..1 so the player can stand (or sit) normally and look straight
// ahead, then sets their view back to the game's eye height (FixedPlayerHeight).
// Works while the game is paused.
public class RecalibrateHeightButton : MonoBehaviour
{
    [Tooltip("The FixedPlayerHeight on XR Origin (found automatically if empty)")]
    public FixedPlayerHeight playerHeight;
    [Tooltip("Optional: text that shows the countdown / 'Done'")]
    public TMP_Text messageText;
    public int countdownSeconds = 3;

    [Header("Messages")]
    public string getReady = "Stand normally and look straight ahead... {0}";
    public string getReadyFilipino = "Tumayo nang normal at tumingin nang diretso... {0}";
    public string done = "Height reset!";
    public string doneFilipino = "Na-reset na ang taas!";

    private bool running;

    private void OnEnable()
    {
        running = false;
        if (messageText != null) messageText.text = "";
    }

    // If the panel closes in the middle of the countdown, the countdown stops -
    // make sure the button works again next time
    private void OnDisable()
    {
        running = false;
    }

    public void Press()
    {
        // Pressing again during the countdown simply starts it over
        if (running) StopAllCoroutines();
        running = false;
        if (playerHeight == null) playerHeight = FindFirstObjectByType<FixedPlayerHeight>();
        if (playerHeight == null)
        {
            Debug.LogWarning("[RecalibrateHeightButton] No FixedPlayerHeight in the scene (it goes on XR Origin).");
            return;
        }
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        running = true;
        for (int i = countdownSeconds; i > 0; i--)
        {
            Show(string.Format(LanguageManager.Pick(getReady, getReadyFilipino), i));
            yield return new WaitForSecondsRealtime(1f);   // keeps counting while paused
        }

        playerHeight.Recalibrate();
        Debug.Log("[RecalibrateHeightButton] Height recalibrated.", this);
        Show(LanguageManager.Pick(done, doneFilipino));
        yield return new WaitForSecondsRealtime(2f);
        Show("");
        running = false;
    }

    private void Show(string s)
    {
        if (messageText != null) messageText.text = s;
    }
}