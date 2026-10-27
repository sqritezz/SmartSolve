using UnityEngine;
using TMPro;

// Put this on a TextMeshPro text (e.g. on the level's White Board).
// Shows that level's time, read from its PerformanceTracker.
// It stops automatically when the tracker's timer stops
// (explanation open, game paused, level finished).
[RequireComponent(typeof(TMP_Text))]
public class TimerDisplay : MonoBehaviour
{
    [Tooltip("This level's Performance Tracker (e.g. EasySpawnPoint (RAM))")]
    public PerformanceTracker tracker;

    [Header("Text")]
    public string label = "Time: ";
    public string labelFilipino = "Oras: ";
    [Tooltip("Shown before the real level starts (during the tutorial)")]
    public string notStartedText = "--:--";

    [Header("Colors (optional)")]
    [Tooltip("Change color as the star time limits pass")]
    public bool colorByStars = true;
    public Color threeStarColor = new Color(0.2f, 0.9f, 0.3f);  // green
    public Color twoStarColor = new Color(1f, 0.8f, 0.1f);      // yellow
    public Color oneStarColor = new Color(1f, 0.3f, 0.25f);     // red

    private TMP_Text text;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        if (tracker == null) return;

        string lbl = LanguageManager.Pick(label, labelFilipino);

        if (!tracker.isTracking)
        {
            text.text = lbl + notStartedText;
            return;
        }

        float t = tracker.elapsedTime;
        int min = (int)(t / 60f);
        int sec = (int)(t % 60f);
        text.text = lbl + min.ToString("00") + ":" + sec.ToString("00");

        if (colorByStars)
        {
            if (t <= tracker.threeStarTime) text.color = threeStarColor;
            else if (t <= tracker.twoStarTime) text.color = twoStarColor;
            else text.color = oneStarColor;
        }
    }
}