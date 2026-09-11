using UnityEngine;

// Attach this to the same spawn point object as LevelCompleteManager
// (e.g. EasySpawnPoint (RAM)). Tracks how long the player took and how
// many mistakes/hints they used, then calculates a 1-3 star rating.
public class PerformanceTracker : MonoBehaviour
{
    [Header("Time Thresholds (seconds)")]
    [Tooltip("Finish within this time, with no mistakes or hints, for 3 stars")]
    public float threeStarTime = 60f;
    [Tooltip("Finish within this time for 2 stars (if no mistakes/hints used)")]
    public float twoStarTime = 120f;

    [Header("Live Tracking (read-only)")]
    public int wrongAttempts = 0;
    public int hintsUsed = 0;

    private float startTime;

    private void Start()
    {
        startTime = Time.time;
    }

    // Call this whenever the player does something wrong (e.g. fails a
    // power-on attempt, picks a wrong diagnosis option, etc.)
    public void RegisterWrongAttempt()
    {
        wrongAttempts++;
    }

    // Call this whenever a hint is used (see HintSystem below)
    public void RegisterHintUsed()
    {
        hintsUsed++;
    }

    // Call this once, when the level completes, to get the star rating
    public int CalculateStars()
    {
        float elapsed = Time.time - startTime;
        bool flawless = wrongAttempts == 0 && hintsUsed == 0;

        if (flawless && elapsed <= threeStarTime)
            return 3;

        if (wrongAttempts <= 1 && hintsUsed <= 1 && elapsed <= twoStarTime)
            return 2;

        return 1;
    }
}