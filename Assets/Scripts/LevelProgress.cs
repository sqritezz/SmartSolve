using System.Collections.Generic;
using UnityEngine;

// Remembers level completion + stars for THIS play session only.
// Every time the game starts (or you press Play in Unity), everything is
// reset: no stars, "?" images, Medium/Hard locked again.
// (Same functions as before, so LevelCompleteManager and LevelCardUI
// keep working without any changes.)
//
// "category" groups the three difficulties together, e.g. "RAM", "PSU".
// "difficulty" is "Easy", "Medium", or "Hard".
public static class LevelProgress
{
    private static readonly Dictionary<string, int> bestStars = new Dictionary<string, int>();
    private static readonly HashSet<string> completed = new HashSet<string>();

    // Old save keys from the previous version (wiped on start so nothing old shows up)
    private const string OldCompletedPrefix = "LevelDone_";
    private const string OldStarsPrefix = "LevelStars_";
    private static readonly string[] OldCategories = { "RAM", "PSU", "Stage3", "HDD" };
    private static readonly string[] Difficulties = { "Easy", "Medium", "Hard" };

    // Runs automatically every time the game starts / Play is pressed
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnGameStart()
    {
        bestStars.Clear();
        completed.Clear();
        ClearOldSaves();
    }

    private static string Key(string category, string difficulty)
    {
        return category + "_" + difficulty;
    }

    public static bool IsCompleted(string category, string difficulty)
    {
        return completed.Contains(Key(category, difficulty));
    }

    public static int GetStars(string category, string difficulty)
    {
        return bestStars.TryGetValue(Key(category, difficulty), out int s) ? s : 0;
    }

    // Call this once, when a level finishes. Replaying keeps the BEST star count
    // for this session.
    public static void CompleteLevel(string category, string difficulty, int starsEarned)
    {
        string key = Key(category, difficulty);
        completed.Add(key);

        int best = Mathf.Max(starsEarned, GetStars(category, difficulty));
        bestStars[key] = best;
    }

    // Wipes all progress right now (e.g. from a "Restart" button)
    public static void ResetAll()
    {
        bestStars.Clear();
        completed.Clear();
        ClearOldSaves();
    }

    // Removes progress saved permanently by the old version
    private static void ClearOldSaves()
    {
        foreach (string c in OldCategories)
            foreach (string d in Difficulties)
            {
                PlayerPrefs.DeleteKey(OldCompletedPrefix + Key(c, d));
                PlayerPrefs.DeleteKey(OldStarsPrefix + Key(c, d));
            }
        PlayerPrefs.Save();
    }
}