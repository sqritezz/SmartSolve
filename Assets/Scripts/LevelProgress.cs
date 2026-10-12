using UnityEngine;

// Saves and loads level completion + star results using PlayerPrefs.
// Call LevelProgress.CompleteLevel(category, difficulty, stars) once a level
// finishes (e.g. from whatever script shows your final star rating screen),
// and use LevelProgress.IsCompleted / GetStars anywhere else (like the
// level-select cards) to read it back.
//
// "category" groups the three difficulties together, e.g. "RAM", "PSU".
// "difficulty" is "Easy", "Medium", or "Hard".
public static class LevelProgress
{
    private const string CompletedPrefix = "LevelDone_";
    private const string StarsPrefix = "LevelStars_";

    private static string Key(string category, string difficulty)
    {
        return category + "_" + difficulty;
    }

    public static bool IsCompleted(string category, string difficulty)
    {
        return PlayerPrefs.GetInt(CompletedPrefix + Key(category, difficulty), 0) == 1;
    }

    public static int GetStars(string category, string difficulty)
    {
        return PlayerPrefs.GetInt(StarsPrefix + Key(category, difficulty), 0);
    }

    // Call this once, when a level finishes. If the player replays it and
    // does worse, this keeps the best star count instead of overwriting it
    // with a lower one.
    public static void CompleteLevel(string category, string difficulty, int starsEarned)
    {
        string key = Key(category, difficulty);
        PlayerPrefs.SetInt(CompletedPrefix + key, 1);

        int best = Mathf.Max(starsEarned, GetStars(category, difficulty));
        PlayerPrefs.SetInt(StarsPrefix + key, best);

        PlayerPrefs.Save();
    }

    // Handy for testing in the Editor - wipes all saved progress.
    public static void ResetAll()
    {
        PlayerPrefs.DeleteAll();
    }
}
