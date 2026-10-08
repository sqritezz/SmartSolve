using UnityEngine;

// One per level (same object as that level's LevelCompleteManager).
// The timer, mistakes and hints are only counted AFTER the real level starts,
// so the tutorial no longer costs you stars.
//
// How the real level starts (pick one):
//  A) Drag the REAL level's checklist panel (or any object that turns on when the
//     real level begins) into "Start When Active". Tracking starts the moment it
//     turns on.
//  B) Wire StartTracking() to an event, e.g. TutorialFlowManager's
//     "On Tutorial Complete", or the button/teleport that starts the level.
//  C) No tutorial for this level? Tick "Start Immediately".
public class PerformanceTracker : MonoBehaviour
{
    [Header("Time Thresholds (seconds)")]
    [Tooltip("Finish within this time, with no mistakes or hints, for 3 stars")]
    public float threeStarTime = 60f;
    [Tooltip("Finish within this time for 2 stars (max 1 mistake and 1 hint)")]
    public float twoStarTime = 120f;

    [Header("When Tracking Starts")]
    [Tooltip("Start counting as soon as the game starts (only for levels WITHOUT a tutorial)")]
    public bool startImmediately = false;
    [Tooltip("Start counting when this object turns on (e.g. the real level's checklist panel)")]
    public GameObject startWhenActive;

    [Header("Live Tracking (read-only)")]
    public bool isTracking = false;
    public int wrongAttempts = 0;
    public int hintsUsed = 0;
    public float elapsedTime = 0f;

    private float startTime;
    private bool finished = false;

    private void Start()
    {
        if (startImmediately) StartTracking();
    }

    private void Update()
    {
        if (!isTracking && !finished && startWhenActive != null && startWhenActive.activeInHierarchy)
            StartTracking();

        if (isTracking && !finished)
            elapsedTime = Time.time - startTime;
    }

    // Call this when the REAL level begins (after the tutorial).
    // Resets everything, so anything done during the tutorial is forgotten.
    public void StartTracking()
    {
        if (isTracking) return;
        isTracking = true;
        finished = false;
        wrongAttempts = 0;
        hintsUsed = 0;
        elapsedTime = 0f;
        startTime = Time.time;
    }

    // Call this whenever the player does something wrong.
    // Ignored during the tutorial.
    public void RegisterWrongAttempt()
    {
        if (!isTracking || finished) return;
        wrongAttempts++;
    }

    // Call this whenever a hint is used. Ignored during the tutorial.
    public void RegisterHintUsed()
    {
        if (!isTracking || finished) return;
        hintsUsed++;
    }

    // Called once when the level completes. Also freezes the timer.
    public int CalculateStars()
    {
        if (!isTracking)
        {
            // Never started (nothing wired) - don't punish the player
            Debug.LogWarning("[PerformanceTracker] " + name +
                " never started tracking. Set 'Start When Active' or call StartTracking().");
            return 3;
        }

        if (!finished)
        {
            elapsedTime = Time.time - startTime;
            finished = true;
        }

        bool flawless = wrongAttempts == 0 && hintsUsed == 0;

        if (flawless && elapsedTime <= threeStarTime)
            return 3;

        if (wrongAttempts <= 1 && hintsUsed <= 1 && elapsedTime <= twoStarTime)
            return 2;

        return 1;
    }
}